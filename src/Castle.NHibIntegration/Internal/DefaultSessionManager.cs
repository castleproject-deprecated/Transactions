namespace Castle.NHibIntegration.Internal
{
	using System;
	using System.Transactions;
	using Core.Logging;
	using MicroKernel.Facilities;
	using NHibernate;
	using Services.Transaction;


	public class DefaultSessionManager : ISessionManager
	{
		private readonly ISessionStore _sessionStore;
		private readonly ISessionFactoryResolver _factoryResolver;
		private readonly ITransactionManager2 _transactionManager;
		private FlushMode defaultFlushMode = FlushMode.Auto;

		public DefaultSessionManager(ISessionStore sessionStore, 
									 ISessionFactoryResolver factoryResolver, 
									 ITransactionManager2 transactionManager)
		{
			_sessionStore = sessionStore;
			_factoryResolver = factoryResolver;
			_transactionManager = transactionManager;

			Logger = NullLogger.Instance;
		}
		
		/// <summary>
		/// The flushmode the created session gets
		/// </summary>
		/// <value></value>
		public FlushMode DefaultFlushMode
		{
			get { return defaultFlushMode; }
			set { defaultFlushMode = value; }
		}

		public ILogger Logger { get; set; }

		public IStatelessSession OpenStatelessSession()
		{
			return OpenStatelessSession(NhConstants.DefaultAlias);
		}

		public IStatelessSession OpenStatelessSession(string alias)
		{
			if (string.IsNullOrEmpty(alias)) throw new ArgumentNullException("alias");

			ITransaction2 currentTransaction = _transactionManager.CurrentTransaction;

			StatelessSessionDelegate wrapped = FindCompatibleStateless(alias, currentTransaction, _sessionStore);

			if (wrapped == null) // || (currentTransaction != null && !wrapped.Transaction.IsActive))
			{
				var session = InternalCreateStatelessSession(alias);

				var newWrapped = WrapSession(alias, session, currentTransaction, canClose: currentTransaction == null);
				EnlistIfNecessary(currentTransaction, newWrapped, weAreSessionOwner: true);

				if (Logger.IsDebugEnabled) Logger.Debug("Created stateless Session = [" + newWrapped + "]");

				Store(alias, newWrapped, currentTransaction);

				// if (Logger.IsDebugEnabled) Logger.Debug("Wrapped Session = [" + newWrapped + "]");

				wrapped = newWrapped;
			}
			else
			{
				if (Logger.IsDebugEnabled) Logger.Debug("Re-wrapping stateless Session = [" + wrapped + "]");

				wrapped = WrapSession(alias, wrapped.InnerSession, null, canClose: false);
				// EnlistIfNecessary(currentTransaction, wrapped, weAreSessionOwner: false);
			}

			return wrapped;
		}

		public ISession OpenSession()
		{
			return OpenSession(NhConstants.DefaultAlias);
		}

		public ISession OpenSession(string alias)
		{
			if (string.IsNullOrEmpty(alias)) throw new ArgumentNullException("alias");

			ITransaction2 currentTransaction = _transactionManager.CurrentTransaction;

			SessionDelegate wrapped = FindCompatible(alias, currentTransaction, _sessionStore);

			if (wrapped == null) // || (currentTransaction != null && !wrapped.Transaction.IsActive))
			{
				var session = InternalCreateSession(alias);

				var newWrapped = WrapSession(alias, session, currentTransaction, canClose: currentTransaction == null);
				EnlistIfNecessary(currentTransaction, newWrapped, weAreSessionOwner: true);

				if (Logger.IsDebugEnabled) Logger.Debug("Created Session = [" + newWrapped + "]");

				Store(alias, newWrapped, currentTransaction);

				// if (Logger.IsDebugEnabled) Logger.Debug("Wrapped Session = [" + newWrapped + "]");

				wrapped = newWrapped;
			}
			else
			{
				if (Logger.IsDebugEnabled) Logger.Debug("Re-wrapping Session = [" + wrapped + "]");

				wrapped = WrapSession(alias, wrapped.InnerSession, null, canClose: false);
				// EnlistIfNecessary(currentTransaction, wrapped, weAreSessionOwner: false);
			}

			return wrapped;
		}

		private static SessionDelegate FindCompatible(string @alias, ITransaction2 transaction, ISessionStore sessionStore)
		{
			if (transaction != null)
			{
				object instance;
				if (transaction.UserData.TryGetValue(@alias, out instance))
				{
					var stateful = instance as SessionDelegate;
					if (stateful != null) return stateful;
				}
			}
			return sessionStore.FindCompatibleSession(@alias);
		}

		private static StatelessSessionDelegate FindCompatibleStateless(string @alias, ITransaction2 transaction, ISessionStore sessionStore)
		{
			if (transaction != null)
			{
				object instance;
				if (transaction.UserData.TryGetValue(@alias, out instance))
				{
					var stateless = instance as StatelessSessionDelegate;
					if (stateless != null) return stateless;
				}
			}
			return sessionStore.FindCompatibleStatelessSession(@alias);
		}

		private static void Store(string @alias, BaseSessionDelegate wrapped, ITransaction2 transaction)
		{
			if (transaction != null)
			{
				if (transaction.UserData.ContainsKey(@alias)) throw new Exception("Key already exists for " + @alias);

				transaction.UserData[@alias] = wrapped;
			}
			else
			{
				wrapped.Store();
			}
		}

		private void EnlistIfNecessary(ITransaction2 transaction, SessionDelegate session, bool weAreSessionOwner)
		{
			if (transaction == null) return;

			if (weAreSessionOwner /*&& session.Transaction.IsActive*/)
			{
				Logger.Debug("Enlisted Session " + session);

				var ue = new UnregisterEnlistment(Logger, session);

				transaction.Inner.EnlistVolatile(ue, EnlistmentOptions.EnlistDuringPrepareRequired);
			}
		}

		private void EnlistIfNecessary(ITransaction2 transaction, StatelessSessionDelegate session, bool weAreSessionOwner)
		{
			if (transaction == null) return;

			if (weAreSessionOwner /*&& session.Transaction.IsActive*/)
			{
				Logger.Debug("Enlisted stateless Session " + session);

				var ue = new UnregisterEnlistment(Logger, session);

				transaction.Inner.EnlistVolatile(ue, EnlistmentOptions.EnlistDuringPrepareRequired);
			}
		}

		private SessionDelegate WrapSession(string alias, ISession session, 
											ITransaction2 currentTransaction, 
											bool canClose)
		{
			var sessdelegate = new SessionDelegate(alias, canClose, session, _sessionStore, this.Logger.CreateChildLogger("Session"));

			if (currentTransaction != null)
			{
				sessdelegate.InternalBeginTransaction();
			}

			return sessdelegate;
		}

		private StatelessSessionDelegate WrapSession(string alias, IStatelessSession session,
													 ITransaction2 currentTransaction,
													 bool canClose)
		{
			var sessdelegate = new StatelessSessionDelegate(alias, canClose, session, _sessionStore, this.Logger.CreateChildLogger("StatelessSession"));

			if (currentTransaction != null)
			{
				sessdelegate.InternalBeginTransaction();
			}

			return sessdelegate;
		}

		private ISession InternalCreateSession(string @alias)
		{
			ISessionFactory sessionFactory = _factoryResolver.GetSessionFactory(@alias);

			if (sessionFactory == null)
			{
				throw new FacilityException("No ISessionFactory implementation " +
											"associated with the given alias: " + @alias);
			}

			ISession session = sessionFactory.OpenSession();

			session.FlushMode = defaultFlushMode;

			return session;
		}

		private IStatelessSession InternalCreateStatelessSession(string @alias)
		{
			ISessionFactory sessionFactory = _factoryResolver.GetSessionFactory(@alias);

			if (sessionFactory == null)
			{
				throw new FacilityException("No ISessionFactory implementation " +
											"associated with the given alias: " + @alias);
			}

			IStatelessSession session = sessionFactory.OpenStatelessSession();

			return session;
		}
	}
}