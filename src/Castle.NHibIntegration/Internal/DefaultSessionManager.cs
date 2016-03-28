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

				wrapped = WrapSession(alias, session, currentTransaction, canClose: currentTransaction == null);
				EnlistIfNecessary(currentTransaction, wrapped, weAreSessionOwner: true);

				if (Logger.IsDebugEnabled) Logger.Debug("Created Session = [" + wrapped + "]");

				// _sessionStore.Store(alias, wrapped);
				// wrapped.Store();
				Store(alias, wrapped, currentTransaction);

				if (Logger.IsDebugEnabled) Logger.Debug("Wrapped Session = [" + wrapped + "]");
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
					return (SessionDelegate) instance;
				}
			}
			return sessionStore.FindCompatibleSession(@alias);
		}

		private static void Store(string @alias, SessionDelegate wrapped, ITransaction2 transaction)
		{
			if (transaction != null)
			{
				if (transaction.UserData.ContainsKey(@alias)) throw new Exception("Key already exists for " + @alias);

				transaction.UserData[@alias] = wrapped;

				return;
			}

			wrapped.Store();
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

		private ISession InternalCreateSession(string @alias)
		{
			ISessionFactory sessionFactory = _factoryResolver.GetSessionFactory(@alias);

			if (sessionFactory == null)
			{
				throw new FacilityException("No ISessionFactory implementation " +
											"associated with the given alias: " + @alias);
			}

			ISession session;

			{
				session = sessionFactory.OpenSession();
			}

			session.FlushMode = defaultFlushMode;

			return session;
		}
	}
}