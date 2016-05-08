namespace Castle.NHibIntegration.Internal
{
	using System;
	using System.Data;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Core.Logging;
	using NHibernate;

	public abstract class BaseSessionDelegate : MarshalByRefObject
	{
		protected readonly string _alias;
		protected readonly ISessionStore sessionStore;
		private readonly ILogger _logger;
		private readonly bool canClose;
//		private object cookie;
		private bool _disposed;
		protected Action removeFromStore;
		protected ITransaction _tx;
		protected Guid _sessionId;
		public string Helper;

		protected BaseSessionDelegate(string @alias, bool canClose, Guid sessionId, 
			ISessionStore sessionStore, ILogger logger)
		{
			this._alias = @alias;
			this.canClose = canClose;
			this._sessionId = sessionId;
			this.sessionStore = sessionStore;
			this._logger = logger;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected void EnsureNotDisposed()
		{
			if (this._disposed) throw new ObjectDisposedException("SessionDelegate");
		}

		public abstract void Store();

		public abstract void InternalBeginTransaction();

		protected abstract void InnerDispose();

		protected abstract void InnerClose();

		protected IDbConnection DoClose(bool closing)
		{
			if (canClose)
			{
				InnerClose();
			}
			else
			{
				// Fuck no, you cannot ever close a session that you dont own
				// inner.Dispose(); //as nhib calls, soft dispose tx aware.
			}

			return null;
		}

		public void Dispose()
		{
			if (_disposed) return;
			Thread.MemoryBarrier();
			_disposed = true;

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

				InnerDispose();
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
				// inner.Dispose();
				InnerDispose();
			}
		}
	}
}