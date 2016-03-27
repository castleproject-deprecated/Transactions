namespace Castle.NHibIntegration.Stores
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using Internal;

	class AsyncLocalSessionStore : ISessionStore
	{
		private AsyncLocal<Dictionary<string, SessionDelegate>> _local; 

		public AsyncLocalSessionStore()
		{
			_local = new AsyncLocal<Dictionary<string, SessionDelegate>>();
		}

		public SessionDelegate FindCompatibleSession(string alias)
		{
			var dict = GetDict();

			SessionDelegate sessDel;
			dict.TryGetValue(alias, out sessDel);
			return sessDel;
		}

		public void Store(string alias, SessionDelegate session)
		{
			var dict = GetDict();

			if (dict.ContainsKey(alias)) throw new Exception("alias already stored " + alias);
			dict[alias] = session;
		}

		public void Remove(string alias, SessionDelegate session)
		{
			var dict = GetDict();

			dict.Remove(alias);
		}

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
