namespace Castle.NHibIntegration.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class AsyncLocalStoreTestCase
	{
		private AsyncLocal<Something> _asyncLocal = new AsyncLocal<Something>();

		[Test]
		public async Task First()
		{
			// ExecutionContext.Run();
			var dict = GetSomething();
			dict.Val = "0";

			await Depth1();

			dict.Val.Should().Be("2");
		}

		private Task Depth1()
		{
			var tcs = new TaskCompletionSource<bool>();

			var dict = GetSomething();
			dict.Val = "1";

			ThreadPool.QueueUserWorkItem(state =>
			{
				Depth2().Wait();

				tcs.SetResult(true);
				
			}, null);

			return tcs.Task;
		}

		private Task Depth2()
		{
			var dict = GetSomething();
			dict.Val = "2";

			return Task.CompletedTask;
		}

		private Something GetSomething()
		{
			if (_asyncLocal.Value == null)
				_asyncLocal.Value = new Something();

			return _asyncLocal.Value;
		}


		class Something : ICloneable
		{
			public Something()
			{
				Console.WriteLine("ctor");
			}

			public string Val { get; set; }

			public object Clone()
			{
				Console.WriteLine("Clone");
				return this;
			}
		}
	}
}