namespace Castle.NHibIntegration.Internal
{
	using System;
	using System.Collections;
	using System.Data;
	using System.Linq.Expressions;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Core.Logging;
	using NHibernate;
	using NHibernate.Stat;
	using NHibernate.Type;

	/// <summary>
	/// Proxies an ISession so the user cannot close a session which
	/// is controlled by a transaction, or, when this is not the case, 
	/// make sure to remove the session from the storage.
	/// <seealso cref="ISessionStore"/>
	/// <seealso cref="ISessionManager"/>
	/// </summary>
	[Serializable]
	public class SessionDelegate : MarshalByRefObject, ISession
	{
		private readonly string _alias;
		private readonly ISession inner;
		private readonly ISessionStore sessionStore;
		private readonly ILogger _logger;
		private readonly bool canClose;
		private object cookie;
		private bool _disposed;
		private Action removeFromStore;
		private ITransaction _tx;
		private Guid _sessionId;
		public string Helper;

		/// <summary>
		/// Initializes a new instance of the <see cref="SessionDelegate"/> class.
		/// </summary>
		/// <param name="canClose">if set to <c>true</c> [can close].</param>
		/// <param name="inner">The inner.</param>
		/// <param name="sessionStore">The session store.</param>
		/// <param name="logger"></param>
		public SessionDelegate(string @alias, bool canClose, ISession inner, ISessionStore sessionStore, ILogger logger)
		{
			_sessionId = inner.GetSessionImplementation().SessionId;

			this.inner = inner;
			this.sessionStore = sessionStore;
			this.canClose = canClose;
			_alias = alias;
			_logger = logger;
		}

		/// <summary>
		/// Gets the inner session.
		/// </summary>
		/// <value>The inner session.</value>
		public ISession InnerSession
		{
			get { return inner; }
		}

		/// <summary>
		/// Gets or sets the session store cookie.
		/// </summary>
		/// <value>The session store cookie.</value>
		public object SessionStoreCookie
		{
			get { return cookie; }
			set { cookie = value; }
		}

		#region ISession delegation

		/// <summary>
		/// Determines at which points Hibernate automatically flushes the session.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// For a readonly session, it is reasonable to set the flush mode to <c>FlushMode.Never</c>
		/// at the start of the session (in order to achieve some extra performance).
		/// </remarks>
		public FlushMode FlushMode
		{
			get { return inner.FlushMode; }
			set { inner.FlushMode = value; }
		}

		/// <summary>
		/// Get the <see cref="T:NHibernate.ISessionFactory"/> that created this instance.
		/// </summary>
		/// <value></value>
		public ISessionFactory SessionFactory
		{
			get { return inner.SessionFactory; }
		}

		/// <summary>
		/// Gets the ADO.NET connection.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// Applications are responsible for calling commit/rollback upon the connection before
		/// closing the <c>ISession</c>.
		/// </remarks>
		public IDbConnection Connection
		{
			get
			{
				EnsureNotDisposed();
				return inner.Connection;
			}
		}

		/// <summary>
		/// Is the <c>ISession</c> still open?
		/// </summary>
		/// <value></value>
		public bool IsOpen
		{
			get
			{
				EnsureNotDisposed();
				return inner.IsOpen;
			}
		}

		/// <summary>
		/// Is the <c>ISession</c> currently connected?
		/// </summary>
		/// <value></value>
		public bool IsConnected
		{
			get { return inner.IsConnected; }
		}

		/// <summary>
		/// The read-only status for entities (and proxies) loaded into this Session.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When a proxy is initialized, the loaded entity will have the same read-only setting
		///             as the uninitialized proxy, regardless of the session's current setting.
		/// </para>
		/// <para>
		/// To change the read-only setting for a particular entity or proxy that is already in 
		///             this session, see <see cref="M:NHibernate.ISession.SetReadOnly(System.Object,System.Boolean)"/>.
		/// </para>
		/// <para>
		/// To override this session's read-only setting for entities and proxies loaded by a query,
		///             see <see cref="M:NHibernate.IQuery.SetReadOnly(System.Boolean)"/>.
		/// </para>
		/// <para>
		/// This method is a facade for <see cref="P:NHibernate.Engine.IPersistenceContext.DefaultReadOnly"/>.
		/// </para>
		/// </remarks>
		/// <seealso cref="M:NHibernate.ISession.IsReadOnly(System.Object)"/><seealso cref="M:NHibernate.ISession.SetReadOnly(System.Object,System.Boolean)"/>
		public bool DefaultReadOnly
		{
			get
			{
				return inner.DefaultReadOnly;
			}
			set
			{
				inner.DefaultReadOnly = value;
			}
		}

		/// <summary>
		/// Get the current Unit of Work and return the associated <c>ITransaction</c> object.
		/// </summary>
		/// <value></value>
		public ITransaction Transaction
		{
			get { return inner.Transaction; }
		}

		/// <summary>
		/// Cancel execution of the current query.
		/// </summary>
		/// <remarks>
		/// May be called from one thread to stop execution of a query in another thread.
		/// Use with care!
		/// </remarks>
		public void CancelQuery()
		{
			EnsureNotDisposed();
			inner.CancelQuery();
		}

		/// <summary>
		/// Does this <c>ISession</c> contain any changes which must be
		/// synchronized with the database? Would any SQL be executed if
		/// we flushed this session?
		/// </summary>
		/// <returns></returns>
		public bool IsDirty()
		{
			EnsureNotDisposed(); 
			return inner.IsDirty();
		}

