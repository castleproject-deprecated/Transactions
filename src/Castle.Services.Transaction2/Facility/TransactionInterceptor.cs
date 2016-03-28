namespace Castle.Services.Transaction.Facility
{
	using System;
	using System.Diagnostics.Contracts;
	using System.Threading.Tasks;
	using System.Transactions;
	using Core;
	using Core.Interceptor;
	using Core.Logging;
	using DynamicProxy;
	using MicroKernel;
	using Transaction;


	class TransactionInterceptor : IInterceptor, IOnBehalfAware
	{
		private readonly IKernel _kernel;
		private readonly ITransactionMetaInfoStore _store;
		private TransactionalClassMetaInfo _meta;
		private ILogger _logger = NullLogger.Instance;

		public TransactionInterceptor(IKernel kernel, ITransactionMetaInfoStore store)
		{
			_kernel = kernel;
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
			var txManager = _kernel.Resolve<ITransactionManager2>();

			var keyMethod = invocation.Method.DeclaringType.IsInterface
				? invocation.MethodInvocationTarget
				: invocation.Method;

			var opts = _meta.AsTransactional(keyMethod);

			if (txManager.CurrentTransaction != null || opts == null)
			{
				// nothing to do - no support for nesting transactions for now

				invocation.Proceed();

				return;
			}

			var transaction = txManager.CreateTransaction(opts);
			if (typeof(Task).IsAssignableFrom(invocation.MethodInvocationTarget.ReturnType))
			{
				AsyncCase(invocation, transaction);
			}
			else
			{
				SynchronizedCase(invocation, transaction);
			}
		}

		private void AsyncCase(IInvocation invocation, ITransaction2 transaction)
		{
			if (_logger.IsDebugEnabled) _logger.Debug("async case");

			try
			{
				invocation.Proceed();

				var ret = (Task) invocation.ReturnValue;

				if (ret == null)
					throw new Exception("Async method returned null instead of Task - bad programmer somewhere");

				SafeHandleAsyncCompletion(ret, transaction);
			}
			catch (Exception e)
			{
				_logger.Error("Transactional call failed", e);

				// Early termination. nothing to do besides disposing the transaction

				transaction.Dispose();

				throw;
			}
		}

		private void SafeHandleAsyncCompletion(Task ret, ITransaction2 transaction)
		{
			if (!ret.IsCompleted)
			{
				// When promised to complete in the future - should this be a case for DependentTransaction?
				// Transaction.Current.DependentClone(DependentCloneOption.BlockCommitUntilComplete));

				ret.ContinueWith((t, aTransaction) =>
				{
					var tran = (ITransaction2) aTransaction;

					try
					{
						if (!t.IsFaulted && !t.IsCanceled && 
							tran.State == TransactionState.Active)
						{
							try
							{
								tran.Complete();
								tran.Dispose();
							}
							catch (Exception e)
							{
								_logger.Error("Transaction complete error ", e);
								throw;
							}
						}
					}
					finally
					{
						tran.Dispose();
					}

				}, transaction, TaskContinuationOptions.ExecuteSynchronously);
			}
			else
			{
				// When completed synchronously 

				try
				{
					if (transaction.State == TransactionState.Active && !ret.IsFaulted && !ret.IsCanceled)
					{
						transaction.Complete();
						transaction.Dispose();
					}
					else if (_logger.IsWarnEnabled)
						_logger.WarnFormat(
							"transaction was in state {0}, so it cannot be completed. the 'consumer' method, so to speak, might have rolled it back.",
							transaction.State);
				}
				finally
				{
					transaction.Dispose();
				}
			}
		}

		private void SynchronizedCase(IInvocation invocation, ITransaction2 transaction)
		{
			if (_logger.IsDebugEnabled)
				_logger.Debug("synchronized case");

			// using (new TxScope(transaction.Inner, _logger.CreateChildLogger("TxScope")))

			var localIdentifier = transaction.LocalIdentifier;

			try
			{
				invocation.Proceed();

				if (transaction.State == TransactionState.Active)
				{
					transaction.Complete();
					transaction.Dispose();
				}
				else if (_logger.IsWarnEnabled)
					_logger.WarnFormat(
						"transaction was in state {0}, so it cannot be completed. the 'consumer' method, so to speak, might have rolled it back.",
						transaction.State);
			}
			catch (Exception)
			{
				if (_logger.IsErrorEnabled)
					_logger.Error("caught exception, rolling back transaction - synchronized case - tx#" + localIdentifier);
				throw;
			}
			finally
			{
				if (_logger.IsDebugEnabled)
					_logger.Debug("dispoing transaction - synchronized case - tx#" + localIdentifier);

				transaction.Dispose();
			}
		}
	}
}
