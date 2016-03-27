namespace Castle.NHibIntegration
{
	using System;
	using Core.Configuration;

	public class NhFactoryConfiguration
	{
		private readonly IConfiguration config;

		///<summary>
		///</summary>
		///<param name="config"></param>
		///<exception cref="ArgumentNullException"></exception>
		public NhFactoryConfiguration(IConfiguration config)
		{
			if (config == null) throw new ArgumentNullException("config");

			this.config = config;

			Id = config.Attributes[NhConstants.SessionFactoryIdConfigurationKey];
			Alias = config.Attributes[NhConstants.SessionFactoryAliasConfigurationKey];
			ConfigurationBuilderType = config.Attributes[NhConstants.ConfigurationBuilderConfigurationKey];
		}

		/// <summary>
		/// Get or sets the factory Id
		/// </summary>
		public string Id { get; set; }

		/// <summary>
		/// Gets or sets the factory Alias
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Gets or sets the factory ConfigurationBuilder
		/// </summary>
		public string ConfigurationBuilderType { get; set; }

		/// <summary>
		/// Constructs an IConfiguration instance for this factory
		/// </summary>
		/// <returns></returns>
		public IConfiguration GetConfiguration()
		{
			return config;
		}
	}
}