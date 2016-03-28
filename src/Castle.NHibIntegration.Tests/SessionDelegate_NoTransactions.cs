namespace Castle.NHibIntegration.Tests
{
	using System.Threading.Tasks;
	using Comps;
	using FluentAssertions;
	using NUnit.Framework;


	[TestFixture]
	public class SessionDelegate_NoTransactions : BaseTest
	{
		[Test]
		public void Sync_NoTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithoutTransactions>();

			// Act
			comp.Sync();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public async Task Async_NoTransactions()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithoutTransactions>();

			// Act
			await comp.Async();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public async Task Async_NoTransactions2()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithoutTransactions>();

			// Act
			await comp.Async2();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}

		[Test]
		public async Task Async_NoTransactions3()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithoutTransactions>();

			// Act
			await comp.Async2();

			// Assert
			_sessionStore.TotalStoredCurrent.Should().Be(0);
		}
    }
}
