namespace Castle.NHibIntegration.Stores
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Core.Logging;
	using Internal;

	public class AsyncLocalSessionStore : ISessionStore
	{
		private readonly AsyncLocal<Dictionary<string, SessionDelegate>> _localSession;
		private readonly AsyncLocal<Dictionary<string, StatelessSessionDelegate>> _localStatelessSession;
		
		private int _stored;
		private int _storedStateless;

		public AsyncLocalSessionStore()
		{
			_localSession = new AsyncLocal<Dictionary<string, SessionDelegate>>();
			_localStatelessSession = new AsyncLocal<Dictionary<string, StatelessSessionDelegate>>();

			Logger = NullLogger.Instance;
		}

		public ILogger Logger { get; set; }

		public int TotalStoredCurrent { get { return _stored; } }
		public int TotalStatelessStoredCurrent { get { return _storedStateless; } }

		public SessionDelegate FindCompatibleSession(string alias)
		{
			var dict = GetDictSession();

			SessionDelegate sessDel;
			dict.TryGetValue(alias, out sessDel);
			return sessDel;
		}

		public StatelessSessionDelegate FindCompatibleStatelessSession(string alias)
		{
			var dict = GetDictStatelessSession();

			StatelessSessionDelegate sessDel;
			dict.TryGetValue(alias, out sessDel);
			return sessDel;
		}

		public void Store(string alias, SessionDelegate session, out Action undoAction)
		{
			InternalStore<SessionDelegate>(GetDictSession(), alias, session, out undoAction);
		}

		public void Store(string alias, StatelessSessionDelegate session, out Action undoAction)
		{
			InternalStore<StatelessSessionDelegate>(GetDictStatelessSession(), alias, session, out undoAction);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InternalStore<TSessionDel>(Dictionary<string, TSessionDel> dict, string @alias,
												BaseSessionDelegate session, out Action undoAction)
			where TSessionDel : BaseSessionDelegate
		{
			session.Helper = new StackTrace().ToString();

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

			dict[alias] = session as TSessionDel;

			if (typeof(TSessionDel) == typeof(SessionDelegate))
				Interlocked.Increment(ref _stored);
			else
				Interlocked.Increment(ref _storedStateless);

			// Console.WriteLine("stored " + alias + " " + session + " Thread " + Thread.CurrentThread.ManagedThreadId);

			undoAction = () =>
			{
				var removed = dict.Remove(alias);

				Logger.Debug("Store removing [" + alias + "] removed? " + removed);

				if (removed)
				{
					if (typeof(TSessionDel) == typeof(SessionDelegate))
						Interlocked.Decrement(ref _stored);
					else
						Interlocked.Decrement(ref _storedStateless);

					// Console.WriteLine("removed " + alias + " " + session + " Thread " + Thread.CurrentThread.ManagedThreadId);
				}
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Dictionary<string, SessionDelegate> GetDictSession()
		{
			if (_localSession.Value == null)
			{
				_localSession.Value = new Dictionary<string, SessionDelegate>(StringComparer.Ordinal);
			}
			return _localSession.Value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Dictionary<string, StatelessSessionDelegate> GetDictStatelessSession()
		{
			if (_localStatelessSession.Value == null)
			{
				_localStatelessSession.Value = new Dictionary<string, StatelessSessionDelegate>(StringComparer.Ordinal);
			}
			return _localStatelessSession.Value;
		}
	}
}
