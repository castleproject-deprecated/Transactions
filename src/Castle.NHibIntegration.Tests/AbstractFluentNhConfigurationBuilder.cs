namespace Castle.NHibIntegration.Tests
{
	using System.Configuration;
	using System.Data;
	using System.Linq;
	using System.Reflection;
	using Core.Configuration;
	using FluentNHibernate.Cfg;
	using FluentNHibernate.Cfg.Db;
	using Configuration = NHibernate.Cfg.Configuration;


	public abstract class AbstractFluentNhConfigurationBuilder : IConfigurationBuilder
	{
		public virtual NhFactoryConfiguration[] Factories { get; set; } 

		public virtual Configuration GetConfiguration(IConfiguration config)
		{
			try
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
					.Cache(ConfigureCache)
					.Mappings(ConfigureMappings)
//					.ExposeConfiguration(c => c.SetProperty("generate_statistics", GenerateStatistics.ToString()))
//					.ExposeConfiguration(c => c.SetProperty("transaction.factory_class", TransactionFactoryType.AssemblyQualifiedName))
					;

				var assembled = configuration.BuildConfiguration();

//				OnConfigurationBuilt(assembled);

				return assembled;
			}
			catch (FluentConfigurationException e)
			{
				var reasons = "Potential Reasons: " + string.Join(",", e.PotentialReasons);

				if (e.InnerException is ReflectionTypeLoadException)
				{
					var typeLoad = (ReflectionTypeLoadException)e.InnerException;

					reasons += "Loader: " + string.Join(",", typeLoad.LoaderExceptions.Select(ex => ex.Message));
				}

				throw new FluentConfigurationException(e.Message + "\r\n" + reasons, e);
			}
		}

		protected virtual void ConfigureCache(CacheSettingsBuilder cacheSettings)
		{
		}

		protected abstract void ConfigureMappings(MappingConfiguration m);
	}
}