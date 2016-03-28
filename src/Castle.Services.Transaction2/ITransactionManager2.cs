namespace Castle.Services.Transaction
{
	using System;

	public interface ITransactionManager2 : IDisposable
	{
		ITransaction2 CurrentTransaction { get; }

		ITransaction2 CreateTransaction(TransactionOptions transactionOptions);

		event Action<ITransaction2> TransactionCreated;
	}
}