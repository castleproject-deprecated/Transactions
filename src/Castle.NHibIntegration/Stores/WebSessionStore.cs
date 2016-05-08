namespace Castle.NHibIntegration.Stores
{
	using System;
	using Internal;

	public class WebSessionStore : ISessionStore
	{
		public SessionDelegate FindCompatibleSession(string alias)
		{
			throw new NotImplementedException();
		}

		public StatelessSessionDelegate FindCompatibleStatelessSession(string alias)
		{
			return null;
		}

		public void Store(string alias, SessionDelegate session, out Action undoAction)
		{
			undoAction = null;

			throw new NotImplementedException();
		}

		public void Store(string alias, StatelessSessionDelegate session, out Action undoAction)
		{
			undoAction = null;
		}

//		public bool IsCurrentActivityEmptyFor(string alias)
//		{
//			throw new NotImplementedException();
//		}

//		public void Dispose()
//		{
//		}

		public int TotalStoredCurrent { get; private set; }

		public int TotalStatelessStoredCurrent { get; private set; }
	}
}