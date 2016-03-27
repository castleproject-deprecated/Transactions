namespace Castle.Services.Transaction
{
	using System;
	using System.Transactions;
	using Core.Logging;
	using Transaction2.Internal;

	public class TransactionManager2 : ITransactionManager2
	{
		private readonly IActivityManager2 _activityManager;

		private ILogger _logger = NullLogger.Instance;

		public TransactionManager2(IActivityManager2 activityManager)
		{
			_activityManager = activityManager;
		}

		public ILogger Logger
		{
			get { return _logger; }
			set { _logger = value; }
		}

		public ITransaction2 CurrentTransaction
		{
			get
			{
				Activity2 activity;
				if (_activityManager.TryGetCurrentActivity(out activity))
					return activity.CurrentTransaction;
				return null;
			}
		}

		public ITransaction2 CreateTransaction(TransactionOptions transactionOptions)
		{
			var activity = _activityManager.EnsureActivityExists();

			var activityCount = activity.Count;

			TransactionImpl2 tx;
			if (activityCount == 0) // root transaction
			{
				

				var inner = new CommittableTransaction(new System.Transactions.TransactionOptions()
				{
					IsolationLevel = transactionOptions.IsolationLevel, 
					Timeout = transactionOptions.Timeout, 
				});

				var txScope = new TransactionScope(inner, TransactionScopeAsyncFlowOption.Enabled);

				tx = new TransactionImpl2(inner, txScope, activity, _logger.CreateChildLogger("TransactionRoot"));
			}
			else
			{
				throw new NotSupportedException("nesting transactions isnt supported yet");
			}

			if (Logger.IsDebugEnabled)
				Logger.Debug("Created ActivityCount = " + activityCount + ". Tx = " + tx.LocalIdentifier);

			return tx;
		}

		public void Dispose()
		{
		}
	}
}