		/// <summary>
		/// Is the specified entity (or proxy) read-only?
		/// </summary>
		/// <remarks>
		/// Facade for <see cref="M:NHibernate.Engine.IPersistenceContext.IsReadOnly(System.Object)"/>.
		/// </remarks>
		/// <param name="entityOrProxy">An entity (or <see cref="T:NHibernate.Proxy.INHibernateProxy"/>)</param>
		/// <returns>
		/// <c>true</c> if the entity (or proxy) is read-only, otherwise <c>false</c>.
		/// </returns>
		/// <seealso cref="P:NHibernate.ISession.DefaultReadOnly"/><seealso cref="M:NHibernate.ISession.SetReadOnly(System.Object,System.Boolean)"/>
		public bool IsReadOnly(object entityOrProxy)
		{
			EnsureNotDisposed(); 
			return inner.IsReadOnly(entityOrProxy);
		}

		/// <summary>
		/// Change the read-only status of an entity (or proxy).
		/// </summary>
		/// <remarks>
		/// <para>
		/// Read-only entities can be modified, but changes are not persisted. They are not dirty-checked 
		///             and snapshots of persistent state are not maintained. 
		/// </para>
		/// <para>
		/// Immutable entities cannot be made read-only.
		/// </para>
		/// <para>
		/// To set the <em>default</em> read-only setting for entities and proxies that are loaded 
		///             into the session, see <see cref="P:NHibernate.ISession.DefaultReadOnly"/>.
		/// </para>
		/// <para>
		/// This method a facade for <see cref="M:NHibernate.Engine.IPersistenceContext.SetReadOnly(System.Object,System.Boolean)"/>.
		/// </para>
		/// </remarks>
		/// <param name="entityOrProxy">An entity (or <see cref="T:NHibernate.Proxy.INHibernateProxy"/>).</param><param name="readOnly">If <c>true</c>, the entity or proxy is made read-only; if <c>false</c>, it is made modifiable.</param><seealso cref="P:NHibernate.ISession.DefaultReadOnly"/><seealso cref="M:NHibernate.ISession.IsReadOnly(System.Object)"/>
		public void SetReadOnly(object entityOrProxy, bool readOnly)
		{
			EnsureNotDisposed(); 
			inner.SetReadOnly(entityOrProxy, readOnly);
		}

		/// <summary>
		/// Force the <c>ISession</c> to flush.
		/// </summary>
		/// <remarks>
		/// Must be called at the end of a unit of work, before commiting the transaction and closing
		/// the session (<c>Transaction.Commit()</c> calls this method). <i>Flushing</i> if the process
		/// of synchronising the underlying persistent store with persistable state held in memory.
		/// </remarks>
		public void Flush()
		{
			EnsureNotDisposed();
			inner.Flush();
		}

		/// <summary>
		/// Disconnect the <c>ISession</c> from the current ADO.NET connection.
		/// </summary>
		/// <returns>
		/// The connection provided by the application or <see langword="null"/>
		/// </returns>
		/// <remarks>
		/// If the connection was obtained by Hibernate, close it or return it to the connection
		/// pool. Otherwise return it to the application. This is used by applications which require
		/// long transactions.
		/// </remarks>
		public IDbConnection Disconnect()
		{
			EnsureNotDisposed();
			return inner.Disconnect();
		}

		/// <summary>
		/// Obtain a new ADO.NET connection.
		/// </summary>
		/// <remarks>
		/// This is used by applications which require long transactions
		/// </remarks>
		public void Reconnect()
		{
			EnsureNotDisposed();
			inner.Reconnect();
		}

		/// <summary>
		/// Reconnect to the given ADO.NET connection.
		/// </summary>
		/// <param name="connection">An ADO.NET connection</param>
		/// <remarks>This is used by applications which require long transactions</remarks>
		public void Reconnect(IDbConnection connection)
		{
			EnsureNotDisposed();
			inner.Reconnect(connection);
		}

		/// <summary>
		/// Return the identifier of an entity instance cached by the <c>ISession</c>
		/// </summary>
		/// <param name="obj">a persistent instance</param>
		/// <returns>the identifier</returns>
		/// <remarks>
		/// Throws an exception if the instance is transient or associated with a different
		/// <c>ISession</c>
		/// </remarks>
		public object GetIdentifier(object obj)
		{
			EnsureNotDisposed();
			return inner.GetIdentifier(obj);
		}

		/// <summary>
		/// Is this instance associated with this Session?
		/// </summary>
		/// <param name="obj">an instance of a persistent class</param>
		/// <returns>
		/// true if the given instance is associated with this Session
		/// </returns>
		public bool Contains(object obj)
		{
			EnsureNotDisposed();
			return inner.Contains(obj);
		}

		/// <summary>
		/// Remove this instance from the session cache.
		/// </summary>
		/// <param name="obj">a persistent instance</param>
		/// <remarks>
		/// Changes to the instance will not be synchronized with the database.
		/// This operation cascades to associated instances if the association is mapped
		/// with <c>cascade="all"</c> or <c>cascade="all-delete-orphan"</c>.
		/// </remarks>
		public void Evict(object obj)
		{
			EnsureNotDisposed();
			inner.Evict(obj);
		}

		/// <summary>
		/// Return the persistent instance of the given entity class with the given identifier,
		/// obtaining the specified lock mode.
		/// </summary>
		/// <param name="theType">A persistent class</param>
		/// <param name="id">A valid identifier of an existing persistent instance of the class</param>
		/// <param name="lockMode">The lock level</param>
		/// <returns>the persistent instance</returns>
		public object Load(Type theType, object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Load(theType, id, lockMode);
		}

		/// <summary>
		/// Return the persistent instance of the given entity class with the given identifier,
		/// obtaining the specified lock mode.
		/// </summary>
		/// <param name="entityName">Name of the entity</param>
		/// <param name="id">A valid identifier of an existing persistent instance of the class</param>
		/// <param name="lockMode">The lock level</param>
		/// <returns>the persistent instance</returns>
		public object Load(string entityName, object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Load(entityName, id, lockMode);
		}

