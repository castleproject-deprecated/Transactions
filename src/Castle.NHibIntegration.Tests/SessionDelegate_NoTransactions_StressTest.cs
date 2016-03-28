namespace Castle.NHibIntegration.Tests
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Comps;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class SessionDelegate_NoTransactions_StressTest : BaseTest
	{
		[Test]
		public async Task Async_NoTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithoutTransactions>();
			var tasks = new List<Task>(); 
			var total = 5000;

			// Act
			for (int i = 0; i < total; i++)
			{
				var task = Task.Factory.StartNew(() => comp.Async()).Unwrap();
				tasks.Add(task);
			}

			Task.WaitAll(tasks.ToArray());

			await Task.Delay(100);

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}
	}
}