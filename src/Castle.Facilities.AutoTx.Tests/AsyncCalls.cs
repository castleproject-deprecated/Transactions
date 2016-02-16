namespace Castle.Facilities.AutoTx.Tests
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Transactions;
	using MicroKernel.Registration;
	using NUnit.Framework;
	using Services.Transaction;
	using SharpTestsEx;
	using Windsor;

	[TestFixture]
	public class AsyncCalls
	{
		private WindsorContainer _container;

		[SetUp]
		public void SetUp()
		{
			_container = new WindsorContainer();
			_container.AddFacility(new AutoTxFacility());
			_container.Register(Component.For<Svc>());
		}

		[TearDown]
		public void TearDown()
		{
			_container.Dispose();
		}

		[Test]
		public async Task TaskReturningService_with_sync_impl()
		{
			var svc = _container.Resolve<Svc>();

			var tx = await svc.SyncOp();
			tx.State.Should().Be(TransactionState.Disposed);
		}

		[Test]
		public async Task TaskReturningService_with_async_impl()
		{
			var svc = _container.Resolve<Svc>();

			var tx = await svc.AsyncOp();

			await Task.Delay(100); // 

			tx.State.Should().Be(TransactionState.Disposed);
		}

		[Test]
		public async Task TaskReturningService_with_async_impl2()
		{
			var svc = _container.Resolve<Svc>();

			var tx = await svc.AsyncOp2();

			await Task.Delay(100); // 
		}

		[Test]
		public async Task TaskReturningService_with_async_impl3()
		{
			var svc = _container.Resolve<Svc>();

			var tx = await svc.AsyncOp3();

			await Task.Delay(100); // 

			tx.State.Should().Be(TransactionState.Disposed);
		}
	}

	public class Svc
	{
		private readonly ITransactionManager _manager;

		public Svc(ITransactionManager manager)
		{
			_manager = manager;
		}

		[Transaction]
		public virtual Task<ITransaction> SyncOp()
		{
			var tcs = new TaskCompletionSource<ITransaction>();
			
			tcs.SetResult(_manager.CurrentTransaction.Value);

			return tcs.Task;
		}

		[Transaction]
		public virtual Task<ITransaction> AsyncOp()
		{
			var tcs = new TaskCompletionSource<ITransaction>();

			var tx = _manager.CurrentTransaction.Value;

			Task.Factory.StartNew(async () =>
			{
				await Task.Delay(300);

				var tx2 = _manager.CurrentTransaction.Value;


				if (tx != null && tx2 != null && tx.LocalIdentifier == tx2.LocalIdentifier)
					tcs.SetResult(tx2);
				else
					tcs.SetException(new Exception("different tx?"));

			}, TaskCreationOptions.AttachedToParent);

			return tcs.Task;
		}

		[Transaction]
		public virtual Task<System.Transactions.Transaction> AsyncOp2()
		{
			var tcs = new TaskCompletionSource<System.Transactions.Transaction>();

			var tx = System.Transactions.Transaction.Current;

			Task.Factory.StartNew(async () =>
			{
				await Task.Delay(300);

				var tx2 = _manager.CurrentTransaction.Value.Inner;


				if (tx != null && tx2 != null && 
					tx.TransactionInformation.LocalIdentifier == tx2.TransactionInformation.LocalIdentifier)
					tcs.SetResult(tx2);
				else
					tcs.SetException(new Exception("different tx?"));

			}, TaskCreationOptions.AttachedToParent);

			return tcs.Task;
		}

		[Transaction]
		public virtual async Task<ITransaction> AsyncOp3()
		{
			var tx = _manager.CurrentTransaction.Value;

			await Task.Delay(300);

			// continuation code here:

			var tx2 = _manager.CurrentTransaction.Value;

			if (tx != null && tx2 != null && tx.LocalIdentifier == tx2.LocalIdentifier)
				return tx;

			throw new Exception("different tx?");
		}
	}
}