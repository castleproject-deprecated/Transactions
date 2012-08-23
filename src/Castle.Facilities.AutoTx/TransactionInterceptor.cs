﻿#region license

// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Castle.Core;
using Castle.Core.Interceptor;
using Castle.Core.Logging;
using Castle.DynamicProxy;
using Castle.MicroKernel;
using Castle.Transactions;
using Castle.Transactions.Internal;
using TransactionException = Castle.Transactions.TransactionException;
using TransactionManager = Castle.Transactions.TransactionManager;

namespace Castle.Facilities.AutoTx
{
	internal class TransactionInterceptor : IInterceptor, IOnBehalfAware
	{
		private enum InterceptorState
		{
			Constructed,
			Initialized,
			Active
		}

		private InterceptorState _State;
		private readonly IKernel _Kernel;
		private readonly ITransactionMetaInfoStore _Store;
		private Maybe<TransactionalClassMetaInfo> _MetaInfo;
		private ILogger _Logger = NullLogger.Instance;

		public ILogger Logger
		{
			get { return _Logger; }
			set { _Logger = value; }
		}

		public TransactionInterceptor(IKernel kernel, ITransactionMetaInfoStore store)
		{
			Contract.Requires(kernel != null, "kernel must be non null");
			Contract.Requires(store != null, "store must be non null");
			Contract.Ensures(_State == InterceptorState.Constructed);

			if (_Logger.IsDebugEnabled) {
				_Logger.Debug("created transaction interceptor");
			}

			_Kernel = kernel;
			_Store = store;
			_State = InterceptorState.Constructed;
		}

		[ContractInvariantMethod]
		private void Invariant()
		{
			Contract.Invariant(_State != InterceptorState.Initialized || _MetaInfo != null);
		}

		void IInterceptor.Intercept(IInvocation invocation)
		{
			Contract.Assume(_State == InterceptorState.Active || _State == InterceptorState.Initialized);
			Contract.Assume(invocation != null);

			var txManagerC = _Kernel.Resolve<TransactionManager>();
			ITransactionManager txManager = txManagerC;

			var mTxMethod = _MetaInfo.Do(x => x.AsTransactional(invocation.Method.DeclaringType.IsInterface
			                                                    	? invocation.MethodInvocationTarget
			                                                    	: invocation.Method));

			var mTxData = mTxMethod.Do(x => txManager.CreateTransaction(x));

			_State = InterceptorState.Active;

			try
			{
				if (!mTxData.HasValue)
				{
					if (mTxMethod.HasValue && mTxMethod.Value.Mode == TransactionScopeOption.Suppress)
					{
						if (_Logger.IsInfoEnabled)
							_Logger.Info("supressing ambient transaction");

						using (new TxScope(null, _Logger.CreateChildLogger("TxScope")))
							invocation.Proceed();
					}
					else invocation.Proceed();

					return;
				}

				var transaction = mTxData.Value.Transaction;
				Contract.Assume(transaction.State == TransactionState.Active,
				                "from post-condition of ITransactionManager CreateTransaction in the (HasValue -> ...)-case");

				if (mTxData.Value.ShouldFork)
				{
					var task = ForkCase(invocation, mTxData.Value);
					txManagerC.EnlistDependentTask(task);
				}
				else SynchronizedCase(invocation, transaction);
			}
			finally
			{
				_Kernel.ReleaseComponent(txManagerC);
			}
		}

		private void SynchronizedCase(IInvocation invocation, ITransaction transaction)
		{
			Contract.Requires(transaction.State == TransactionState.Active);

			using (new TxScope(transaction.Inner, _Logger.CreateChildLogger("TxScope")))
			{
				var localIdentifier = transaction.LocalIdentifier;

				try
				{
					invocation.Proceed();

					if (transaction.State == TransactionState.Active)
						transaction.Complete();
					else if (_Logger.IsWarnEnabled)
						_Logger.WarnFormat(
							"tx#{0} was in state {1}, so it cannot be completed. the 'consumer' method, so to speak, might have rolled it back.",
							localIdentifier,
							transaction.State);
				}
				catch (TransactionAbortedException)
				{
					// if we have aborted the transaction, we both warn and re-throw the exception
					if (_Logger.IsWarnEnabled)
						_Logger.WarnFormat("transaction aborted - synchronized case, tx#{0}", localIdentifier);

					throw;
				}
				catch (TransactionException ex)
				{
					if (_Logger.IsFatalEnabled)
						_Logger.FatalFormat(ex, "internal error in transaction system - synchronized case, tx#{0}", localIdentifier);

					throw;
				}
				catch (AggregateException ex)
				{
					if (_Logger.IsWarnEnabled)
						_Logger.WarnFormat(ex, "one or more dependent transactions failed, re-throwing exceptions! tx#{0}", localIdentifier);

					throw;
				}
				catch (Exception e)
				{
					if (_Logger.IsErrorEnabled)
						_Logger.ErrorFormat(e, "caught exception, transaction will roll back - synchronized case - tx#{0}",
								  localIdentifier);

					// the transaction rolls back itself on exceptions, so just throw it
					throw;
				}
				finally
				{
					if (_Logger.IsDebugEnabled)
						_Logger.DebugFormat("dispoing transaction - synchronized case - tx#{0}", localIdentifier);

					transaction.Dispose();
				}
			}
		}

