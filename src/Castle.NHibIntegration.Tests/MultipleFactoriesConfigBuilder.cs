namespace Castle.NHibIntegration.Tests
{
	using Core.Configuration;
	using NHibernate.Cfg;

	public class MultipleFactoriesConfigBuilder : IConfigurationBuilder
	{
		public MultipleFactoriesConfigBuilder()
		{
			this.Factories = new []
			{
				new NhFactoryConfiguration(new MutableConfiguration("oracle")) { Id = "sessfactory.oracle" },
				new NhFactoryConfiguration(new MutableConfiguration("mssql")) { Alias = "mssql", Id = "sessfactory.mssql" },
			};
		}

		public Configuration GetConfiguration(IConfiguration config)
		{
			if (config.Name == "oracle")
			{
				return new TestConfigOracleBuilder().GetConfiguration(config);
			}

			return new TestConfigSqlServerBuilder().GetConfiguration(config);
		}

		public NhFactoryConfiguration[] Factories { get; set; }
	}
}