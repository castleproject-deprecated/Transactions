namespace Castle.Services.Transaction
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public interface ITransaction2 : IDisposable
	{
		string LocalIdentifier { get; }

		TransactionState State { get; }

		System.Transactions.TransactionStatus? Status { get; }

		System.Transactions.Transaction Inner { get; }

		IDictionary<string, object> UserData { get; }

		void Rollback();

		void Complete();
	}
}