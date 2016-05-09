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

		[SetUp]
		public void Init()
		{
			_actionBlock = new ActionBlock<string>((Action<string>) OnActionPosted);

		}

		[Test]
		public void Test1()
		{
			using (var transactionRoot = new System.Transactions.CommittableTransaction())
			{
				using (var txScope = new TransactionScope(transactionRoot, TransactionScopeAsyncFlowOption.Enabled))
				{
					var undoer = ExecutionContext.SuppressFlow();
					_actionBlock.Post("root1");
					_actionBlock.Post("root2");
					_actionBlock.Post("root3");
					_actionBlock.Post("root4");
					_actionBlock.Post("root5");
					_actionBlock.Post("root6");
					_actionBlock.Post("root7");
					_actionBlock.Post("root8");
					undoer.Undo();

					Thread.Sleep(100);

					txScope.Complete();
					// transactionRoot.Commit();
				}

				Console.WriteLine("root status " + transactionRoot.TransactionInformation.Status + " id " + transactionRoot.TransactionInformation.LocalIdentifier);
			}

			Console.WriteLine("Disposed root transaction");
			
			// Console.WriteLine(_confirmation);

			Thread.Sleep(1400);
		}

		private void OnActionPosted(string arg)
		{
			
			try
			{
				try
				{
					var current = System.Transactions.Transaction.Current;
					Console.WriteLine(arg + " " + current);

					Console.WriteLine("Has transaction? " + current);

					Thread.Sleep(2);

//				var confirmation = new EnlistedConfirmation();
//
//				using (var newRoot = new System.Transactions.CommittableTransaction())
//				{
//					using (var txScope = new TransactionScope(newRoot, TransactionScopeAsyncFlowOption.Enabled))
//					{
//						current = System.Transactions.Transaction.Current;
//						current.EnlistVolatile(confirmation, EnlistmentOptions.EnlistDuringPrepareRequired);
//
//						Console.WriteLine(arg + " new root " + current.TransactionInformation.Status + 
//										  " id " + current.TransactionInformation.LocalIdentifier);
//
//						txScope.Complete();
//					}
//					newRoot.Commit();
//				}
//
//				Console.WriteLine(confirmation);

//				Console.WriteLine(arg + " new root status " + current.TransactionInformation.Status + 
//								  " id " + current.TransactionInformation.LocalIdentifier);
				}
				catch (Exception ex)
				{
					Console.Error.WriteLine(ex);
				}
			}
			finally
			{
//				undoer.Undo();
			}

		}
	}
}