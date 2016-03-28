namespace Castle.NHibIntegration.Stores
{
	using System;
	using Internal;

	class WebSessionStore : ISessionStore
	{
		public SessionDelegate FindCompatibleSession(string alias)
		{
			throw new NotImplementedException();
		}

		public void Store(string alias, SessionDelegate session, out Action undoAction)
		{
			undoAction = null;

			throw new NotImplementedException();
		}

//		public void Remove(string alias, SessionDelegate session)
//		{
//		}

		public bool IsCurrentActivityEmptyFor(string alias)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
		}

		public int TotalStoredCurrent { get; private set; }
	}
}