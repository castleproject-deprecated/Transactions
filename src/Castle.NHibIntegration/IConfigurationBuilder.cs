namespace Castle.NHibIntegration
{
	using Core.Configuration;

	/// <summary>
	/// Builds up the Configuration object
	/// </summary>
	public interface IConfigurationBuilder
	{
		/// <summary>
		/// Builds the Configuration object from the specifed configuration
		/// </summary>
		NHibernate.Cfg.Configuration GetConfiguration(IConfiguration config);

		NhFactoryConfiguration[] Factories { get; set; }
	}
}