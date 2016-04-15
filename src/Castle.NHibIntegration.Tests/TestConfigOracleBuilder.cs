namespace Castle.NHibIntegration.Tests
{
	using System.Configuration;
	using System.Data;
	using Core.Configuration;
	using FluentNHibernate.Cfg;
	using FluentNHibernate.Cfg.Db;
	using Configuration = NHibernate.Cfg.Configuration;

	public class TestConfigOracleBuilder : IConfigurationBuilder
	{
		public Configuration GetConfiguration(IConfiguration config)
		{
			var entry = ConfigurationManager.ConnectionStrings["default"];

			var dbConfig = OracleDataClientConfiguration
				.Oracle10
				.Driver<OracleManagedDataClientDriver>()
				.ConnectionString(entry.ConnectionString)
				.IsolationLevel(IsolationLevel.ReadCommitted);

			var configuration = Fluently
				.Configure()
				.ProxyFactoryFactory<NHibernate.ByteCode.Castle.ProxyFactoryFactory>()
				.Database(dbConfig)
//				.Cache(ConfigureCache)
				.Mappings(ConfigureMappings)
				.ExposeConfiguration(c => c.SetProperty("generate_statistics", "true"))
				// .ExposeConfiguration(c => c.SetProperty("transaction.factory_class", TransactionFactoryType.AssemblyQualifiedName))
				.ExposeConfiguration(c => c.SetProperty("transaction.factory_class", "Castle.NHibIntegration.Tx.AdoNetWithDistributedTransactionFactory, Castle.NHibIntegration"))
//				.ExposeConfiguration(c => c.SetProperty("transaction.factory_class", "Castle.NHibIntegration.Tx.CastleFriendlyScopelessTxFactory, Castle.NHibIntegration"))
				;

			var assembled = configuration.BuildConfiguration();

			// OnConfigurationBuilt(assembled);

			return assembled;
		}

		public NhFactoryConfiguration[] Factories { get; set; }

		protected void ConfigureMappings(MappingConfiguration m)
		{
			m.FluentMappings
				.Add<TestTableMap>()
//				.AddFromAssemblyOf<TestConfigOracleBuilder>()
				;
		}
	}
}