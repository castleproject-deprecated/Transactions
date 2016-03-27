namespace Castle.NHibIntegration
{
	internal class NhConstants
	{
		internal const string DefaultConfigurationBuilderKey = "nhfacility.configuration.builder";
		internal const string ConfigurationBuilderConfigurationKey = "configurationBuilder";
		internal const string SessionFactoryResolverKey = "nhfacility.sessionfactory.resolver";
		internal const string SessionInterceptorKey = "nhibernate.sessionfactory.interceptor";
		internal const string IsWebConfigurationKey = "isWeb";
		internal const string CustomStoreConfigurationKey = "customStore";
		internal const string DefaultFlushModeConfigurationKey = "defaultFlushMode";
		internal const string SessionManagerKey = "nhfacility.sessionmanager";

		internal const string SessionFactoryIdConfigurationKey = "id";
		internal const string SessionFactoryAliasConfigurationKey = "alias";
		internal const string SessionStoreKey = "nhfacility.sessionstore";
		internal const string ConfigurationBuilderForFactoryFormat = "{0}.configurationBuilder";

		/// <summary>
		/// 
		/// </summary>
		public static readonly string DefaultAlias = "nh.facility.default";

		/// <summary>
		/// Key at which the configuration for a specific SessionFactory is stored
		/// </summary>
		public static readonly string SessionFactoryConfiguration = "Configuration";
	}
}