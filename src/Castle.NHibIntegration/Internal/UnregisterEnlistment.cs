namespace Castle.NHibIntegration.Internal
{
	using System.Transactions;
	using Core.Logging;

	class UnregisterEnlistment : IEnlistmentNotification
	{
		private readonly ILogger _logger;
		private readonly SessionDelegate _session;

		public UnregisterEnlistment(ILogger logger, SessionDelegate session)
		{
			_logger = logger;
			_session = session;
		}

		public void Prepare(PreparingEnlistment preparingEnlistment)
		{
			preparingEnlistment.Prepared();
		}

		public void Commit(Enlistment enlistment)
		{
			enlistment.Done();

			_session.UnsafeDispose();
		}

		public void Rollback(Enlistment enlistment)
		{
			enlistment.Done();

			_session.UnsafeDispose();
		}

		public void InDoubt(Enlistment enlistment)
		{
			enlistment.Done();

			_session.UnsafeDispose();
		}
	}
}