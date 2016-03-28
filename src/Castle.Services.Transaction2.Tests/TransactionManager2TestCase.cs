namespace Castle.Services.Transaction2.Tests
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using NUnit.Framework;
	using Transaction;
	using Transaction.Internal;


	[TestFixture]
	public class TransactionManager2TestCase
    {
		private TransactionManager2 _tm;

		[Test]
		public async Task CurrentTransaction_reflects_currentstate_per_activity()
		{
			_tm = new TransactionManager2(new AsyncLocalActivityManager());

			_tm.CurrentTransaction.Should().BeNull();

			await Branch1();

			_tm.CurrentTransaction.Should().BeNull();
		}

		private async Task Branch1()
		{
			var tx = _tm.CreateTransaction(TransactionOptions.RequiresNewReadCommitted);
			tx.Should().NotBeNull();

			System.Transactions.Transaction.Current.Should().Be(tx.Inner);

			await Branch1Child();

			tx.Dispose();
		}

		private Task Branch1Child()
		{
			var tcs = new TaskCompletionSource<bool>();

			ThreadPool.QueueUserWorkItem((s) =>
			{
				try
				{
					var curtm = _tm.CurrentTransaction;

					curtm.Should().NotBeNull();

					System.Transactions.Transaction.Current.Should().Be(curtm.Inner);

					tcs.SetResult(true);
				}
				catch (Exception e)
				{
					tcs.SetException(e);
				}
			});

			return tcs.Task;
		}
    }
}
