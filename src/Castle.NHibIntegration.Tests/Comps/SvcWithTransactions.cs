namespace Castle.NHibIntegration.Tests.Comps
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Transactions;
	using Castle.Services.Transaction;


	public class SvcWithTransactions
	{
		private readonly ISessionManager _sessionManager;

		public SvcWithTransactions(ISessionManager sessionManager)
		{
			_sessionManager = sessionManager;
		}

		[Transaction]
		public virtual void Sync(EnlistedConfirmation confirmation)
		{
			System.Transactions.Transaction.Current.EnlistVolatile(confirmation, EnlistmentOptions.None);

			using (var sess = _sessionManager.OpenSession())
			{
				Child1();

				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual void SyncThatFails(EnlistedConfirmation confirmation)
		{
			System.Transactions.Transaction.Current.EnlistVolatile(confirmation, EnlistmentOptions.None);

			using (var sess = _sessionManager.OpenSession())
			{
				Child2();
				var isOpen = sess.IsOpen;
			}
			throw new Exception("fake");
		}

		[Transaction]
		public virtual void SyncWithTwoConnections(EnlistedConfirmation confirmation)
		{
			System.Transactions.Transaction.Current.EnlistVolatile(confirmation, EnlistmentOptions.None);

			using (var sess = _sessionManager.OpenSession())
			{
				AddToOracle();
				AddToMsSql();
				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual void SyncWithTwoConnectionsThatFails(bool withRootSession)
		{
			if (withRootSession)
			{
				using (var sess = _sessionManager.OpenSession())
				{
					AddToOracle();
					AddToMsSql();
					var isOpen = sess.IsOpen;
				}
			}
			else
			{
				AddToOracle();
				AddToMsSql();
			}
			
			throw new Exception("fake");
		}

		[Transaction]
		public virtual async Task AsyncCompletingSync(EnlistedConfirmation confirmation)
		{
			System.Transactions.Transaction.Current.EnlistVolatile(confirmation, EnlistmentOptions.None);
		
			using (var sess = _sessionManager.OpenSession())
			{
				await AChild1();
				await AChild1();

				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual async Task Async2CompletingSyncWithoutRootSession()
		{
			await AChild1();
			await AChild1();
		}

		[Transaction]
		public virtual async Task Async3CompletingAsync()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				await LateComplete();

				AddToOracle();

				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual async Task Async4CompletingAsyncWithoutRoot()
		{
			await LateComplete();

			AddToOracle();
		}

		[Transaction]
		public virtual Task AsyncWithTwoDbsCompletingSync()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				AddToOracle();
				AddToMsSql();

				var isOpen = sess.IsOpen;
			}
			return Task.CompletedTask;
		}

		[Transaction]
		public virtual Task AsyncWithTwoDbsCompletingSync2()
		{
			AddToOracle();
			AddToMsSql();

			return Task.CompletedTask;
		}

		[Transaction]
		public virtual Task AsyncWithLateFaultedTask()
		{
			AddToOracle();

			var tcs = new TaskCompletionSource<bool>();

			ThreadPool.QueueUserWorkItem((_) =>
			{
				Thread.Sleep(100);
				using (var sess = _sessionManager.OpenSession())
				{
					tcs.SetException(new Exception("fake"));
				}
			});

			return tcs.Task;
		}

		[Transaction]
		public virtual async Task AsyncWithTwoDbsCompletingASync(bool shouldFail)
		{
			using (var sess = _sessionManager.OpenSession())
			{
				await LateComplete();

				AddToOracle();
				AddToMsSql();

				var isOpen = sess.IsOpen;
			}

			if (shouldFail) throw new Exception("fake");
		}


		private Task AChild1()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				Child2();

				var isOpen = sess.IsOpen;
			}

			return Task.CompletedTask;
		}

		private Task LateComplete()
		{
			var tcs = new TaskCompletionSource<bool>();

			ThreadPool.QueueUserWorkItem((_) =>
			{
				Thread.Sleep(100);
				using (var sess = _sessionManager.OpenSession())
				{
					tcs.SetResult(true);
				}
			});

			return tcs.Task;
		}

		private void Child1()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				Child2();

				var isOpen = sess.IsOpen;
			}
		}

		private void Child2()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				var isOpen = sess.IsOpen;

				sess.Save(new TestTable {Id = Guid.NewGuid(), Counter = 1});

				sess.Flush();
			}
		}

		private void AddToOracle()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				var isOpen = sess.IsOpen;

				sess.Save(new TestTable() { Id = Guid.NewGuid(), Counter = 1 });

				sess.Flush();
			}
		}

		private void AddToMsSql()
		{
			using (var sess = _sessionManager.OpenSession("mssql"))
			{
				var isOpen = sess.IsOpen;

				sess.Save(new TestTable2() { Id = Guid.NewGuid(), Counter = 1 });

				sess.Flush();

				sess.Transaction.Commit();
			}
		}
	}
}