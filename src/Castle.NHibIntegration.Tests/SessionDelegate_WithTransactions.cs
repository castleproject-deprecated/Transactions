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
			var resource = new EnlistedConfirmation();

			// Act
			comp.Sync(resource);

			// Assert
			resource.Committed.Should().Be(1);
			resource.RolledBack.Should().Be(0);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			// _sessionFactoryOracle.Statistics.TransactionCount.Should().Be(1);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public void Sync_WithTransactions_AbortingTransaction()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();
			var resource = new EnlistedConfirmation();

			// Act
			Assert.Throws<Exception>(() => comp.SyncThatFails(resource))
				.Message.Should().Be("fake");

			// Assert
			resource.Committed.Should().Be(0);
			resource.RolledBack.Should().Be(1);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			CountTestTableOracle().Should().Be(0);
		}

		[Test]
		public void Sync_WithTwoDatabases_WithTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();
			var resource = new EnlistedConfirmation();

			// Act
			comp.SyncWithTwoConnections(resource);

			// Assert
			resource.Committed.Should().Be(1);
			resource.RolledBack.Should().Be(0);
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			// _sessionFactoryOracle.Statistics.TransactionCount.Should().Be(1);
			CountTestTableMsSql().Should().Be(1);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public void Sync_WithTwoDatabases_AbortingTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			Assert.Throws<Exception>(() => comp.SyncWithTwoConnectionsThatFails(true))
				.Message.Should().Be("fake");

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(0);
			CountTestTableMsSql().Should().Be(0);
		}

		[Test]
		public void Sync_WithTwoDatabases_AbortingTransactions2()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			Assert.Throws<Exception>(() => comp.SyncWithTwoConnectionsThatFails(false)).Message.Should().Be("fake");

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(0);
			CountTestTableMsSql().Should().Be(0);
		}

		[Test]
		public async Task Async_CompletingSync()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.AsyncCompletingSync();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(2);
		}

		[Test]
		public async Task Async_CompletingSync2()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.Async2CompletingSyncWithoutRootSession();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(2);
		}

		[Test]
		public async Task Async_CompletingAsync()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.Async3CompletingAsync();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public async Task Async_CompletingAsync2()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.Async4CompletingAsyncWithoutRoot();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public async Task Async_CompletingSyncWithTwoDbConnections()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.AsyncWithTwoDbsCompletingSync();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
			CountTestTableMsSql().Should().Be(1);
		}

		[Test]
		public async Task Async_CompletingSyncWithTwoDbConnections2()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.AsyncWithTwoDbsCompletingSync2();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
			CountTestTableMsSql().Should().Be(1);
		}

		[Test]
		public async Task Async_CompletingAsyncWithTwoDbConnections()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			await comp.AsyncWithTwoDbsCompletingASync(shouldFail: false);

			await Task.Delay(400);

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
			CountTestTableMsSql().Should().Be(1);
		}

		[Test]
		public async Task Async_CompletingAsyncWithTwoDbConnectionsRollingBack()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			Assert.Throws<AggregateException>(() => comp.AsyncWithTwoDbsCompletingASync(shouldFail: true).Wait())
				.InnerExceptions[0].Message.Should()
				.Be("fake");

			await Task.Delay(250);

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(0);
			CountTestTableMsSql().Should().Be(0);
		}

		[Test]
		public async Task Async_CompletingAsyncWitFaultedTask()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithTransactions>();

			// Act
			Assert.Throws<AggregateException>(() => comp.AsyncWithLateFaultedTask().Wait())
				.InnerExceptions[0].Message.Should()
				.Be("fake");

			await Task.Delay(250);

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			CountTestTableOracle().Should().Be(0);
			CountTestTableMsSql().Should().Be(0);
		}
	}
}