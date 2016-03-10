// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Castle.Services.Transaction.Activities
{
	using System;
	using System.Configuration;
	using System.Runtime.Remoting.Messaging;
	using System.Threading;
	using Castle.Core.Logging;

	/// <summary>
	/// 	The call-context activity manager saves the stack of transactions
	/// 	on the call-stack-context. This is the recommended manager and the default,
	/// 	also.
	/// </summary>
	public class ThreadLocalActivityManager : IActivityManager
	{
		private const string Key = "Castle.Services.Transaction.Activity";

		public ILoggerFactory LoggerFactory { get; set; }

		private readonly ThreadLocal<Activity> holder;
		private readonly bool goWithCallContext;

		public ThreadLocalActivityManager()
		{
			//CallContext.LogicalSetData(Key, null);

			holder = new ThreadLocal<Activity>(CreateActivity);

			goWithCallContext = Convert.ToBoolean(ConfigurationManager.AppSettings["castle.tx.callcontext"]);
		}

		public Activity GetCurrentActivity()
		{
			return goWithCallContext ? GetFromCallContext() : holder.Value;
		}

		private Activity GetFromCallContext()
		{
			var activity = (Activity) CallContext.GetData(Key);

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
				logger = LoggerFactory.Create(typeof (Activity));
			// create activity
			var activity = new Activity(logger);
			return activity;
		}
	}
}