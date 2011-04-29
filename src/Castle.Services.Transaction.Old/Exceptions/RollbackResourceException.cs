#region License
//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 
#endregion

namespace Castle.Services.Transaction
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using Core;

	[Serializable]
	public class RollbackResourceException : TransactionException
	{
		private readonly List<Pair<IResource, Exception>> failedResources = new List<Pair<IResource, Exception>>();

		public RollbackResourceException(string message,
			IEnumerable<Pair<IResource,Exception>> failedResources)
			: base(message, null)
		{
			this.failedResources.AddRange(failedResources);
		}

		public RollbackResourceException(SerializationInfo info, StreamingContext context) 
			: base(info, context)
		{
		}

		public RollbackResourceException(SerializationInfo info, StreamingContext context,
			IEnumerable<Pair<IResource,Exception>> failedResources) : base(info, context)
		{
			this.failedResources.AddRange(failedResources);
		}

		public IList<Pair<IResource,Exception>> FailedResources
		{
			get { return failedResources; }
		}
	}
}