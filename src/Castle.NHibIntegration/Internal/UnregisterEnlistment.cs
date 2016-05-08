namespace Castle.NHibIntegration.Internal
{
	using System.Transactions;
	using Core.Logging;

	class UnregisterEnlistment : IEnlistmentNotification
	{
		private readonly ILogger _logger;
		private readonly BaseSessionDelegate _session;

		public UnregisterEnlistment(ILogger logger, BaseSessionDelegate session)
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

			_session.UnsafeDispose(commit: true);
		}

		public void Rollback(Enlistment enlistment)
		{
			enlistment.Done();

			_session.UnsafeDispose(commit: false);
		}

		public void InDoubt(Enlistment enlistment)
		{
			enlistment.Done();

			_session.UnsafeDispose(commit: false);
		}
	}
}