namespace Castle.NHibIntegration.Tests
{
	using System;
	using FluentNHibernate.Cfg;

	public class TestConfigBuilder : AbstractFluentNhConfigurationBuilder
	{
		protected override void ConfigureMappings(MappingConfiguration m)
		{
			m.FluentMappings
				.AddFromAssemblyOf<TestConfigBuilder>()
				;
		}
	}

	public class ClientAccount
	{
		public virtual Guid Id { get; set; }
		public virtual int? Counter { get; set; }
	}

	public class ClientAccountMap : FluentNHibernate.Mapping.ClassMap<ClientAccount>
	{
		public ClientAccountMap()
		{
			Table("ClientAccount2");

			this.Not.LazyLoad();

			Id(a => a.Id).GeneratedBy.Assigned();

			Map(a => a.Counter);
		}
	}
}