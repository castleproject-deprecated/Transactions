namespace Castle.Services.Transaction.Internal
{
	using System;
	using System.Collections.Concurrent;
	using System.Threading;
	using Core.Logging;
	using Transaction;

	// this one is thread safe, just to be on the safe side.
	public class Activity2 : IDisposable
	{
		private readonly IActivityManager2 _manager;
		private readonly int _id;
		private readonly ILogger _logger;
		private ConcurrentStack<ITransaction2> _stack;
		private volatile bool _disposed;

		public Activity2(IActivityManager2 manager, int id, ILogger logger)
		{
			_manager = manager;
			_id = id;
			_logger = logger;
			_stack = new ConcurrentStack<ITransaction2>();
		}

		public ITransaction2 CurrentTransaction
		{
			get
			{
				ITransaction2 peeked;
				while(!_stack.IsEmpty)
					if (_stack.TryPeek(out peeked))
					{
						return peeked;
					}
				return null;
			}
		}

		public int Count
		{
			get { return _stack.Count; }
		}

		public bool IsEmpty
		{
			get { return _stack.IsEmpty; }
		}

		public void Push(ITransaction2 transaction)
		{
			if (_disposed) throw new ObjectDisposedException("Activity2");

			_stack.Push(transaction);

			if (_logger.IsDebugEnabled)
			{
				_logger.Debug("Pushed " + transaction);
			}
		}

		public void Pop(ITransaction2 transaction)
		{
			if (_disposed) throw new ObjectDisposedException("Activity2");

			tryAgain:

			ITransaction2 result;
			if (_stack.TryPop(out result))
			{
				// confirm it's the expected one
				if (!transaction.Equals(result))
				{
					var msg = "Transaction popped from activity didn't match the parameter one. " +
					          "Found " + result + " and was expecting " + transaction;

					_logger.Fatal(msg);

					throw new Exception(msg);
				}
			}
			else if (_stack.IsEmpty)
			{
				var msg = "Tried to pop transaction from activity, but activity stack was empty.";
				
				_logger.Fatal(msg);

				throw new Exception(msg);
			}
			else
			{
				goto tryAgain;
			}

			if (_logger.IsDebugEnabled)
			{
				_logger.Debug("Pop " + transaction);
			}

			_manager.NotifyPop(this);
		}

		public override string ToString()
		{
			return "Activity." + _id;
		}

		protected bool InternalEquals(Activity2 other)
		{
			return _id == other._id;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return InternalEquals((Activity2)obj);
		}

		public override int GetHashCode()
		{
			return _id;
		}

		public void Dispose()
		{
			if (_disposed) return;

			_disposed = true;
			Thread.MemoryBarrier();
		}
	}
}