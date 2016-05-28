namespace Castle.NHibIntegration.AutoClosing
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Core;
	using Core.Interceptor;
	using Core.Logging;
	using DynamicProxy;

	public class AutoSessionCloseInterceptor : IInterceptor, IOnBehalfAware
	{
		private static readonly object Marker = new object();
		private static readonly AsyncLocal<object> AutoCloseInEffectMarkerAsyncLocal = new AsyncLocal<object>();

		private readonly ISessionStore _sessionStore;
		private readonly INhMetaInfoStore _store;
		private ILogger _logger = NullLogger.Instance;
		private AutoCloseClassMetaInfo _meta;


		public AutoSessionCloseInterceptor(ISessionStore sessionStore, INhMetaInfoStore store)
		{
//			_kernel = kernel;
			_sessionStore = sessionStore;
			_store = store;
		}

		public ILogger Logger
		{
			get { return _logger; }
			set { _logger = value; }
		}

		private void AddMarker()
		{
			AutoCloseInEffectMarkerAsyncLocal.Value = Marker;
		}

		private void RemoveMarker()
		{
			AutoCloseInEffectMarkerAsyncLocal.Value = null;
		}

		private bool HasPreviousMarker()
		{
			return AutoCloseInEffectMarkerAsyncLocal.Value != null;
		}

		public void SetInterceptedComponentModel(ComponentModel target)
		{
			_meta = _store.GetMetaFromType(target.Implementation);
		}

		public void Intercept(IInvocation invocation)
		{
			if (HasPreviousMarker())
			{
				invocation.Proceed();
				return;
			}

			var keyMethod = invocation.Method.DeclaringType.IsInterface
				? invocation.MethodInvocationTarget
				: invocation.Method;

			var hasAutoCloseEnabled = _meta.HasAutoCloseEnabled(keyMethod);
			if (!hasAutoCloseEnabled)
			{
				// nothing to do

				invocation.Proceed();

				return;
			}

			AddMarker();

			if (typeof(Task).IsAssignableFrom(invocation.MethodInvocationTarget.ReturnType))
			{
				AsyncCase(invocation);
			}
			else
			{
				SynchronizedCase(invocation);
			}
		}

		private void AsyncCase(IInvocation invocation)
		{
			try
			{
				invocation.Proceed();

				var ret = (Task) invocation.ReturnValue;

				if (ret == null)
					throw new Exception("Async method returned null instead of Task - bad programmer somewhere");

				SafeHandleAsyncCompletion(ret);
			}
			catch (Exception e)
			{
				_logger.Error("Auto close session call failed", e);

				// Early termination. nothing to do

				RemoveMarker();
				EnsureSessionsDisposed();

				throw;
			}
		}

		// This method should not throw
		private void SafeHandleAsyncCompletion(Task ret)
		{
			if (!ret.IsCompleted)
			{
				// When promised to complete in the future

				ret.ContinueWith((t, arg) =>
				{
					var pthis = arg as AutoSessionCloseInterceptor;
					pthis.EnsureSessionsDisposed();

				}, this, TaskContinuationOptions.ExecuteSynchronously);
			}
			else
			{
				// When completed synchronously 
				RemoveMarker();
				EnsureSessionsDisposed();
			}
		}

		private void SynchronizedCase(IInvocation invocation)
		{
			try
			{
				invocation.Proceed();
			}
			finally
			{
				RemoveMarker();
				EnsureSessionsDisposed();
			}
		}

		private void EnsureSessionsDisposed()
		{
			_sessionStore.DisposeAllInCurrentContext();
		}
	}
}