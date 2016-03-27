namespace Castle.NHibIntegration.Tests.Comps
{
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