		/// <summary>
		/// Return the persistent instance of the given entity class with the given identifier,
		/// assuming that the instance exists.
		/// </summary>
		/// <param name="theType">A persistent class</param>
		/// <param name="id">A valid identifier of an existing persistent instance of the class</param>
		/// <returns>The persistent instance or proxy</returns>
		/// <remarks>
		/// You should not use this method to determine if an instance exists (use a query or
		/// <see cref="M:NHibernate.ISession.Get(System.Type,System.Object)"/> instead). Use this only to retrieve an instance
		/// that you assume exists, where non-existence would be an actual error.
		/// </remarks>
		public object Load(Type theType, object id)
		{
			EnsureNotDisposed();
			return inner.Load(theType, id);
		}

		/// <summary>
		/// Loads the specified id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The id.</param>
		/// <param name="lockMode">The lock mode.</param>
		/// <returns></returns>
		public T Load<T>(object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Load<T>(id, lockMode);
		}

		/// <summary>
		/// Loads the specified id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public T Load<T>(object id)
		{
			EnsureNotDisposed();
			return inner.Load<T>(id);
		}

		/// <summary>
		/// Loads the specified id.
		/// </summary>
		/// <param name="entityName">Name of the entity</param>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public object Load(string entityName, object id)
		{
			EnsureNotDisposed();
			return inner.Load(entityName, id);
		}

		/// <summary>
		/// Read the persistent state associated with the given identifier into the given transient
		/// instance.
		/// </summary>
		/// <param name="obj">An "empty" instance of the persistent class</param>
		/// <param name="id">A valid identifier of an existing persistent instance of the class</param>
		public void Load(object obj, object id)
		{
			EnsureNotDisposed();
			inner.Load(obj, id);
		}

		/// <summary>
		/// Return the persistent instance of the given entity class with the given identifier, or null
		/// if there is no such persistent instance. (If the instance, or a proxy for the instance, is
		/// already associated with the session, return that instance or proxy.)
		/// </summary>
		/// <param name="clazz">a persistent class</param>
		/// <param name="id">an identifier</param>
		/// <returns>a persistent instance or null</returns>
		public object Get(Type clazz, object id)
		{
			EnsureNotDisposed();
			return inner.Get(clazz, id);
		}

		/// <summary>
		/// Return the persistent instance of the given entity class with the given identifier, or null
		/// if there is no such persistent instance. Obtain the specified lock mode if the instance
		/// exists.
		/// </summary>
		/// <param name="clazz">a persistent class</param>
		/// <param name="id">an identifier</param>
		/// <param name="lockMode">the lock mode</param>
		/// <returns>a persistent instance or null</returns>
		public object Get(Type clazz, object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Get(clazz, id, lockMode);
		}

		/// <summary>
		/// Gets the session implementation.
		/// </summary>
		/// <returns>
		/// An NHibernate implementation of the <seealso cref="T:NHibernate.Engine.ISessionImplementor"/> interface
		/// </returns>
		/// <remarks>
		/// This method is provided in order to get the <b>NHibernate</b> implementation of the session from wrapper implementions.
		/// Implementors of the <seealso cref="T:NHibernate.ISession"/> interface should return the NHibernate implementation of this method.
		/// </remarks>
		public NHibernate.Engine.ISessionImplementor GetSessionImplementation()
		{
			EnsureNotDisposed();
			return inner.GetSessionImplementation();
		}

		/// <summary>
		/// Starts a new Session with the given entity mode in effect. This secondary
		/// Session inherits the connection, transaction, and other context
		///	information from the primary Session. It doesn't need to be flushed
		/// or closed by the developer.
		/// </summary>
		/// <param name="entityMode">The entity mode to use for the new session.</param>
		/// <returns>The new session</returns>
		public ISession GetSession(EntityMode entityMode)
		{
			EnsureNotDisposed();
			return inner.GetSession(entityMode);
		}

		/// <summary> 
		/// Return the persistent instance of the given named entity with the given identifier,
		/// or null if there is no such persistent instance. (If the instance, or a proxy for the
		/// instance, is already associated with the session, return that instance or proxy.) 
		/// </summary>
		/// <param name="entityName">the entity name </param>
		/// <param name="id">an identifier </param>
		/// <returns> a persistent instance or null </returns>
		public object Get(string entityName, object id)
		{
			EnsureNotDisposed();
			return inner.Get(entityName, id);
		}

		/// <summary>
		/// Gets the specified id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public T Get<T>(object id)
		{
			EnsureNotDisposed();
			return inner.Get<T>(id);
		}

		/// <summary>
		/// Gets the specified id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="id">The id.</param>
		/// <param name="lockMode">The lock mode.</param>
		/// <returns></returns>
		public T Get<T>(object id, LockMode lockMode)
		{
			EnsureNotDisposed();
			return inner.Get<T>(id, lockMode);
		}

		/// <summary>
		/// Enable the named filter for this current session.
		/// </summary>
		/// <param name="filterName">The name of the filter to be enabled.</param>
		/// <returns>
		/// The Filter instance representing the enabled fiter.
		/// </returns>
		public IFilter EnableFilter(string filterName)
		{
			EnsureNotDisposed();
			return inner.EnableFilter(filterName);
		}

		/// <summary>
		/// Retrieve a currently enabled filter by name.
		/// </summary>
		/// <param name="filterName">The name of the filter to be retrieved.</param>
		/// <returns>
		/// The Filter instance representing the enabled fiter.
		/// </returns>
		public IFilter GetEnabledFilter(string filterName)
		{
			EnsureNotDisposed();
			return inner.GetEnabledFilter(filterName);
		}

		/// <summary>
		/// Disable the named filter for the current session.
		/// </summary>
		/// <param name="filterName">The name of the filter to be disabled.</param>
		public void DisableFilter(string filterName)
		{
			EnsureNotDisposed();
			inner.DisableFilter(filterName);
		}

		/// <summary>
		/// Create a multi query, a query that can send several
		/// queries to the server, and return all their results in a single
		/// call.
		/// </summary>
		/// <returns>
		/// An <see cref="T:NHibernate.IMultiQuery"/> that can return
		/// a list of all the results of all the queries.
		/// Note that each query result is itself usually a list.
		/// </returns>
		public IMultiQuery CreateMultiQuery()
		{
			EnsureNotDisposed();
			return inner.CreateMultiQuery();
		}

