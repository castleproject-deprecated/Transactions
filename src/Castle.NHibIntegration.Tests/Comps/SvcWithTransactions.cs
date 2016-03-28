namespace Castle.NHibIntegration.Tests.Comps
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using Services.Transaction2;

	public class SvcWithTransactions
	{
		private readonly ISessionManager _sessionManager;

		public SvcWithTransactions(ISessionManager sessionManager)
		{
			_sessionManager = sessionManager;
		}

		[Transaction]
		public virtual void Sync()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				Child1();

				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual void SyncThatFails()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				Child2();
				var isOpen = sess.IsOpen;
			}
			throw new Exception("fake");
		}

		[Transaction]
		public virtual void SyncWithTwoConnections()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				AddToOracle();
				AddToMsSql();
				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual void SyncWithTwoConnectionsThatFails()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				AddToOracle();
				AddToMsSql();
				var isOpen = sess.IsOpen;
			}
			throw new Exception("fake");
		}

		[Transaction]
		public virtual async Task Async()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				await AChild1();
				await AChild1();

				var isOpen = sess.IsOpen;
			}
		}

		[Transaction]
		public virtual async Task Async2()
		{
			await AChild1();
			await AChild1();
		}

		[Transaction]
		public virtual async Task Async3()
		{
			await AChild2();
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

		private Task AChild2()
		{
			var tcs = new TaskCompletionSource<bool>();

			ThreadPool.QueueUserWorkItem((_) =>
			{
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

				sess.Save(new TestTable() {Id = Guid.NewGuid(), Counter = 1});

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
			}
		}
	}
}