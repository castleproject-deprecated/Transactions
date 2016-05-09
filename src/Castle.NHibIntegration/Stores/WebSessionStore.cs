namespace Castle.NHibIntegration.Stores
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using System.Web;
	using Core.Logging;
	using Internal;
	using MicroKernel.Facilities;

	public class WebSessionStore : ISessionStore
	{
		private readonly string _slotKey;
		private readonly string _statelessSlotKey;

		public WebSessionStore()
		{
			_slotKey = "nh.facility.stacks." + Guid.NewGuid();
			_statelessSlotKey = "nh.facility.stacks1." + Guid.NewGuid();
			
			Logger = NullLogger.Instance;
		}

		public ILogger Logger { get; set; }

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

		public int TotalStoredCurrent { get; private set; }

		public int TotalStatelessStoredCurrent { get; private set; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InternalStore<TSessionDel>(Dictionary<string, TSessionDel> dict, string @alias,
												BaseSessionDelegate session, out Action undoAction)
			where TSessionDel : BaseSessionDelegate
		{
			// session.Helper = new StackTrace().ToString();

			if (dict.ContainsKey(alias))
			{
				var msg =
					"alias already stored " + alias + " when trying to store " + session +
					" existing " + dict[alias] + " Thread " + Thread.CurrentThread.ManagedThreadId + " other was created " +
					dict[alias].Helper + " and this " + session.Helper;

				Logger.Warn(msg);
//				Console.WriteLine(msg);

				undoAction = () => { };
				return;
				// throw new Exception("alias already stored " + alias + " when trying to store " + session);
			}

			dict[alias] = session as TSessionDel;

			// Console.WriteLine("stored " + alias + " " + session + " Thread " + Thread.CurrentThread.ManagedThreadId);

			undoAction = () =>
			{
				var removed = dict.Remove(alias);

//				Logger.Debug("Store removing [" + alias + "] removed? " + removed);

			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Dictionary<string, SessionDelegate> GetDictSession()
		{
			var curContext = ObtainSessionContext();
			var dict = curContext.Items[_slotKey] as Dictionary<string, SessionDelegate>;
			if (dict == null)
			{
				dict = new Dictionary<string, SessionDelegate>(StringComparer.Ordinal);
				curContext.Items[_slotKey] = dict;
			}
			return dict;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Dictionary<string, StatelessSessionDelegate> GetDictStatelessSession()
		{
			var curContext = ObtainSessionContext();
			var dict = curContext.Items[_statelessSlotKey] as Dictionary<string, StatelessSessionDelegate>;
			if (dict == null)
			{
				dict = new Dictionary<string, StatelessSessionDelegate>(StringComparer.Ordinal);
				curContext.Items[_statelessSlotKey] = dict;
			}
			return dict;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static HttpContext ObtainSessionContext()
		{
			HttpContext curContext = HttpContext.Current;

			if (curContext == null)
			{
				throw new FacilityException("WebSessionStore: Could not obtain reference to HttpContext");
			}
			return curContext;
		}
	}
}