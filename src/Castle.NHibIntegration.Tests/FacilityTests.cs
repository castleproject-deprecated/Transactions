namespace Castle.NHibIntegration.Tests
{
	using Comps;
	using MicroKernel.Registration;
	using NUnit.Framework;
	using Services.Transaction2.Facility;
	using Windsor;
	using Windsor.Configuration.Interpreters;

	[TestFixture]
	public class FacilityTests
	{
		private WindsorContainer _container;

		[SetUp]
		public void Init()
		{
			_container = new WindsorContainer();
			_container.AddFacility<AutoTx2Facility>();
			_container.AddFacility(new NhFacility(new TestConfigBuilder()));

			_container.Register(
				Component.For<SvcWithTransactions>(),
				Component.For<SvcWithoutTransactions>()
			);
		}

		[Test]
		public void Test1()
		{
			// Arrange
			var comp = _container.Resolve<SvcWithoutTransactions>();

			// Act
			comp.Sync();

			// Assert
		}
    }
}
