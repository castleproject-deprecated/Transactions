namespace Castle.NHibIntegration.Tests
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Comps;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class SessionDelegate_WithTransactions_StressTest : BaseTest
	{
		[Test]
		public async Task Stress_Sync()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();
			var resource = new EnlistedConfirmation();
			var tasks = new List<Task>();
			var total = 1000;

			// Act
			for (int i = 0; i < total; i++)
			{
				var task = Task.Factory.StartNew(() => comp.Sync(resource));
				tasks.Add(task);
			}

			Task.WaitAll(tasks.ToArray());

			await Task.Delay(100);

			// Assert
			resource.RolledBack.Should().Be(0);
			resource.Committed.Should().Be(total);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(total);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(total);
			// _sessionFactoryOracle.Statistics.TransactionCount.Should().Be(1);
			CountTestTableOracle().Should().Be(total);
		}

		[Test]
		public void Stress_ASync()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();
			var resource = new EnlistedConfirmation();
			var tasks = new List<Task>();
			var total = 1000;

			// Act
			for (int i = 0; i < total; i++)
			{
				var task = Task.Factory.StartNew(() => comp.AsyncCompletingSync(resource));
				tasks.Add(task);
			}

			Task.WaitAll(tasks.ToArray());

			// Assert
			resource.Committed.Should().Be(total);
			resource.RolledBack.Should().Be(0);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(total);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(total);
			// _sessionFactoryOracle.Statistics.TransactionCount.Should().Be(1);
			CountTestTableOracle().Should().Be(total * 2);
		}
	}
}