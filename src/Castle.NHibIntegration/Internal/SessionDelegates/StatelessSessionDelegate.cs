namespace Castle.NHibIntegration.Internal
{
	using System;
	using System.Data;
	using System.Linq.Expressions;
	using Core.Logging;
	using NHibernate;


	public class StatelessSessionDelegate : BaseSessionDelegate, IStatelessSession
	{
		private readonly IStatelessSession inner;

		public StatelessSessionDelegate(string @alias, bool canClose, IStatelessSession inner, ISessionStore sessionStore, ILogger logger) :
			base(alias, canClose, inner.GetSessionImplementation().SessionId, sessionStore, logger)
		{
			this.inner = inner;
		}

		public IStatelessSession InnerSession
		{
			get { return inner; }
		}

		public override void Store()
		{
			sessionStore.Store(this._alias, this, out this.removeFromStore);
		}

		public override void InternalBeginTransaction()
		{
			_tx = inner.BeginTransaction();
		}

		protected override void InnerDispose()
		{
			this.inner.Dispose();
		}

		protected override void InnerClose()
		{
			this.inner.Close();
		}

		public override string ToString()
		{
			return "SessionDelegate for stateless session " + _sessionId;
		}

		#region IStatelessSession delegation

		public bool IsOpen
		{
			get
			{
				EnsureNotDisposed();
				return inner.IsOpen;
			}
		}

		public bool IsConnected { get { return inner.IsConnected; } }

		public ITransaction Transaction { get { return this.inner.Transaction; } }

		public IDbConnection Connection
		{
			get
			{
				EnsureNotDisposed();
				return inner.Connection;
			}
		}

		public void Close()
		{
			EnsureNotDisposed();
			DoClose(closing: true);
		}

		public NHibernate.Engine.ISessionImplementor GetSessionImplementation()
		{
			EnsureNotDisposed();
			return inner.GetSessionImplementation();
		}

		public object Insert(object entity)
		{
			EnsureNotDisposed();
			return inner.Insert(entity);
		}

		public object Insert(string entityName, object entity)
		{
			EnsureNotDisposed();
			return inner.Insert(entityName, entity);
		}

		public void Update(object entity)
		{
			EnsureNotDisposed();
			inner.Update(entity);
		}

		public void Update(string entityName, object entity)
		{
			EnsureNotDisposed();
			inner.Update(entityName, entity);
		}

		public void Delete(object entity)
		{
			EnsureNotDisposed();
			inner.Delete(entity);
		}

		public void Delete(string entityName, object entity)
		{
			EnsureNotDisposed();
			inner.Delete(entityName, entity);
		}

		public object Get(string entityName, object id)
		{
			EnsureNotDisposed();
			return inner.Get(entityName, id);
		}

		public T Get<T>(object id)
		{
			EnsureNotDisposed();
			return inner.Get<T>(id);
		}

		public object Get(string entityName, object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Get(entityName, id, lockMode);
		}

		public T Get<T>(object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Get<T>(id, lockMode);
		}

		public void Refresh(object entity)
		{
			EnsureNotDisposed();
			inner.Refresh(entity);
		}

		public void Refresh(string entityName, object entity)
		{
			EnsureNotDisposed();
			inner.Refresh(entity);
		}

		public void Refresh(object entity, LockMode lockMode)
		{
			EnsureNotDisposed();
			inner.Refresh(entity, lockMode);
		}

		public void Refresh(string entityName, object entity, LockMode lockMode)
		{
			EnsureNotDisposed();
			inner.Refresh(entityName, entityName, lockMode);
		}

		public IQuery CreateQuery(string queryString)
		{
			EnsureNotDisposed();
			return inner.CreateQuery(queryString);
		}

		public IQuery GetNamedQuery(string queryName)
		{
			EnsureNotDisposed();
			return inner.GetNamedQuery(queryName);
		}

		public ICriteria CreateCriteria<T>() where T : class
		{
			EnsureNotDisposed();
			return inner.CreateCriteria<T>();
		}

		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			EnsureNotDisposed();
			return inner.CreateCriteria<T>(alias);
		}

		public ICriteria CreateCriteria(Type entityType)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(entityType);
		}

		public ICriteria CreateCriteria(Type entityType, string alias)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(entityType, alias);
		}

		public ICriteria CreateCriteria(string entityName)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(entityName);
		}

		public ICriteria CreateCriteria(string entityName, string alias)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(entityName, alias);
		}

		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			EnsureNotDisposed();
			return inner.QueryOver<T>();
		}

		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			EnsureNotDisposed();
			return inner.QueryOver<T>(alias);
		}

		public ISQLQuery CreateSQLQuery(string queryString)
		{
			EnsureNotDisposed();
			return inner.CreateSQLQuery(queryString);
		}

		public ITransaction BeginTransaction()
		{
			EnsureNotDisposed();
			return inner.BeginTransaction();
		}

		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			EnsureNotDisposed();
			return inner.BeginTransaction(isolationLevel);
		}

		public IStatelessSession SetBatchSize(int batchSize)
		{
			EnsureNotDisposed();
			return inner.SetBatchSize(batchSize);
		}

		#endregion
	}
}