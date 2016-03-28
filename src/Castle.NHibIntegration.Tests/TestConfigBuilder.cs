namespace Castle.NHibIntegration.Tests
{
	using System;
	using System.Configuration;
	using System.Data;
	using Core.Configuration;
	using FluentNHibernate.Cfg;
	using FluentNHibernate.Cfg.Db;
	using Configuration = NHibernate.Cfg.Configuration;


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

	public class TestConfigOracleBuilder : IConfigurationBuilder
	{
		public Configuration GetConfiguration(IConfiguration config)
		{
			var entry = ConfigurationManager.ConnectionStrings["default"];

			var dbConfig = OracleDataClientConfiguration
				.Oracle10
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

	public class TestTable
	{
		public virtual Guid Id { get; set; }
		public virtual int? Counter { get; set; }
	}

	public class TestTable2
	{
		public virtual Guid Id { get; set; }
		public virtual int? Counter { get; set; }
	}

	public class TestTableMap : FluentNHibernate.Mapping.ClassMap<TestTable>
	{
		public TestTableMap()
		{
			Table("TestTable");

			this.Not.LazyLoad();

			Id(a => a.Id).GeneratedBy.Assigned();

			Map(a => a.Counter);
		}
	}


	public class TestTable2Map : FluentNHibernate.Mapping.ClassMap<TestTable2>
	{
		public TestTable2Map()
		{
			Table("TestTable2");

			this.Not.LazyLoad();

			Id(a => a.Id).GeneratedBy.Assigned();

			Map(a => a.Counter);
		}
	}
}