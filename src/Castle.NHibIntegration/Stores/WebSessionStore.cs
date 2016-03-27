namespace Castle.NHibIntegration.Stores
{
	using Internal;

	class WebSessionStore : ISessionStore
	{
		public SessionDelegate FindCompatibleSession(string alias)
		{
			return null;
		}

		public void Store(string alias, SessionDelegate session)
		{
		}

		public void Remove(string alias, SessionDelegate session)
		{
		}

		public bool IsCurrentActivityEmptyFor(string alias)
		{
			return false;
		}

		public void Dispose()
		{
		}
	}
}