		/// <summary>
		/// Persist all reachable transient objects, reusing the current identifier
		/// values. Note that this will not trigger the Interceptor of the Session.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="replicationMode"></param>
		public void Replicate(object obj, ReplicationMode replicationMode)
		{
			EnsureNotDisposed();
			inner.Replicate(obj, replicationMode);
		}

		/// <summary> 
		/// Persist the state of the given detached instance, reusing the current
		/// identifier value.  This operation cascades to associated instances if
		/// the association is mapped with <tt>cascade="replicate"</tt>. 
		/// </summary>
		/// <param name="entityName"></param>
		/// <param name="obj">a detached instance of a persistent class </param>
		/// <param name="replicationMode"></param>
		public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
		{
			EnsureNotDisposed();
			inner.Replicate(entityName, obj, replicationMode);
		}

		/// <summary>
		/// Persist the given transient instance, first assigning a generated identifier.
		/// </summary>
		/// <param name="obj">A transient instance of a persistent class</param>
		/// <returns>The generated identifier</returns>
		/// <remarks>
		/// Save will use the current value of the identifier property if the <c>Assigned</c>
		/// generator is used.
		/// </remarks>
		public object Save(object obj)
		{
			EnsureNotDisposed();
			return inner.Save(obj);
		}

		/// <summary>
		/// Persist the given transient instance, using the given identifier.
		/// </summary>
		/// <param name="obj">A transient instance of a persistent class</param>
		/// <param name="id">An unused valid identifier</param>
		public void Save(object obj, object id)
		{
			EnsureNotDisposed();
			inner.Save(obj, id);
		}

		/// <summary>
		/// Persist the given transient instance, first assigning a generated identifier. (Or
		/// using the current value of the identifier property if the <tt>assigned</tt>
		/// generator is used.)
		/// </summary>
		/// <param name="entityName">The Entity name.</param>
		/// <param name="obj">a transient instance of a persistent class </param>
		/// <returns> the generated identifier </returns>
		/// <remarks>
		/// This operation cascades to associated instances if the
		/// association is mapped with <tt>cascade="save-update"</tt>. 
		/// </remarks>
		public object Save(string entityName, object obj)
		{
			EnsureNotDisposed();
			return inner.Save(entityName, obj);
		}

		/// <summary>
		/// Either <c>Save()</c> or <c>Update()</c> the given instance, depending upon the value of
		/// its identifier property.
		/// </summary>
		/// <param name="obj">A transient instance containing new or updated state</param>
		/// <remarks>
		/// By default the instance is always saved. This behaviour may be adjusted by specifying
		/// an <c>unsaved-value</c> attribute of the identifier property mapping
		/// </remarks>
		public void SaveOrUpdate(object obj)
		{
			EnsureNotDisposed();
			inner.SaveOrUpdate(obj);
		}

		/// <summary> 
		/// Either <see cref="Save(String,Object)"/> or <see cref="Update(String,Object)"/>
		/// the given instance, depending upon resolution of the unsaved-value checks
		/// (see the manual for discussion of unsaved-value checking).
		/// </summary>
		/// <param name="entityName">The name of the entity </param>
		/// <param name="obj">a transient or detached instance containing new or updated state </param>
		/// <seealso cref="ISession.Save(String,Object)"/>
		/// <seealso cref="ISession.Update(String,Object)"/>
		/// <remarks>
		/// This operation cascades to associated instances if the association is mapped
		/// with <tt>cascade="save-update"</tt>. 
		/// </remarks>
		public void SaveOrUpdate(string entityName, object obj)
		{
			EnsureNotDisposed();
			inner.SaveOrUpdate(entityName, obj);
		}


		/// <summary>
		/// Update the persistent instance with the identifier of the given transient instance.
		/// </summary>
		/// <param name="obj">A transient instance containing updated state</param>
		/// <remarks>
		/// If there is a persistent instance with the same identifier, an exception is thrown. If
		/// the given transient instance has a <see langword="null"/> identifier, an exception will be thrown.
		/// </remarks>
		public void Update(object obj)
		{
			EnsureNotDisposed();
			inner.Update(obj);
		}

		/// <summary>
		/// Update the persistent state associated with the given identifier.
		/// </summary>
		/// <param name="obj">A transient instance containing updated state</param>
		/// <param name="id">Identifier of persistent instance</param>
		/// <remarks>
		/// An exception is thrown if there is a persistent instance with the same identifier
		/// in the current session.
		/// </remarks>
		public void Update(object obj, object id)
		{
			EnsureNotDisposed();
			inner.Update(obj, id);
		}

		/// <summary> 
		/// Update the persistent instance with the identifier of the given detached
		/// instance. 
		/// </summary>
		/// <param name="entityName">The Entity name.</param>
		/// <param name="obj">a detached instance containing updated state </param>
		/// <remarks>
		/// If there is a persistent instance with the same identifier,
		/// an exception is thrown. This operation cascades to associated instances
		/// if the association is mapped with <tt>cascade="save-update"</tt>. 
		/// </remarks>
		public void Update(string entityName, object obj)
		{
			EnsureNotDisposed();
			inner.Update(entityName, obj);
		}

		/// <summary> 
		/// Copy the state of the given object onto the persistent object with the same
		/// identifier. If there is no persistent instance currently associated with
		/// the session, it will be loaded. Return the persistent instance. If the
		/// given instance is unsaved, save a copy of and return it as a newly persistent
		/// instance. The given instance does not become associated with the session.
		/// This operation cascades to associated instances if the association is mapped
		/// with <tt>cascade="merge"</tt>.<br/>
		/// <br/>
		/// The semantics of this method are defined by JSR-220. 
		/// </summary>
		/// <param name="obj">a detached instance with state to be copied </param>
		/// <returns> an updated persistent instance </returns>
		public object Merge(object obj)
		{
			EnsureNotDisposed();
			return inner.Merge(obj);
		}

