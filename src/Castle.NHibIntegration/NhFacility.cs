namespace Castle.NHibIntegration
{
	using System;
	using System.Configuration;
	using AutoClosing;
	using Core.Configuration;
	using Core.Logging;
	using Internal;
	using MicroKernel.Facilities;
	using MicroKernel.Registration;
	using NHibernate;
	using Services.Transaction;
	using ILoggerFactory = Core.Logging.ILoggerFactory;


	public class NhFacility : AbstractFacility
	{
		private readonly IConfigurationBuilder configurationBuilder;

		private ILogger log = NullLogger.Instance;
		private Type customConfigurationBuilderType;
		private readonly NhFacilityConfiguration facilitySettingConfig;

		public NhFacility(IConfigurationBuilder configurationBuilder)
		{
			this.configurationBuilder = configurationBuilder;
			facilitySettingConfig = new NhFacilityConfiguration(configurationBuilder);
		}

		public NhFacility() : this(new DefaultConfigurationBuilder())
		{
		}

		protected override void Init()
		{
			if (Kernel.HasComponent(typeof(ILoggerFactory)))
			{
				log = Kernel.Resolve<ILoggerFactory>().Create(GetType());
			}

			facilitySettingConfig.Init(FacilityConfig, Kernel);

			AssertHasConfig();
			AssertHasAtLeastOneFactoryConfigured();
			RegisterComponents();
			ConfigureFacility();

			// New Auto close support
			Kernel.Register(
				Component.For<AutoSessionCloseInterceptor>().LifeStyle.Transient,
				Component.For<INhMetaInfoStore>().ImplementedBy<NhMetaInfoStore>()
					.Named("autoclosesess.metaInfoStore")
					.LifeStyle.Singleton
				);

			Kernel.ComponentModelBuilder.AddContributor(new SessionAttributeComponentInspector());
		}

		protected virtual void RegisterComponents()
		{
			RegisterDefaultConfigurationBuilder();
			RegisterSessionFactoryResolver();
			RegisterSessionStore();
			RegisterSessionManager();
			VerifyIsTxManagerIsPresent();
		}

		/// <summary>
		/// Register <see cref="IConfigurationBuilder"/> the default ConfigurationBuilder or (if present) the one 
		/// specified via "configurationBuilder" attribute.
		/// </summary>
		private void RegisterDefaultConfigurationBuilder()
		{
			if (!facilitySettingConfig.HasConcreteConfigurationBuilder())
			{
				customConfigurationBuilderType = facilitySettingConfig.GetConfigurationBuilderType();

				if (facilitySettingConfig.HasConfigurationBuilderType())
				{
					if (!typeof(IConfigurationBuilder).IsAssignableFrom(customConfigurationBuilderType))
					{
						throw new FacilityException(
							string.Format(
								"ConfigurationBuilder type '{0}' invalid. The type must implement the IConfigurationBuilder contract",
								customConfigurationBuilderType.FullName));
					}
				}

				Kernel.Register(Component
									.For<IConfigurationBuilder>()
									.ImplementedBy(customConfigurationBuilderType)
									.Named(NhConstants.DefaultConfigurationBuilderKey));
			}
			else
				Kernel.Register(
					Component.For<IConfigurationBuilder>().Instance(configurationBuilder).Named(NhConstants.DefaultConfigurationBuilderKey));
		}

		/// <summary>
		/// Registers <see cref="SessionFactoryResolver"/> as the session factory resolver.
		/// </summary>
		protected void RegisterSessionFactoryResolver()
		{
			Kernel.Register(
				Component.For<ISessionFactoryResolver>()
					.ImplementedBy<SessionFactoryResolver>()
					.Named(NhConstants.SessionFactoryResolverKey).LifeStyle.Singleton
				);
		}

		/// <summary>
		/// Checks if a <see cref="ITransactionManager2"/> is registered as the transaction manager.
		/// </summary>
		protected void VerifyIsTxManagerIsPresent()
		{
			if (!Kernel.HasComponent(typeof(ITransactionManager2)))
			{
				log.Warn("No Transaction Manager registered on container");
			}
		}

		/// <summary>
		/// Registers the configured session store.
		/// </summary>
		protected void RegisterSessionStore()
		{
			Kernel.Register(
				Component.For<ISessionStore>()
					.ImplementedBy(facilitySettingConfig.GetSessionStoreType())
					.Named(NhConstants.SessionStoreKey)
			);
		}

		/// <summary>
		/// Registers <see cref="DefaultSessionManager"/> as the session manager.
		/// </summary>
		protected void RegisterSessionManager()
		{
			string defaultFlushMode = facilitySettingConfig.FlushMode;

			if (!string.IsNullOrEmpty(defaultFlushMode))
			{
				var confignode = new MutableConfiguration(NhConstants.SessionManagerKey);

				IConfiguration properties = new MutableConfiguration("parameters");
				confignode.Children.Add(properties);

				properties.Children.Add(new MutableConfiguration("DefaultFlushMode", defaultFlushMode));

				Kernel.ConfigurationStore.AddComponentConfiguration(NhConstants.SessionManagerKey, confignode);
			}

			Kernel.Register(Component.For<ISessionManager>().ImplementedBy<DefaultSessionManager>().Named(NhConstants.SessionManagerKey));
		}

		/// <summary>
		/// Configures the facility.
		/// </summary>
		protected void ConfigureFacility()
		{
			var sessionFactoryResolver = Kernel.Resolve<ISessionFactoryResolver>();

			ConfigureReflectionOptimizer();

			var configBuilder = Kernel.Resolve<IConfigurationBuilder>();

			foreach (var factoryConfig in configBuilder.Factories ?? facilitySettingConfig.Factories)
			{
				ConfigureFactories(factoryConfig, sessionFactoryResolver, configBuilder);
			}
		}

		/// <summary>
		/// Reads the attribute <c>useReflectionOptimizer</c> and configure
		/// the reflection optimizer accordingly.
		/// </summary>
		/// <remarks>
		/// As reported on Jira (FACILITIES-39) the reflection optimizer
		/// slow things down. So by default it will be disabled. You
		/// can use the attribute <c>useReflectionOptimizer</c> to turn it
		/// on. 
		/// </remarks>
		private void ConfigureReflectionOptimizer()
		{
			NHibernate.Cfg.Environment.UseReflectionOptimizer = facilitySettingConfig.ShouldUseReflectionOptimizer();
		}

		/// <summary>
		/// Configures the factories.
		/// </summary>
		/// <param name="config">The config.</param>
		/// <param name="sessionFactoryResolver">The session factory resolver.</param>
		/// <param name="configBuilder"></param>
		protected void ConfigureFactories(NhFactoryConfiguration config, ISessionFactoryResolver sessionFactoryResolver, IConfigurationBuilder configBuilder)
		{
			var id = config.Id;

			if (string.IsNullOrEmpty(id))
			{
				const string message = "You must provide a " +
									   "valid 'id' attribute for the 'factory' node. This id is used as key for " +
									   "the ISessionFactory component registered on the container";

				throw new ConfigurationErrorsException(message);
			}

			var alias = config.Alias ?? NhConstants.DefaultAlias;

			var cfg = configBuilder.GetConfiguration(config.GetConfiguration());

			// Registers the Configuration object
			Kernel.Register(Component.For<NHibernate.Cfg.Configuration>().Instance(cfg).Named(String.Format("{0}.cfg", id)));

			// If a Session Factory level interceptor was provided, we use it
			if (Kernel.HasComponent(NhConstants.SessionInterceptorKey))
			{
				cfg.Interceptor = Kernel.Resolve<IInterceptor>(NhConstants.SessionInterceptorKey);
			}

			// Registers the ISessionFactory as a component
			Kernel.Register(Component
								.For<ISessionFactory>()
								.Named(id)
								.Activator<SessionFactoryActivator>()
								.ExtendedProperties(new Property[] { Property.ForKey(NhConstants.SessionFactoryConfiguration).Eq(cfg) })
								.LifeStyle.Singleton);

			sessionFactoryResolver.RegisterAliasComponentIdMapping(alias, id);
		}

		#region FluentConfiguration

		/// <summary>
		/// Sets a custom <see cref="IConfigurationBuilder"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public NhFacility ConfigurationBuilder<T>() where T : IConfigurationBuilder 
		{
			return ConfigurationBuilder(typeof(T));
		}

		/// <summary>
		/// Sets a custom <see cref="IConfigurationBuilder"/>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public NhFacility ConfigurationBuilder(Type type)
		{
			facilitySettingConfig.ConfigurationBuilder(type);
			return this;
		}

		/// <summary>
		/// Sets the facility to work on a web conext
		/// </summary>
		/// <returns></returns>
		public NhFacility IsWeb()
		{
			facilitySettingConfig.IsWeb();
			return this;
		}

		public NhFacility IsHybrid()
		{
			facilitySettingConfig.IsHybrid();
			return this;
		}

		public NhFacility SessionStore(Type type)
		{
			facilitySettingConfig.SessionStore(type);
			return this;
		}

		#endregion

		#region Helper methods

		private void AssertHasAtLeastOneFactoryConfigured()
		{
			if (facilitySettingConfig.HasValidFactory()) return;

			IConfiguration factoriesConfig = FacilityConfig.Children["factory"];

			if (factoriesConfig == null)
			{
				const string message = "You need to configure at least one factory to use the NHibernateFacility";

				throw new ConfigurationErrorsException(message);
			}
		}

		private void AssertHasConfig()
		{
			if (!facilitySettingConfig.IsValid())
			{
				const string message = "The NHibernateFacility requires configuration";

				throw new ConfigurationErrorsException(message);
			}
		}

		#endregion
	}
}
