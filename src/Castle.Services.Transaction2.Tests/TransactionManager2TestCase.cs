namespace Castle.Services.Transaction2.Tests
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using Internal;
	using NUnit.Framework;
	using Transaction;

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

			Thread.Sleep(1000);

			var xt = _tm.CurrentTransaction;
			Console.WriteLine(xt);

			// tx.Dispose();
		}

		private async Task Branch1()
		{
			var tx = _tm.CreateTransaction(TransactionOptions.RequiresNewReadCommitted);
			tx.Should().NotBeNull();

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
					_tm.CurrentTransaction.Should().NotBeNull();

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
