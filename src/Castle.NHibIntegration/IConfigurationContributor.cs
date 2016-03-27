namespace Castle.NHibIntegration
{
	using System.Configuration;

	/// <summary>
	/// Allows implementors to modify <see cref="Configuration"/>
	/// </summary>
	public interface IConfigurationContributor
	{
		/// <summary>
		/// Modifies available <see cref="Configuration"/> instances.
		/// </summary>
		/// <param name="name">Name of the session factory</param>
		/// <param name="config">The config for sessionFactory</param>
		void Process(string name, Configuration config);
	}
}