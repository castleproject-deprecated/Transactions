// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
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

using Castle.Core.Logging;
using Castle.IO.Extensions;
using Castle.Transactions.Activities;
using NUnit.Framework;

namespace Castle.Transactions.IO.Tests
{
	[TestFixture]
	public class AsDependentTransaction : TxFTestFixtureBase
	{
		string test_file;
		ITransactionManager subject;

		[SetUp]
		public void given_manager()
		{
			test_file = ".".Combine("test.txt");
			subject = new TransactionManager(new AsyncLocalActivityManager(), NullLogger.Instance);
		}

		[Test]
		public void Then_DependentTransaction_CanBeCommitted()
		{
			// verify process state
			Assert.That(subject.CurrentTransaction.HasValue, Is.False);
			Assert.That(System.Transactions.Transaction.Current, Is.Null);

			// actual test code
			using (var stdTx = subject.CreateTransaction(new DefaultTransactionOptions()).Value.Transaction)
			{
				Assert.That(subject.CurrentTransaction.HasValue, Is.True);
				Assert.That(subject.CurrentTransaction.Value, Is.EqualTo(stdTx));

				using (var innerTransaction = subject.CreateFileTransaction().Value)
				{
					Assert.That(subject.CurrentTransaction.Value, Is.EqualTo(innerTransaction),
					            "Now that we have created a dependent transaction, it's the current tx in the resource manager.");

					// this is supposed to be registered in an IoC container
					//var fa = (IFileAdapter)innerTransaction;
					//fa.WriteAllText(test_file, "Hello world");

					innerTransaction.Complete();
				}
			}

			//Assert.That(File.Exists(test_file));
			//Assert.That(File.ReadAllText(test_file), Is.EqualTo("Hello world"));
		}

		[Test]
		public void CompletedState()
		{
			using (var tx = subject.CreateFileTransaction().Value)
			{
				Assert.That(tx.State, Is.EqualTo(TransactionState.Active));
				tx.Complete();
				Assert.That(tx.State, Is.EqualTo(TransactionState.CommittedOrCompleted));
			}
		}

		[TearDown]
		public void tear_down()
		{
			subject.Dispose();
		}
	}
}