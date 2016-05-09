namespace Castle.Services.Transaction2.Tests.Comps
{
	using System.Transactions;

	public class EnlistedConfirmation : System.Transactions.IEnlistmentNotification
	{
		public int Prepared, Committed, RolledBack, InDoubtSet;

		public void Prepare(PreparingEnlistment preparingEnlistment)
		{
			Prepared++;

			preparingEnlistment.Prepared();
		}

		public void Commit(Enlistment enlistment)
		{
			Committed++;

			enlistment.Done();
		}

		public void Rollback(Enlistment enlistment)
		{
			RolledBack++;

			enlistment.Done();
		}

		public void InDoubt(Enlistment enlistment)
		{
			InDoubtSet++;

			enlistment.Done();
		}

		public override string ToString()
		{
			return "EnlistedConfirmation :" + 
				" Committed: " + Committed +
				" Rolledback: " + RolledBack +
				" InDoubtSet: " + InDoubtSet +
				" Prepared: " + Prepared;
		}
	}
}