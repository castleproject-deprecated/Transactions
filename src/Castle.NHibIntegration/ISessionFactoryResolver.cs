namespace Castle.NHibIntegration
{
	using System;
	using MicroKernel.Facilities;
	using NHibernate;

	/// <summary>
	/// Dictates the contract for possible different approach 
	/// of session factories obtention.
	/// </summary>
	/// <remarks>
	/// Inspired on Cuyahoga project
	/// </remarks>
	public interface ISessionFactoryResolver
	{
		/// <summary>
		/// Invoked by the facility while the configuration 
		/// node are being interpreted.
		/// </summary>
		/// <param name="alias">
		/// The alias associated with the session factory on the configuration node
		/// </param>
		/// <param name="componentKey">
		/// The component key associated with the session factory on the kernel
		/// </param>
		void RegisterAliasComponentIdMapping(string alias, string componentKey);

		/// <summary>
		/// Implementors should return a session factory 
		/// instance for the specified alias configured previously.
		/// </summary>
		/// <param name="alias">
		/// The alias associated with the session factory on the configuration node
		/// </param>
		/// <returns>
		/// A session factory instance
		/// </returns>
		/// <exception cref="FacilityException">
		/// If the alias is not associated with a session factory
		/// </exception>
		ISessionFactory GetSessionFactory(String alias);
	}
}