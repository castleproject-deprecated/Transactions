﻿#region license

// Copyright 2004-2010 Castle Project - http://www.castleproject.org/
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

namespace Castle.Services.Transaction.Tests
{
	using System;
	using System.Diagnostics.Contracts;
	using System.Transactions;

	using NUnit.Framework;

	public class MyService : IMyService
	{
		private readonly ITransactionManager _Manager;

		public MyService(ITransactionManager manager)
		{
			Contract.Ensures(_Manager != null);
			_Manager = manager;
		}

		[Transaction]
		void IMyService.VerifyInAmbient(Action a)
		{
			Assert.That(Transaction.Current != null,
			            "The current transaction mustn't be null.");

			a();
		}
	}
}