		/// <summary> 
		/// Copy the state of the given object onto the persistent object with the same
		/// identifier. If there is no persistent instance currently associated with
		/// the session, it will be loaded. Return the persistent instance. If the
		/// given instance is unsaved, save a copy of and return it as a newly persistent
		/// instance. The given instance does not become associated with the session.
		/// This operation cascades to associated instances if the association is mapped
		/// with <tt>cascade="merge"</tt>.<br/>
		/// <br/>
		/// The semantics of this method are defined by JSR-220. 
		/// </summary>
		/// <param name="entityName">The entity name</param>
		/// <param name="obj">a detached instance with state to be copied </param>
		/// <returns> an updated persistent instance </returns>
		public object Merge(string entityName, object obj)
		{
			EnsureNotDisposed();
			return inner.Merge(entityName, obj);
		}

		/// <summary>
		/// Copy the state of the given object onto the persistent object with the same
		/// identifier. If there is no persistent instance currently associated with
		/// the session, it will be loaded. Return the persistent instance. If the
		/// given instance is unsaved, save a copy of and return it as a newly persistent
		/// instance. The given instance does not become associated with the session.
		/// This operation cascades to associated instances if the association is mapped
		/// with <tt>cascade="merge"</tt>.<br/>
		/// The semantics of this method are defined by JSR-220.
		/// </summary>
		/// <param name="entity">a detached instance with state to be copied </param>
		/// <returns>
		/// an updated persistent instance 
		/// </returns>
		public T Merge<T>(T entity) where T : class
		{
			EnsureNotDisposed();
			return inner.Merge(entity);
		}

		/// <summary>
		/// Copy the state of the given object onto the persistent object with the same
		/// identifier. If there is no persistent instance currently associated with
		/// the session, it will be loaded. Return the persistent instance. If the
		/// given instance is unsaved, save a copy of and return it as a newly persistent
		/// instance. The given instance does not become associated with the session.
		/// This operation cascades to associated instances if the association is mapped
		/// with <tt>cascade="merge"</tt>.<br/>
		/// The semantics of this method are defined by JSR-220.
		/// <param name="entityName">Name of the entity.</param><param name="entity">a detached instance with state to be copied </param>
		/// <returns>
		/// an updated persistent instance 
		/// </returns>
		/// </summary>
		/// <returns/>
		public T Merge<T>(string entityName, T entity) where T : class
		{
			EnsureNotDisposed();
			return inner.Merge(entityName, entity);
		}

		/// <summary> 
		/// Make a transient instance persistent. This operation cascades to associated
		/// instances if the association is mapped with <tt>cascade="persist"</tt>.<br/>
		/// <br/>
		/// The semantics of this method are defined by JSR-220. 
		/// </summary>
		/// <param name="obj">a transient instance to be made persistent </param>
		public void Persist(object obj)
		{
			EnsureNotDisposed();
			inner.Persist(obj);
		}

		/// <summary> 
		/// Make a transient instance persistent. This operation cascades to associated
		/// instances if the association is mapped with <tt>cascade="persist"</tt>.<br/>
		/// <br/>
		/// The semantics of this method are defined by JSR-220. 
		/// </summary>
		/// <param name="entityName">The entity name</param>
		/// <param name="obj">a transient instance to be made persistent </param>
		public void Persist(string entityName, object obj)
		{
			EnsureNotDisposed();
			inner.Persist(entityName, obj);
		}

		/// <summary>
		/// Copy the state of the given object onto the persistent object with the same
		/// identifier. If there is no persistent instance currently associated with
		/// the session, it will be loaded. Return the persistent instance. If the
		/// given instance is unsaved or does not exist in the database, save it and
		/// return it as a newly persistent instance. Otherwise, the given instance
		/// does not become associated with the session.
		/// </summary>
		/// <param name="obj">a transient instance with state to be copied</param>
		/// <returns>an updated persistent instance</returns>
		public object SaveOrUpdateCopy(object obj)
		{
			EnsureNotDisposed();
			return inner.SaveOrUpdateCopy(obj);
		}

		/// <summary>
		/// Copy the state of the given object onto the persistent object with the
		/// given identifier. If there is no persistent instance currently associated
		/// with the session, it will be loaded. Return the persistent instance. If
		/// there is no database row with the given identifier, save the given instance
		/// and return it as a newly persistent instance. Otherwise, the given instance
		/// does not become associated with the session.
		/// </summary>
		/// <param name="obj">a persistent or transient instance with state to be copied</param>
		/// <param name="id">the identifier of the instance to copy to</param>
		/// <returns>an updated persistent instance</returns>
		public object SaveOrUpdateCopy(object obj, object id)
		{
			EnsureNotDisposed();
			return inner.SaveOrUpdateCopy(obj, id);
		}

		/// <summary>
		/// Remove a persistent instance from the datastore.
		/// </summary>
		/// <param name="obj">The instance to be removed</param>
		/// <remarks>
		/// The argument may be an instance associated with the receiving <c>ISession</c> or a
		/// transient instance with an identifier associated with existing persistent state.
		/// </remarks>
		public void Delete(object obj)
		{
			EnsureNotDisposed();
			inner.Delete(obj);
		}

		/// <summary>
		/// Remove a persistent instance from the datastore.
		/// </summary>
		/// <param name="entityName">Name of the entity</param>
		/// <param name="obj">The instance to be removed</param>
		/// <remarks>
		/// The argument may be an instance associated with the receiving <c>ISession</c> or a
		/// transient instance with an identifier associated with existing persistent state.
		/// </remarks>
		public void Delete(string entityName, object obj)
		{
			EnsureNotDisposed();
			inner.Delete(entityName, obj);
		}

