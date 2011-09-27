﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using NUnit.Framework;
using Castle.Core.Logging;

namespace Castle.Services.Transaction.Tests
{
	public class CorrectDisposalOfITransaction
	{
		private ITransactionManager _Tm;

		[SetUp]
		public void SetUp()
		{
			_Tm = new Services.Transaction.TransactionManager(new TransientActivityManager());
		}

		[Test, Description("This test doesn't explicitly check disposal, but a pass means the bug in previous versions was fixed.")]
		public void Dispose_ITransaction_using_IDisposable_should_run_dispose()
		{
			using (var tx = _Tm.CreateTransaction().Value.Transaction)
				tx.Complete();

			var newTx = _Tm.CreateTransaction().Value.Transaction;
		}

		[Test]
		public void Dispose_ITransaction_using_IDisposable_should_run_action()
		{
			var actionWasCalled = false;

			using (ITransaction tx = new Transaction(new CommittableTransaction(), 1, new DefaultTransactionOptions(), () => actionWasCalled = true, NullLogger.Instance))
				tx.Complete();

			Assert.That(actionWasCalled);
		}
	}
}