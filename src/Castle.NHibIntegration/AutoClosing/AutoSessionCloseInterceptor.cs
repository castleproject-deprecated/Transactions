namespace Castle.NHibIntegration.AutoClosing
{
	using System;
	using System.Threading.Tasks;
	using Core;
	using Core.Interceptor;
	using Core.Logging;
	using DynamicProxy;

	public class AutoSessionCloseInterceptor : IInterceptor, IOnBehalfAware
	{
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

		public void SetInterceptedComponentModel(ComponentModel target)
		{
			_meta = _store.GetMetaFromType(target.Implementation);
		}

		public void Intercept(IInvocation invocation)
		{
			var keyMethod = invocation.Method.DeclaringType.IsInterface
				? invocation.MethodInvocationTarget
				: invocation.Method;

			var hasAUtoCloseEnabled = _meta.HasAutoCloseEnabled(keyMethod);

			if (!hasAUtoCloseEnabled)
			{
				// nothing to do

				invocation.Proceed();

				return;
			}

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
				_logger.Error("Transactional call failed", e);

				// Early termination. nothing to do
				
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
				EnsureSessionsDisposed();
			}
		}

		private void EnsureSessionsDisposed()
		{
			_sessionStore.DisposeAllInCurrentContext();
		}
	}
}