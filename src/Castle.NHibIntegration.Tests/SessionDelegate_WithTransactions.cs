namespace Castle.NHibIntegration.Tests
{
	using System;
	using System.Threading.Tasks;
	using Comps;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class SessionDelegate_WithTransactions : BaseTest
	{
		[Test]
		public void Sync_WithTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			comp.Sync();

			// Assert
			CountTestTableOracle().Should().Be(1);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public void Sync_WithTwoConnections_WithTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			comp.SyncWithTwoConnections();

			// Assert
			CountTestTableOracle().Should().Be(1);
			CountTestTableMsSql().Should().Be(1);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public void Sync_WithTransactions_AbortingTransaction()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			Assert.Throws<Exception>(() => comp.SyncThatFails()).Message.Should().Be("fake");

			// Assert
			CountTestTableOracle().Should().Be(0);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public async Task Async_NoTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.Async();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public async Task Async_NoTransactions2()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.Async2();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public async Task Async_NoTransactions3()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.Async2();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}
	}
}