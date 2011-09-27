#region license

// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Castle.Services.Transaction.Activities;
using Castle.Core.Logging;

namespace Castle.Services.Transaction.Tests
{
	public class TransientActivityManager : IActivityManager
	{
		private readonly Activity activity;

		public TransientActivityManager()
		{
			activity = new Activity(NullLogger.Instance);
		}

		/// <summary>
		/// 	Gets the current activity.
		/// </summary>
		/// <value>The current activity.</value>
		Activity IActivityManager.GetCurrentActivity()
		{
			return activity;
		}
	}
}