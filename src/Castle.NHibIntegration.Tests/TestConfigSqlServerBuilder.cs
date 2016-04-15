namespace Castle.NHibIntegration.Tests
{
	using System.Configuration;
	using System.Data;
	using Core.Configuration;
	using FluentNHibernate.Cfg;
	using Configuration = NHibernate.Cfg.Configuration;

	public class TestConfigSqlServerBuilder : IConfigurationBuilder
	{
		public Configuration GetConfiguration(IConfiguration config)
		{
			var entry = ConfigurationManager.ConnectionStrings["sqlserver"];

			var dbConfig = FluentNHibernate.Cfg.Db.MsSqlConfiguration
				.MsSql2012
				.ConnectionString(entry.ConnectionString)
				.IsolationLevel(IsolationLevel.ReadCommitted);

			var configuration = Fluently
				.Configure()
				.ProxyFactoryFactory<NHibernate.ByteCode.Castle.ProxyFactoryFactory>()
				.Database(dbConfig)
				// .Cache(ConfigureCache)
				.Mappings(ConfigureMappings)
				.ExposeConfiguration(c => c.SetProperty("generate_statistics", "true"))
				// .ExposeConfiguration(c => c.SetProperty("transaction.factory_class", TransactionFactoryType.AssemblyQualifiedName))
				.ExposeConfiguration(c => c.SetProperty("transaction.factory_class", "Castle.NHibIntegration.Tx.AdoNetWithDistributedTransactionFactory, Castle.NHibIntegration"))
//				.ExposeConfiguration(c => c.SetProperty("transaction.factory_class", "Castle.NHibIntegration.Tx.CastleFriendlyScopelessTxFactory, Castle.NHibIntegration"))
				;

			var assembled = configuration.BuildConfiguration();

			return assembled;
		}

		public NhFactoryConfiguration[] Factories { get; set; }

		protected void ConfigureMappings(MappingConfiguration m)
		{
			m.FluentMappings
				.Add<TestTable2Map>()
				// .AddFromAssemblyOf<TestConfigOracleBuilder>()
				;
		}
	}
}