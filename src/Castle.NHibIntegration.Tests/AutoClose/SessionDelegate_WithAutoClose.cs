namespace Castle.NHibIntegration.Tests.AutoClose
{
	using System;
	using Comps;
	using FluentAssertions;
	using NUnit.Framework;


	[TestFixture]
	public class SessionDelegate_WithAutoClose : BaseTest
	{
		[Test]
		public void Sync_NoExplicitSessionManagement()
		{
			// Arrange
			var comp = _container.Resolve<SvcAutoWithoutTransactions>();

			// Act
			comp.NoExplicitSessionManagement();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.TransactionCount.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public void Sync_ExplicitSessionManagement()
		{
			// Arrange
			var comp = _container.Resolve<SvcAutoWithoutTransactions>();

			// Act
			comp.ExplicitSessionManagement();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.TransactionCount.Should().Be(0);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public void Sync_NoExplicitSessionManagement_ManyCalls()
		{
			// Arrange
			var comp = _container.Resolve<SvcAutoWithoutTransactions>();

			// Act
			comp.NoExplicitSessionManagement_MultCall();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.TransactionCount.Should().Be(0);
			CountTestTableOracle().Should().Be(3);
		}

		[Test]
		public void Sync_ExplicitSessionManagement_WithTransaction()
		{
			// Arrange
			var comp = _container.Resolve<SvcAutoWithTransactions>();

			// Act
			comp.ExplicitSessionManagement();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.TransactionCount.Should().Be(2);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public void Sync_NoExplicitSessionManagement_WithTransaction()
		{
			// Arrange
			var comp = _container.Resolve<SvcAutoWithTransactions>();

			// Act
			comp.NoExplicitSessionManagement();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			// _sessionFactoryOracle.Statistics.TransactionCount.Should().Be(1);
			CountTestTableOracle().Should().Be(1);
		}

		[Test]
		public void Sync_TransactionThenAutoClose()
		{
			// Arrange
			var comp = _container.Resolve<SvcAutoWithTransactions>();

			// Act
			comp.TransactionThenAutoClose();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
			_sessionFactoryOracle.Statistics.SessionOpenCount.Should().Be(1);
			_sessionFactoryOracle.Statistics.SessionCloseCount.Should().Be(1);
			// _sessionFactoryOracle.Statistics.TransactionCount.Should().Be(1);
			CountTestTableOracle().Should().Be(1);
		}
	}
}
