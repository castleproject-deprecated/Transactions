namespace Castle.NHibIntegration.Stores
{
	using System;
	using Internal;

	class WebSessionStore : ISessionStore
	{
		public SessionDelegate FindCompatibleSession(string alias)
		{
			return null;
		}

		public void Store(string alias, SessionDelegate session, out Action undoAction)
		{
			undoAction = null;
		}

//		public void Remove(string alias, SessionDelegate session)
//		{
//		}

		public bool IsCurrentActivityEmptyFor(string alias)
		{
			return false;
		}

		public void Dispose()
		{
		}

		public int TotalStoredCurrent { get; private set; }
	}
}