		/// <summary>
		/// For ordering interleaving of threads during testing!
		/// </summary>
		internal static ManualResetEvent Finally;

		private Task ForkCase(IInvocation invocation, ICreatedTransaction txData)
		{
			Contract.Requires(txData.Transaction.State == TransactionState.Active);
			Contract.Ensures(Contract.Result<Task>() != null);
			Contract.Assume(txData.Transaction.Inner is DependentTransaction);

			if (_Logger.IsDebugEnabled)
				_Logger.DebugFormat("fork case invocation {0} tx#{1}", invocation, txData.Transaction.LocalIdentifier);

			return Task.Factory.StartNew(t =>
			{
				var hasException = false;
				var tuple = (Tuple<IInvocation, ICreatedTransaction, string>)t;
				var dependent = tuple.Item2.Transaction.Inner as DependentTransaction;

				using (tuple.Item2.GetForkScope())
				{
					try
					{
						if (_Logger.IsDebugEnabled)
							_Logger.DebugFormat("calling proceed on tx#{0}", tuple.Item3);
						
						using (var ts = new TransactionScope(dependent))
						{
							tuple.Item1.Proceed();
							if (_Logger.IsDebugEnabled)
								_Logger.DebugFormat("calling complete on TransactionScope for tx#{0}", tuple.Item3);
							ts.Complete();
						}
					}
					catch (TransactionAbortedException ex)
					{
						// if we have aborted the transaction, we both warn and re-throw the exception
						hasException = true;

						if (_Logger.IsWarnEnabled)
							_Logger.WarnFormat(ex, "transaction aborted, tx#{0}", tuple.Item3);

						throw new TransactionAbortedException(
							String.Format("Parallel/forked transaction aborted! See inner exception for details. tx#{0}", tuple.Item3), ex);
					}
					catch (Exception)
					{
						hasException = true;
						throw;
					}
					finally
					{
						if (!hasException)
						{
							if (_Logger.IsDebugEnabled)
								_Logger.DebugFormat("tx#{0} in finally-clause", tuple.Item2.Transaction.LocalIdentifier);
							dependent.Complete();
						}
						else
						{
							if (_Logger.IsDebugEnabled)
								_Logger.DebugFormat("tx#{0} in finally-clause, skipping completing dependent because it did throw exception", tuple.Item2.Transaction.LocalIdentifier);
						}

						if (Finally != null) 
							Finally.Set();

						// See footnote at end of file
					}
				}
			}, Tuple.Create(invocation, txData, txData.Transaction.LocalIdentifier));
		}

		void IOnBehalfAware.SetInterceptedComponentModel(ComponentModel target)
		{
			Contract.Ensures(_MetaInfo != null);
			Contract.Assume(target != null && target.Implementation != null);
			_MetaInfo = _Store.GetMetaFromType(target.Implementation);
			_State = InterceptorState.Initialized;
		}
	}

	// * In their code: http://msdn.microsoft.com/en-us/library/ms229976%28v=vs.90%29.aspx#Y17
	//
	// MSDN article is wrong; transaction is committed twice now:
	//                      System.InvalidOperationException was unhandled by user code
	//Message=The operation is not valid for the current state of the enlistment.
	//Source=System.Transactions
	//StackTrace:
	//     at System.Transactions.EnlistmentState.ForceRollback(InternalEnlistment enlistment, Exception e)
	//     at System.Transactions.PreparingEnlistment.ForceRollback(Exception e)
	//     at NHibernate.Transaction.AdoNetWithDistributedTransactionFactory.DistributedTransactionContext.System.Transactions.IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment) in d:\CSharp\NH\NH\nhibernate\src\NHibernate\Transaction\AdoNetWithDistributedTransactionFactory.cs:line 129
	//     at System.Transactions.VolatileEnlistmentPreparing.EnterState(InternalEnlistment enlistment)
	//     at System.Transactions.VolatileEnlistmentActive.ChangeStatePreparing(InternalEnlistment enlistment)
	//     at System.Transactions.TransactionStatePhase0.Phase0VolatilePrepareDone(InternalTransaction tx)
	//     at System.Transactions.EnlistableStates.CompleteBlockingClone(InternalTransaction tx)
	//     at System.Transactions.DependentTransaction.Complete()
	//     at Castle.Facilities.AutoTx.TransactionInterceptor.<ForkCase>b__4(Object t) in f:\code\castle\Castle.Services.Transaction\src\Castle.Facilities.AutoTx\TransactionInterceptor.cs:line 144
	//     at System.Threading.Tasks.Task.InnerInvoke()
	//     at System.Threading.Tasks.Task.Execute()
	//InnerException: 
	//dependent.Complete();

	// Second wrong; calling dispose on it
	// MSDN-article is wrong; transaction is disposed twice in their code:
	// tuple.Item2.Dispose(); 
}