		/// <summary>
		/// Execute a query
		/// </summary>
		/// <param name="query">A query expressed in Hibernate's query language</param>
		/// <returns>A distinct list of instances</returns>
		/// <remarks>See <see cref="M:NHibernate.IQuery.List"/> for implications of <c>cache</c> usage.</remarks>
		public IList Find(string query)
		{
			EnsureNotDisposed();
			return inner.CreateQuery(query).List();
		}

		/// <summary>
		/// Apply a filter to a persistent collection.
		/// </summary>
		/// <param name="collection">A persistent collection to filter</param>
		/// <param name="filter">A filter query string</param>
		/// <returns>The resulting collection</returns>
		/// <remarks>
		/// A filter is a Hibernate query that may refer to <c>this</c>, the collection element.
		/// Filters allow efficient access to very large lazy collections. (Executing the filter
		/// does not initialize the collection.)
		/// </remarks>
		public ICollection Filter(object collection, string filter)
		{
			EnsureNotDisposed();
			return inner.CreateFilter(collection, filter).List();
		}

		/// <summary>
		/// Apply a filter to a persistent collection, binding the given parameter to a "?" placeholder
		/// </summary>
		/// <param name="collection">A persistent collection to filter</param>
		/// <param name="filter">A filter query string</param>
		/// <param name="value">A value to be written to a "?" placeholder in the query</param>
		/// <param name="type">The hibernate type of value</param>
		/// <returns>A collection</returns>
		/// <remarks>
		/// A filter is a Hibernate query that may refer to <c>this</c>, the collection element.
		/// Filters allow efficient access to very large lazy collections. (Executing the filter
		/// does not initialize the collection.)
		/// </remarks>
		public ICollection Filter(object collection, string filter, object value, IType type)
		{
			EnsureNotDisposed();
			IQuery q = inner.CreateFilter(collection, filter);
			q.SetParameter(0, value, type);
			return q.List();
		}

		/// <summary>
		/// Apply a filter to a persistent collection, binding the given parameters to "?" placeholders.
		/// </summary>
		/// <param name="collection">A persistent collection to filter</param>
		/// <param name="filter">A filter query string</param>
		/// <param name="values">The values to be written to "?" placeholders in the query</param>
		/// <param name="types">The hibernate types of the values</param>
		/// <returns>A collection</returns>
		/// <remarks>
		/// A filter is a Hibernate query that may refer to <c>this</c>, the collection element.
		/// Filters allow efficient access to very large lazy collections. (Executing the filter
		/// does not initialize the collection.)
		/// </remarks>
		public ICollection Filter(object collection, string filter, object[] values, IType[] types)
		{
			EnsureNotDisposed();
			IQuery q = inner.CreateFilter(collection, filter);
			for (int i = 0; i < values.Length; i++)
			{
				q.SetParameter(0, values[i], types[i]);
			}

			return q.List();
		}

		/// <summary>
		/// Delete all objects returned by the query.
		/// </summary>
		/// <param name="query">The query string</param>
		/// <returns>Returns the number of objects deleted.</returns>
		public int Delete(string query)
		{
			EnsureNotDisposed();
			return inner.Delete(query);
		}

		/// <summary>
		/// Delete all objects returned by the query.
		/// </summary>
		/// <param name="query">The query string</param>
		/// <param name="value">A value to be written to a "?" placeholer in the query</param>
		/// <param name="type">The hibernate type of value.</param>
		/// <returns>The number of instances deleted</returns>
		public int Delete(string query, object value, IType type)
		{
			EnsureNotDisposed();
			return inner.Delete(query, value, type);
		}

		/// <summary>
		/// Delete all objects returned by the query.
		/// </summary>
		/// <param name="query">The query string</param>
		/// <param name="values">A list of values to be written to "?" placeholders in the query</param>
		/// <param name="types">A list of Hibernate types of the values</param>
		/// <returns>The number of instances deleted</returns>
		public int Delete(string query, object[] values, IType[] types)
		{
			EnsureNotDisposed();
			return inner.Delete(query, values, types);
		}

		/// <summary>
		/// Obtain the specified lock level upon the given object.
		/// </summary>
		/// <param name="obj">A persistent instance</param>
		/// <param name="lockMode">The lock level</param>
		public void Lock(object obj, LockMode lockMode)
		{
			EnsureNotDisposed();
			inner.Lock(obj, lockMode);
		}

		/// <summary> 
		/// Obtain the specified lock level upon the given object. 
		/// </summary>
		/// <param name="entityName">The Entity name.</param>
		/// <param name="obj">a persistent or transient instance </param>
		/// <param name="lockMode">the lock level </param>
		/// <remarks>
		/// This may be used to perform a version check (<see cref="LockMode.Read"/>), to upgrade to a pessimistic
		/// lock (<see cref="LockMode.Upgrade"/>), or to simply reassociate a transient instance
		/// with a session (<see cref="LockMode.None"/>). This operation cascades to associated
		/// instances if the association is mapped with <tt>cascade="lock"</tt>.
		/// </remarks>
		public void Lock(string entityName, object obj, LockMode lockMode)
		{
			EnsureNotDisposed();
			inner.Lock(entityName, obj, lockMode);
		}

		/// <summary>
		/// Re-read the state of the given instance from the underlying database.
		/// </summary>
		/// <param name="obj">A persistent instance</param>
		/// <remarks>
		/// 	<para>
		/// It is inadvisable to use this to implement long-running sessions that span many
		/// business tasks. This method is, however, useful in certain special circumstances.
		/// </para>
		/// 	<para>
		/// For example,
		/// <list>
		/// 			<item>Where a database trigger alters the object state upon insert or update</item>
		/// 			<item>After executing direct SQL (eg. a mass update) in the same session</item>
		/// 			<item>After inserting a <c>Blob</c> or <c>Clob</c></item>
		/// 		</list>
		/// 	</para>
		/// </remarks>
		public void Refresh(object obj)
		{
			EnsureNotDisposed();
			inner.Refresh(obj);
		}

