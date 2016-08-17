namespace Castle.NHibIntegration
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using Core.Configuration;
	using Core.Internal;
	using Internal;
	using MicroKernel;
	using MicroKernel.Facilities;
	using MicroKernel.SubSystems.Conversion;

	internal class NhFacilityConfiguration
	{
		private IConfigurationBuilder configurationBuilderInstance;
		private Type configurationBuilderType;
		private IConfiguration facilityConfig;
		private bool isWeb;
		private IKernel kernel;
		private Type customStore;
		private bool isHybrid;


		public IEnumerable<NhFactoryConfiguration> Factories { get; set; }

		///<summary>
		///</summary>
		///<param name="configurationBuilderInstance"></param>
		public NhFacilityConfiguration(IConfigurationBuilder configurationBuilderInstance)
		{
			this.configurationBuilderInstance = configurationBuilderInstance;
			Factories = Enumerable.Empty<NhFactoryConfiguration>();
		}

		public bool OnWeb
		{
			get { return isWeb; }
		}

		public string FlushMode { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="con"></param>
		/// <param name="ker"></param>
		public void Init(IConfiguration con, IKernel ker)
		{
			facilityConfig = con;
			kernel = ker;

			if (ConfigurationIsValid())
			{
				ConfigureWithExternalConfiguration();
			}
			else
			{
				Factories = new[]
				{
					new NhFactoryConfiguration(new MutableConfiguration("factory"))
					{
						Id = "factory_1"
					}
				};
			}
		}

		private void ConfigureWithExternalConfiguration()
		{
			var customConfigurationBuilder = facilityConfig.Attributes[NhConstants.ConfigurationBuilderConfigurationKey];

			if (!string.IsNullOrEmpty(customConfigurationBuilder))
			{

				var converter = (IConversionManager)kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey);

				try
				{
					ConfigurationBuilder(
						(Type)converter.PerformConversion(customConfigurationBuilder, typeof(Type)));
				}
				catch (ConverterException)
				{
					throw new FacilityException(string.Format(
						"ConfigurationBuilder type '{0}' invalid or not found", customConfigurationBuilder));
				}
			}

			BuildFactories();


			if (facilityConfig.Attributes[NhConstants.CustomStoreConfigurationKey] != null)
			{
				var customStoreType = facilityConfig.Attributes[NhConstants.CustomStoreConfigurationKey];
				var converter = (ITypeConverter)kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey);
				SessionStore((Type)converter.PerformConversion(customStoreType, typeof(Type)));
			}

			FlushMode = facilityConfig.Attributes[NhConstants.DefaultFlushModeConfigurationKey];

			bool.TryParse(facilityConfig.Attributes[NhConstants.IsWebConfigurationKey], out isWeb);
		}

		private bool ConfigurationIsValid()
		{
			return facilityConfig != null && facilityConfig.Children.Count > 0;
		}

		private void BuildFactories()
		{
			Factories = facilityConfig
				.Children
				.Select(config => new NhFactoryConfiguration(config));
		}

		public void ConfigurationBuilder(Type type)
		{
			configurationBuilderInstance = null;
			configurationBuilderType = type;
		}

		public void SessionStore(Type type)
		{
			if (!typeof(ISessionStore).IsAssignableFrom(type))
			{
				var message = "The specified customStore does " +
				              "not implement the interface ISessionStore. Type " + type;
				throw new ConfigurationErrorsException(message);
			}

			customStore = type;
		}

		public void ConfigurationBuilder(IConfigurationBuilder builder)
		{
			configurationBuilderInstance = builder;
		}

		public void IsWeb()
		{
			isWeb = true;
		}

		public void IsHybrid()
		{
			isHybrid = true;
		}


		public bool IsValid()
		{
			return facilityConfig != null || (configurationBuilderInstance != null || configurationBuilderType != null);
		}

		public bool HasValidFactory()
		{
			return Factories.Any();
		}

		public bool ShouldUseReflectionOptimizer()
		{
			if (facilityConfig != null)
			{
				bool result;
				if (bool.TryParse(facilityConfig.Attributes["useReflectionOptimizer"], out result))
					return result;
			}

			return false;
		}

		public bool HasConcreteConfigurationBuilder()
		{
			return configurationBuilderInstance != null && !HasConfigurationBuilderType();
		}

		public Type GetConfigurationBuilderType()
		{
			return configurationBuilderType;
		}

		public bool HasConfigurationBuilderType()
		{
			return configurationBuilderType != null;
		}

		public Type GetSessionStoreType()
		{
			// Default implementation
			// Type sessionStoreType = typeof (ThreadLocalSessionStore);
			Type sessionStoreType = typeof(AsyncLocalSessionStore);

			if (isWeb)
			{
				sessionStoreType = typeof(WebSessionStore);
			}
			else if (isHybrid)
				sessionStoreType = typeof(HybridSessionStore);

			if (customStore != null)
			{
				sessionStoreType = customStore;
			}

			return sessionStoreType;
		}
	}
}