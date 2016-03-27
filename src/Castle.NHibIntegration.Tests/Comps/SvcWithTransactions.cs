namespace Castle.NHibIntegration.Tests.Comps
{
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