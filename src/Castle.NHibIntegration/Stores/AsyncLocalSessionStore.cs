namespace Castle.NHibIntegration.Stores
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Internal;

	class AsyncLocalSessionStore : ISessionStore
	{
		private AsyncLocal<Dictionary<string, SessionDelegate>> _local;
		private int _stored;

		public AsyncLocalSessionStore()
		{
			_local = new AsyncLocal<Dictionary<string, SessionDelegate>>();
		}

		public int TotalStoredCurrent { get {return _stored; } }

		public SessionDelegate FindCompatibleSession(string alias)
		{
			var dict = GetDict();

			SessionDelegate sessDel;
			dict.TryGetValue(alias, out sessDel);
			return sessDel;
		}

		public void Store(string alias, SessionDelegate session, out Action undoAction)
		{
			var dict = GetDict();

			if (dict.ContainsKey(alias)) throw new Exception("alias already stored " + alias);

			dict[alias] = session;

			Interlocked.Increment(ref _stored);

			undoAction = () =>
			{
				if (dict.Remove(alias))
				{
					Interlocked.Decrement(ref _stored);
				}
			};
		}

//		public void Remove(string alias, SessionDelegate session)
//		{
//			var dict = GetDict();
//
//			if (dict.Remove(alias))
//			{
//				Interlocked.Decrement(ref _stored);
//			}
//		}

		public bool IsCurrentActivityEmptyFor(string alias)
		{
			return GetDict().Count == 0;
		}

		public void Dispose()
		{
		}

		internal Dictionary<string, SessionDelegate> GetDict()
		{
			if (_local.Value == null)
			{
				_local.Value = new Dictionary<string, SessionDelegate>(StringComparer.Ordinal);
			}

			return _local.Value;
		} 
	}
}
