namespace Castle.Services.Transaction
{
	using System;

	public interface ITransaction2 : IDisposable
	{
		string LocalIdentifier { get; }

		TransactionState State { get; }

		System.Transactions.TransactionStatus? Status { get; }

		System.Transactions.Transaction Inner { get; }

		void Rollback();

		void Complete();
	}
}