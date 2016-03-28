namespace Castle.Services.Transaction
{
	using System;
	using System.Transactions;

	public class TransactionOptions
	{
		public static readonly TransactionOptions RequiresNewReadCommitted = new TransactionOptions
		{
			IsolationLevel = IsolationLevel.ReadCommitted, 
			Mode = TransactionScopeOption.Required,
			Timeout = TimeSpan.Zero
		};

		public IsolationLevel IsolationLevel { get; set; }

		public TransactionScopeOption Mode { get; set; }

		public TimeSpan Timeout { get; set; }

		// public DependentCloneOption DependentOption { get; set; }
	}
}