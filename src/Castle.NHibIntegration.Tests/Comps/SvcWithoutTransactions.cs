namespace Castle.NHibIntegration.Tests.Comps
{
	using System.Threading;
	using System.Threading.Tasks;

	public class SvcWithoutTransactions
	{
		private readonly ISessionManager _sessionManager;

		public SvcWithoutTransactions(ISessionManager sessionManager)
		{
			_sessionManager = sessionManager;
		}

		public void Sync()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				Child1();

				var isOpen = sess.IsOpen;
			}
		}

		public async Task Async()
		{
			using (var sess = _sessionManager.OpenSession())
			{
				await AChild1();
				await AChild1();

				var isOpen = sess.IsOpen;
			}
		}

		public async Task Async2()
		{
			await AChild1();
			await AChild1();
		}

		public async Task Async3()
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
			}
		}
	}
}
