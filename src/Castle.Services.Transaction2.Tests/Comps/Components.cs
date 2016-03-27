namespace Castle.Services.Transaction2.Tests.Comps
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Transactions;
	using FluentAssertions;
	using Transaction;

	public interface IService1
	{
		void Service1();
	}

	public class ProblemComp1 : IService1
	{
		[Transaction]
		public void Service1()
		{
		}

		[Transaction]
		public void Service2()
		{
		}
	}

	public class TransactionalComponent 
	{
		private readonly ITransactionManager2 _manager;

		public TransactionalComponent(ITransactionManager2 manager)
		{
			_manager = manager;
		}

		[Transaction]
		public virtual void Sync(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);

			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);
		}

		[Transaction]
		public virtual void SyncThatThrows(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			throw new Exception("fake");
		}

		[Transaction]
		public virtual async Task ASync(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			await Branch1();
		}

		[Transaction]
		public virtual Task ASyncThatThrows0(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			throw new Exception("fake 0");
		}

		[Transaction]
		public virtual async Task ASyncThatThrows1(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			await BranchThatThrowsImmediately();
		}

		[Transaction]
		public virtual async Task ASyncThatThrows2(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			await BranchThatSetsTheTaskAsFaulted();
		}

		[Transaction]
		public virtual async Task ASyncThatThrows3(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			await BranchThatSetsTheTaskAsFaultedAfterAFewSeconds();
		}

		[Transaction]
		public virtual async Task ASyncThatCompletesAsync(IEnlistmentNotification resource)
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			System.Transactions.Transaction.Current.EnlistVolatile(resource, EnlistmentOptions.None);

			await BranchThatSetsTheTaskAsCompletedAfterAFewSeconds();
		}

		internal Task Branch1()
		{
			_manager.CurrentTransaction.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().NotBeNull();
			System.Transactions.Transaction.Current.Should().Be(_manager.CurrentTransaction.Inner);
			return Task.CompletedTask;
		}

		internal Task BranchThatThrowsImmediately()
		{
			throw new Exception("fake 1");
		}

		internal Task BranchThatSetsTheTaskAsFaulted()
		{
			var tcs = new TaskCompletionSource<bool>();

			tcs.SetException(new Exception("fake 2"));

			return tcs.Task;
		}

		internal Task BranchThatSetsTheTaskAsFaultedAfterAFewSeconds()
		{
			var tcs = new TaskCompletionSource<bool>();

			ThreadPool.QueueUserWorkItem((_) =>
			{
				Thread.Sleep(500);

				tcs.SetException(new Exception("fake 3"));

			}, null);

			return tcs.Task;
		}

		internal Task BranchThatSetsTheTaskAsCompletedAfterAFewSeconds()
		{
			var tcs = new TaskCompletionSource<bool>();

			ThreadPool.QueueUserWorkItem((_) =>
			{
				Thread.Sleep(500);

				tcs.SetResult(true);

			}, null);

			return tcs.Task;
		}
	}
}
