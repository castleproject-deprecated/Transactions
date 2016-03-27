namespace Castle.Services.Transaction2.Tests
{
	using System;
	using System.Threading.Tasks;
	using Comps;
	using Facility;
	using FluentAssertions;
	using MicroKernel.Facilities;
	using MicroKernel.Registration;
	using NUnit.Framework;
	using Transaction;
	using Windsor;

	[TestFixture]
	public class AutoTxFacilityTestCase
	{
		private WindsorContainer _container;
		private ITransactionManager2 _manager;

		[SetUp]
		public void Init()
		{
			_container = new WindsorContainer();
			_container.AddFacility<AutoTx2Facility>();
			_manager = _container.Resolve<ITransactionManager2>();
		}

		[Test]
		public void Validate_components_withouth_virtuals()
		{
			Assert.Throws<FacilityException>(() => _container.Register(Component.For<ProblemComp1>()));
		}

		[Test]
		public void Intercepting_Sync_method()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			comp.Sync(resource);

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(1);
			resource.RolledBack.Should().Be(0);
		}

		[Test]
		public void Intercepting_Sync_method_that_throws()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			Assert.Throws<Exception>(() => comp.SyncThatThrows(resource)).Message.Should().Be("fake");

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(0);
			resource.RolledBack.Should().Be(1);
		}

		[Test]
		public async Task Intercepting_ASync_method()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			await comp.ASync(resource);

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(1);
			resource.RolledBack.Should().Be(0);
		}

		[Test]
		public void Intercepting_ASync_method_that_faults_0()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			Assert.Throws<Exception>(() => comp.ASyncThatThrows0(resource)).Message.Should().Be("fake 0");

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(0);
			resource.RolledBack.Should().Be(1);
		}

		[Test]
		public void Intercepting_ASync_method_that_faults_1()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			Assert.Throws<AggregateException>(() => comp.ASyncThatThrows1(resource).Wait())
				.InnerException.Message.Should().Be("fake 1");

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(0);
			resource.RolledBack.Should().Be(1);
		}

		[Test]
		public void Intercepting_ASync_method_that_faults_2()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			Assert.Throws<AggregateException>(() => comp.ASyncThatThrows2(resource).Wait())
				.InnerException.Message.Should().Be("fake 2");

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(0);
			resource.RolledBack.Should().Be(1);
		}

		[Test]
		public async Task Intercepting_ASync_method_that_faults_3()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			Assert.Throws<AggregateException>(() => comp.ASyncThatThrows3(resource).Wait())
				.InnerException.Message.Should().Be("fake 3");

			await Task.Delay(100); // continuation doesnt run immediately

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(0);
			resource.RolledBack.Should().Be(1);
		}

		[Test]
		public async Task Intercepting_ASync_method_that_completes_asynchronously()
		{
			// Arrange
			_container.Register(Component.For<TransactionalComponent>());
			var comp = _container.Resolve<TransactionalComponent>();
			var resource = new EnlistedConfirmation();

			// Act
			await comp.ASyncThatCompletesAsync(resource);

			await Task.Delay(100); // continuation doesnt run immediately

			// Assert
			_manager.CurrentTransaction.Should().BeNull();
			resource.Committed.Should().Be(1);
			resource.RolledBack.Should().Be(0);
		}

		
	}
}
