namespace Castle.NHibIntegration.Tx
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Diagnostics;
	using System.Transactions;
	using NHibernate;
	using NHibernate.Engine;
	using NHibernate.Engine.Transaction;
	using NHibernate.Exceptions;
	using NHibernate.Impl;
	using NHibernate.Transaction;


	public class CastleFriendlyScopelessTxFactory : ITransactionFactory
	{
		private static readonly IInternalLogger logger = LoggerProvider.LoggerFor(typeof(CastleFriendlyScopelessTxFactory));

		public void Configure(IDictionary props)
		{
		}

		public ITransaction CreateTransaction(ISessionImplementor session)
		{
			return new AdoBoundTransaction(session);
		}

		public void EnlistInDistributedTransactionIfNeeded(ISessionImplementor session)
		{
			if (session.TransactionContext != null)
				return;

			var ambientTx = Transaction.Current;

			if (ambientTx == null)
				return;

			var transactionContext = new DistributedTransactionContext(session);
			session.TransactionContext = transactionContext;

			if (logger.IsDebugEnabled)
				logger.DebugFormat("Enlisted into ambient transaction: {0}. Id: {1}.", ambientTx.IsolationLevel,
					ambientTx.TransactionInformation.LocalIdentifier);

			session.AfterTransactionBegin(null);

			if (!session.ConnectionManager.Transaction.IsActive)
			{
				transactionContext.ShouldCloseSessionOnDistributedTransactionCompleted = true;
				session.ConnectionManager.Transaction.Begin(ambientTx.IsolationLevel.AsDataIsolationLevel());
			}
			else
			{
				logger.Debug("Tx is active");
			}

			ambientTx.EnlistVolatile(transactionContext, EnlistmentOptions.EnlistDuringPrepareRequired);
		}

		public bool IsInDistributedActiveTransaction(ISessionImplementor session)
		{
			var distributedTransactionContext = ((DistributedTransactionContext)session.TransactionContext);

			return distributedTransactionContext != null && distributedTransactionContext.IsInActiveTransaction;
		}

		public void ExecuteWorkInIsolation(ISessionImplementor session, IIsolatedWork work, bool transacted)
		{
			if (logger.IsDebugEnabled)
				logger.Debug("ExecuteWorkInIsolation Session: " + session.SessionId);

			IDbConnection connection = null;
			IDbTransaction trans = null;
			// bool wasAutoCommit = false;
			try
			{
				connection = session.Factory.ConnectionProvider.GetConnection();

				if (transacted)
				{
					trans = connection.BeginTransaction();
				}

				work.DoWork(connection, trans);

				if (transacted)
				{
					trans.Commit();
				}
			}
			catch (Exception t)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					try
					{
						if (trans != null && connection.State != ConnectionState.Closed)
						{
							trans.Rollback();
						}
					}
					catch (Exception ignore)
					{
						logger.Debug("unable to release connection on exception [" + ignore + "]");
					}

					if (t is HibernateException)
					{
						throw;
					}
					else if (t is DbException)
					{
						throw ADOExceptionHelper.Convert(session.Factory.SQLExceptionConverter, t,
														 "error performing isolated work");
					}
					else
					{
						throw new HibernateException("error performing isolated work", t);
					}
				}
			}
			finally
			{
				session.Factory.ConnectionProvider.CloseConnection(connection);
			}
		}

		public class DistributedTransactionContext : ITransactionContext, IEnlistmentNotification
		{
			private readonly ISessionImplementor session;
			private readonly AdoBoundTransaction nhtx;
			private readonly Stopwatch stopwatch = new Stopwatch();

			public bool IsInActiveTransaction;

			public DistributedTransactionContext(ISessionImplementor session)
			{
				this.session = session;

				nhtx = session.ConnectionManager.Transaction as AdoBoundTransaction;

				IsInActiveTransaction = true;
			}

			//public System.Transactions.Transaction AmbientTransation { get; set; }

			public bool ShouldCloseSessionOnDistributedTransactionCompleted { get; set; }

			#region IEnlistmentNotification Members

			void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
			{
				stopwatch.Start();

				using (new SessionIdLoggingContext(session.SessionId))
				{
					try
					{
						if (logger.IsDebugEnabled)
							logger.Debug("Preparing NHibernate resource");

						nhtx.Prepare();

						preparingEnlistment.Prepared();
					}
					catch (Exception exception)
					{
						logger.Error("Transaction prepare phase failed", exception);

						preparingEnlistment.ForceRollback(exception);
					}
				}
			}

			void IEnlistmentNotification.Commit(Enlistment enlistment)
			{
				//Console.WriteLine("ctxn c");

				using (new SessionIdLoggingContext(session.SessionId))
				{
					if (logger.IsDebugEnabled)
						logger.Debug("Committing NHibernate resource");

					nhtx.Commit();
					End(true);

					Done(enlistment);
				}
			}

			private void Done(Enlistment enlistment)
			{
				enlistment.Done();
				IsInActiveTransaction = false;

				PostMetric();
			}

			void IEnlistmentNotification.Rollback(Enlistment enlistment)
			{
				//Console.WriteLine("ctxn r");

				using (new SessionIdLoggingContext(session.SessionId))
				{
					//session.AfterTransactionCompletion(false, null);
					if (logger.IsDebugEnabled)
						logger.Debug("Rolled back NHibernate resource");

					nhtx.Rollback();
					End(false);

					Done(enlistment);
				}
			}

			void IEnlistmentNotification.InDoubt(Enlistment enlistment)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					session.AfterTransactionCompletion(false, null);

					if (logger.IsDebugEnabled)
						logger.Debug("NHibernate resource is in doubt");

					End(false);

					Done(enlistment);
				}
			}

			private void PostMetric()
			{
				stopwatch.Stop();

				// new MetricsTimer(Naming.withEnvironmentApplicationAndHostname("nhibernate.tx.flush"), payload: stopwatch.ElapsedMilliseconds()).Dispose();
			}

			void End(bool wasSuccessful)
			{
				using (new SessionIdLoggingContext(session.SessionId))
				{
					((DistributedTransactionContext)session.TransactionContext).IsInActiveTransaction = false;

					session.AfterTransactionCompletion(wasSuccessful, null);

					if (ShouldCloseSessionOnDistributedTransactionCompleted)
					{
						session.CloseSessionFromDistributedTransaction();
					}

					session.TransactionContext = null;
				}
			}

			#endregion

			public void Dispose()
			{
				//nhtx.Dispose();
			}
		}
	}

	internal static class IsolationLevelExtensions
	{
		internal static System.Data.IsolationLevel AsDataIsolationLevel(this System.Transactions.IsolationLevel level)
		{
			switch (level)
			{
				case System.Transactions.IsolationLevel.Chaos:
					return System.Data.IsolationLevel.Chaos;
				case System.Transactions.IsolationLevel.ReadCommitted:
					return System.Data.IsolationLevel.ReadCommitted;
				case System.Transactions.IsolationLevel.ReadUncommitted:
					return System.Data.IsolationLevel.ReadUncommitted;
				case System.Transactions.IsolationLevel.RepeatableRead:
					return System.Data.IsolationLevel.RepeatableRead;
				case System.Transactions.IsolationLevel.Serializable:
					return System.Data.IsolationLevel.Serializable;
				default:
					return System.Data.IsolationLevel.Unspecified;
			}
		}
	}
}
