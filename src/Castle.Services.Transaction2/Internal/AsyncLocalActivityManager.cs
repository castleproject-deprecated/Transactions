namespace Castle.Services.Transaction2.Internal
{
	using System;
	using System.Threading;
	using Core.Logging;

	public class AsyncLocalActivityManager : IActivityManager2
	{
		private readonly AsyncLocal<Activity2> _holder;
		private int _counter;

		private ILogger _logger = NullLogger.Instance;

		public AsyncLocalActivityManager()
		{
			_holder = new AsyncLocal<Activity2>(OnValueChanged);
		}
		
		// Invoked by the activy itself after popping a transaction
		public void NotifyPop(Activity2 activity2)
		{
			if (activity2.IsEmpty)
			{
				var ctxActivity = _holder.Value;
				if (!activity2.Equals(ctxActivity))
				{
					// wtf?
					_logger.Fatal("activity does not match the context one. Expecting " + activity2 + " but found " + ctxActivity);
				}
				_holder.Value = null; // removes empty activity from context

				activity2.Dispose();
			}
		}

		private void OnValueChanged(AsyncLocalValueChangedArgs<Activity2> args)
		{
			Console.WriteLine("OnValueChanged from " + args.PreviousValue + " to " + args.CurrentValue + "  ctx_switch: " + args.ThreadContextChanged);
		}

		public ILogger Logger
		{
			get { return _logger; }
			set { _logger = value; }
		}

		public Activity2 EnsureActivityExists()
		{
			var cur = _holder.Value;
			if (cur == null)
			{
				_holder.Value = cur = CreateActivity();
			}
			return cur;
		}

		public bool TryGetCurrentActivity(out Activity2 activity)
		{
			activity = null;
			var cur = _holder.Value;
			if (cur == null)
				return false;

			activity = _holder.Value;
			return true;
		}

		private Activity2 CreateActivity()
		{
			var id = Interlocked.Increment(ref _counter);
			return new Activity2(this, id, _logger.CreateChildLogger("Activity." + id));
		}
	}
}