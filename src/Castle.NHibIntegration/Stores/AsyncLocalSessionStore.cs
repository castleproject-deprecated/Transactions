namespace Castle.NHibIntegration.Stores
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Threading;
	using Core.Logging;
	using Internal;

	class AsyncLocalSessionStore : ISessionStore
	{
		private readonly AsyncLocal<Dictionary<string, SessionDelegate>> _local;
		private int _stored;

		public AsyncLocalSessionStore()
		{
			_local = new AsyncLocal<Dictionary<string, SessionDelegate>>();
			Logger = NullLogger.Instance;
		}

		public ILogger Logger { get; set; }

		public int TotalStoredCurrent { get { return _stored; } }

		public SessionDelegate FindCompatibleSession(string alias)
		{
			var dict = GetDict();

			SessionDelegate sessDel;
			dict.TryGetValue(alias, out sessDel);
			return sessDel;
		}

		public void Store(string alias, SessionDelegate session, out Action undoAction)
		{
			session.Helper = new StackTrace().ToString();

			var dict = GetDict();

			if (dict.ContainsKey(alias))
			{
				var msg =
					"alias already stored " + alias + " when trying to store " + session +
					" existing " + dict[alias] + " Thread " + Thread.CurrentThread.ManagedThreadId + " other was created " +
					dict[alias].Helper + " and this " + session.Helper;

				Logger.Warn(msg);
				Console.WriteLine(msg);

				undoAction = () => { };
				return;
				// throw new Exception("alias already stored " + alias + " when trying to store " + session);
			}

			dict[alias] = session;

			Interlocked.Increment(ref _stored);
			// Console.WriteLine("stored " + alias + " " + session + " Thread " + Thread.CurrentThread.ManagedThreadId);

			undoAction = () =>
			{
				var removed = dict.Remove(alias);

				Logger.Debug("Store removing [" + alias + "] removed? " + removed);

				if (removed)
				{
					Interlocked.Decrement(ref _stored);
					// Console.WriteLine("removed " + alias + " " + session + " Thread " + Thread.CurrentThread.ManagedThreadId);
				}
			};
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