		/// <summary>
		/// Re-read the state of the given instance from the underlying database, with
		/// the given <c>LockMode</c>.
		/// </summary>
		/// <param name="obj">a persistent or transient instance</param>
		/// <param name="lockMode">the lock mode to use</param>
		/// <remarks>
		/// It is inadvisable to use this to implement long-running sessions that span many
		/// business tasks. This method is, however, useful in certain special circumstances.
		/// </remarks>
		public void Refresh(object obj, LockMode lockMode)
		{
			EnsureNotDisposed();
			inner.Refresh(obj, lockMode);
		}

		/// <summary>
		/// Determine the current lock mode of the given object
		/// </summary>
		/// <param name="obj">A persistent instance</param>
		/// <returns>The current lock mode</returns>
		public LockMode GetCurrentLockMode(object obj)
		{
			EnsureNotDisposed();
			return inner.GetCurrentLockMode(obj);
		}

		/// <summary>
		/// Begin a unit of work and return the associated <c>ITransaction</c> object.
		/// </summary>
		/// <returns>A transaction instance</returns>
		/// <remarks>
		/// If a new underlying transaction is required, begin the transaction. Otherwise
		/// continue the new work in the context of the existing underlying transaction.
		/// The class of the returned <see cref="T:NHibernate.ITransaction"/> object is determined by
		/// the property <c>transaction_factory</c>
		/// </remarks>
		public ITransaction BeginTransaction()
		{
			EnsureNotDisposed();
			return inner.BeginTransaction();
		}

		/// <summary>
		/// Begin a transaction with the specified <c>isolationLevel</c>
		/// </summary>
		/// <param name="isolationLevel">Isolation level for the new transaction</param>
		/// <returns>
		/// A transaction instance having the specified isolation level
		/// </returns>
		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			EnsureNotDisposed();
			return inner.BeginTransaction(isolationLevel);
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The class to Query</typeparam>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria<T>() where T : class
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(typeof(T));
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class with a specific alias
		/// </summary>
		/// <typeparam name="T">The class to Query</typeparam>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(typeof(T), alias);
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class.
		/// </summary>
		/// <param name="persistentClass">The class to Query</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(Type persistentClass)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(persistentClass);
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class with a specific alias
		/// </summary>
		/// <param name="persistentClass">The class to Query</param>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(Type persistentClass, string alias)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(persistentClass, alias);
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class with a specific alias
		/// </summary>
		/// <param name="entityName">Name of the entity</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(string entityName)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(entityName);
		}

		/// <summary>
		/// Creates a new <c>Criteria</c> for the entity class with a specific alias
		/// </summary>
		/// <param name="entityName">Name of the entity</param>
		/// <param name="alias">The alias of the entity</param>
		/// <returns>An ICriteria object</returns>
		public ICriteria CreateCriteria(string entityName, string alias)
		{
			EnsureNotDisposed();
			return inner.CreateCriteria(entityName, alias);
		}

