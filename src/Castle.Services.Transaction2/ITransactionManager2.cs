namespace Castle.Services.Transaction
{
	using System;
	using System.Transactions;

	public interface ITransactionManager2 : IDisposable
	{
		ITransaction2 CurrentTransaction { get; }

		ITransaction2 CreateTransaction(TransactionOptions transactionOptions);
	}

	public class TransactionOptions
	{
		public static readonly TransactionOptions RequiresNewReadCommitted = new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted, Mode = TransactionScopeOption.Required };

		public TransactionOptions()
		{
		}

		public IsolationLevel IsolationLevel { get; set; }

		public TransactionScopeOption Mode { get; set; }

		public TimeSpan Timeout { get; set; }

		// public DependentCloneOption DependentOption { get; set; }
	}
}