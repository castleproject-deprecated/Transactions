namespace Castle.Services.Transaction.Activities
{
	using System;
	using System.Configuration;
	using System.Runtime.Remoting.Messaging;
	using System.Threading;
	using Core.Logging;

	/// <summary>
	/// 	The call-context activity manager saves the stack of transactions
	/// 	on the call-stack-context. This is the recommended manager and the default,
	/// 	also.
	/// </summary>
	public class AsyncLocalActivityManager : IActivityManager
	{
		private const string Key = "Castle.Services.Transaction.Activity2";

		public ILoggerFactory LoggerFactory { get; set; }

		private static readonly AsyncLocal<Activity> holder = new AsyncLocal<Activity>();
		private readonly bool goWithCallContext;

		public AsyncLocalActivityManager()
		{
			//CallContext.LogicalSetData(Key, null);
			// holder = new ThreadLocal<Activity>(CreateActivity);

			if (!goWithCallContext && holder.Value == null)
			{
				holder.Value = CreateActivity();
			}

			goWithCallContext = Convert.ToBoolean(ConfigurationManager.AppSettings["castle.tx.callcontext"]);
		}

		public Activity GetCurrentActivity()
		{
			return goWithCallContext ? GetFromCallContext() : SafeGetFromAsyncLocal();
		}

		private Activity SafeGetFromAsyncLocal()
		{
			var cur = holder.Value;
			if (cur == null)
			{
				holder.Value = cur = CreateActivity();
			}
			return cur;
		}

		private Activity GetFromCallContext()
		{
			var activity = (Activity)CallContext.GetData(Key);

			if (activity == null)
			{
				activity = CreateActivity();

				// set activity in call context
				CallContext.SetData(Key, activity);
			}

			return activity;
		}

		private Activity CreateActivity()
		{
			ILogger logger = NullLogger.Instance;

			// check we have a ILoggerFactory service instance (logging is enabled)
			if (LoggerFactory != null) // create logger
				logger = LoggerFactory.Create(typeof(Activity));
			// create activity
			var activity = new Activity(logger);
			return activity;
		}
	}
}