		/// <summary>
		/// Creates a new <c>IQueryOver&lt;T&gt;</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The entity class</typeparam>
		/// <returns>
		/// An ICriteria&lt;T&gt; object
		/// </returns>
		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			EnsureNotDisposed();
			return inner.QueryOver<T>();
		}

		/// <summary>
		/// Creates a new <c>IQueryOver&lt;T&gt;</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The entity class</typeparam>
		/// <returns>
		/// An ICriteria&lt;T&gt; object
		/// </returns>
		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			EnsureNotDisposed();
			return inner.QueryOver(alias);
		}

		/// <summary>
		/// Creates a new <c>IQueryOver{T};</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The entity class</typeparam><param name="entityName">The name of the entity to Query</param>
		/// <returns>
		/// An IQueryOver{T} object
		/// </returns>
		public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
		{
			EnsureNotDisposed();
			return inner.QueryOver<T>(entityName);
		}

		/// <summary>
		/// Creates a new <c>IQueryOver{T}</c> for the entity class.
		/// </summary>
		/// <typeparam name="T">The entity class</typeparam><param name="entityName">The name of the entity to Query</param><param name="alias">The alias of the entity</param>
		/// <returns>
		/// An IQueryOver{T} object
		/// </returns>
		public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
		{
			EnsureNotDisposed();
			return inner.QueryOver(entityName, alias);
		}

		/// <summary>
		/// Create a new instance of <c>Query</c> for the given query string
		/// </summary>
		/// <param name="queryString">A hibernate query string</param>
		/// <returns>The query</returns>
		public IQuery CreateQuery(string queryString)
		{
			EnsureNotDisposed();
			return inner.CreateQuery(queryString);
		}

		/// <summary>
		/// Create a new instance of <c>Query</c> for the given collection and filter string
		/// </summary>
		/// <param name="collection">A persistent collection</param>
		/// <param name="queryString">A hibernate query</param>
		/// <returns>A query</returns>
		public IQuery CreateFilter(object collection, string queryString)
		{
			EnsureNotDisposed();
			return inner.CreateFilter(collection, queryString);
		}

		/// <summary>
		/// Obtain an instance of <see cref="T:NHibernate.IQuery"/> for a named query string defined in the
		/// mapping file.
		/// </summary>
		/// <param name="queryName">The name of a query defined externally.</param>
		/// <returns>
		/// An <see cref="T:NHibernate.IQuery"/> from a named query string.
		/// </returns>
		/// <remarks>
		/// The query can be either in <c>HQL</c> or <c>SQL</c> format.
		/// </remarks>
		public IQuery GetNamedQuery(string queryName)
		{
			EnsureNotDisposed();
			return inner.GetNamedQuery(queryName);
		}

		/// <summary>
		/// Create a new instance of <see cref="T:NHibernate.ISQLQuery"/> for the given SQL query string.
		/// </summary>
		/// <param name="queryString">a query expressed in SQL</param>
		/// <returns>
		/// An <see cref="T:NHibernate.ISQLQuery"/> from the SQL string
		/// </returns>
		public ISQLQuery CreateSQLQuery(string queryString)
		{
			EnsureNotDisposed();
			return inner.CreateSQLQuery(queryString);
		}

		/// <summary>
		/// Create a new instance of <c>IQuery</c> for the given SQL string.
		/// </summary>
		/// <param name="sql">a query expressed in SQL</param>
		/// <param name="returnAlias">a table alias that appears inside <c>{}</c> in the SQL string</param>
		/// <param name="returnClass">the returned persistent class</param>
		/// <returns>
		/// An <see cref="T:NHibernate.IQuery"/> from the SQL string
		/// </returns>
		public IQuery CreateSQLQuery(string sql, string returnAlias, Type returnClass)
		{
			EnsureNotDisposed();
			return inner.CreateSQLQuery(sql).AddEntity(returnAlias, returnClass);
		}

		/// <summary>
		/// Completely clear the session. Evict all loaded instances and cancel all pending
		/// saves, updates and deletions. Do not close open enumerables or instances of
		/// <c>ScrollableResults</c>.
		/// </summary>
		public void Clear()
		{
			EnsureNotDisposed();
			inner.Clear();
		}

		/// <summary>
		/// End the <c>ISession</c> by disconnecting from the ADO.NET connection and cleaning up.
		/// </summary>
		/// <returns>
		/// The connection provided by the application or <see langword="null"/>
		/// </returns>
		/// <remarks>
		/// It is not strictly necessary to <c>Close()</c> the <c>ISession</c> but you must
		/// at least <c>Disconnect()</c> it.
		/// </remarks>
		public IDbConnection Close()
		{
			EnsureNotDisposed();
			return DoClose(closing: true);
		}


		/// <summary>
		/// Return the entity name for a persistent entity
		/// </summary>
		/// <param name="obj">a persistent entity</param>
		/// <returns>the entity name</returns>
		public string GetEntityName(object obj)
		{
			EnsureNotDisposed();
			return inner.GetEntityName(obj);
		}

		/// <summary>
		/// Sets the batch size of the session
		/// </summary>
		/// <param name="batchSize"></param>
		/// <returns></returns>
		public ISession SetBatchSize(int batchSize)
		{
			EnsureNotDisposed();
			return inner.SetBatchSize(batchSize);
		}

		/// <summary>
		/// An <see cref="T:NHibernate.IMultiCriteria"/> that can return a list of all the results
		/// of all the criterias.
		/// </summary>
		/// <returns></returns>
		public IMultiCriteria CreateMultiCriteria()
		{
			EnsureNotDisposed();
			return inner.CreateMultiCriteria();
		}

		/// <summary>
		/// The current cache mode.
		/// </summary>
		/// <value></value>
		/// <remarks>
		/// Cache mode determines the manner in which this session can interact with
		/// the second level cache.
		/// </remarks>
		public CacheMode CacheMode
		{
			get { return inner.CacheMode; }
			set { inner.CacheMode = value; }
		}

		/// <summary>
		/// Get the statistics for this session.
		/// </summary>
		/// <value></value>
		public ISessionStatistics Statistics
		{
			get { return inner.Statistics; }
		}

		/// <summary>
		/// Gets the active entity mode.
		/// </summary>
		/// <value>The active entity mode.</value>
		public EntityMode ActiveEntityMode
		{
			get { return inner.ActiveEntityMode; }
		}

		#endregion

		#region Dispose delegation

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			Thread.MemoryBarrier();

			if (_logger.IsDebugEnabled)
				_logger.Debug("Disposing Session = [" + _sessionId + "] canClose? " + canClose);

			// DoClose(closing: false);

			if (canClose)
			{
				// sessionStore.Remove(_alias, this);

				// called when there are no transactions and the root sessionwrapper is disposed
				if (removeFromStore != null) 
				{
					removeFromStore();
					removeFromStore = null;
				}

				inner.Dispose();
			}
		}

		// called by the transaction that "owns" this session
		public void UnsafeDispose(bool commit)
		{
			_disposed = true;
			Thread.MemoryBarrier();

			// sessionStore.Remove(_alias, this);
			if (removeFromStore != null)
			{
				removeFromStore();
				removeFromStore = null;
			}

			try
			{
				if (_tx != null)
				{
					if (commit && !_tx.WasCommitted)
						_tx.Commit();
					else if (!commit && !_tx.WasRolledBack)
						_tx.Rollback();
				}
			}
			catch (Exception ex)
			{
				this._logger.Error("Error completing transaction", ex);
			}
			finally
			{
				inner.Dispose();
			}
		}

		#endregion

		/// <summary>
		/// Does the close.
		/// </summary>
		/// <returns></returns>
		protected IDbConnection DoClose(bool closing)
		{
			if (canClose)
			{
				return inner.Close();
			}
			else
			{
				// Fuck no, you cannot ever close a session that you dont own
				// inner.Dispose(); //as nhib calls, soft dispose tx aware.
			}

			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void EnsureNotDisposed()
		{
			if (this._disposed) throw new ObjectDisposedException("SessionDelegate");
		}

		/// <summary>
		/// Returns <see langword="true"/> if the supplied sessions are equal, <see langword="false"/> otherwise.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns></returns>
		public static bool AreEqual(ISession left, ISession right)
		{
			var sdLeft = left as SessionDelegate;
			var sdRight = right as SessionDelegate;

			if (sdLeft != null && sdRight != null)
			{
				return Object.ReferenceEquals(sdLeft.inner, sdRight.inner);
			}

			throw new NotSupportedException("AreEqual: left is " +
											left.GetType().Name + " and right is " + right.GetType().Name);
		}

		public void Store()
		{
			sessionStore.Store(this._alias, this, out this.removeFromStore);
		}

		public void InternalBeginTransaction()
		{
			_tx = inner.BeginTransaction();
		}

		public override string ToString()
		{
			return "SessionDelegate for session " + _sessionId;
		}
	}
}
