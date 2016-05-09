namespace Castle.Services.Transaction2.Tests
{
	using System;
	using System.Threading;
	using System.Threading.Tasks.Dataflow;
	using System.Transactions;
	using Comps;
	using NUnit.Framework;

	[TestFixture, Explicit]
	public class TransactionScopesWithActionBlockExperimentTestCase
	{
		private ActionBlock<string> _actionBlock;
		private EnlistedConfirmation _confirmation;

		[SetUp]
		public void Init()
		{
			_actionBlock = new ActionBlock<string>((Action<string>) OnActionPosted);

			_confirmation = new EnlistedConfirmation();
		}

		[Test]
		public void Test1()
		{
			var transactionRoot = new System.Transactions.CommittableTransaction();
			using (var txScope = new TransactionScope(transactionRoot, TransactionScopeAsyncFlowOption.Enabled))
			{
				_actionBlock.Post("root");

				Thread.Sleep(1400);

				// txScope.Complete();
				// transactionRoot.Commit();
			}

			Console.WriteLine("root status " + transactionRoot.TransactionInformation.Status + " id " + transactionRoot.TransactionInformation.LocalIdentifier);

			Console.WriteLine(_confirmation);
		}

		private void OnActionPosted(string arg)
		{
			var current = System.Transactions.Transaction.Current;

			Console.WriteLine("Has transaction? " + current);

			var newRoot = new System.Transactions.CommittableTransaction();

			using (var txScope = new TransactionScope(newRoot, TransactionScopeAsyncFlowOption.Enabled))
			{
				current = System.Transactions.Transaction.Current;
				current.EnlistVolatile(_confirmation, EnlistmentOptions.EnlistDuringPrepareRequired);

				Console.WriteLine("new root " + current.TransactionInformation.Status + " id " + current.TransactionInformation.LocalIdentifier);

				txScope.Complete();
			}
			newRoot.Commit();

			Console.WriteLine("new root status " + current.TransactionInformation.Status + " id " + current.TransactionInformation.LocalIdentifier);
		}
	}
}