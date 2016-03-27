namespace Castle.NHibIntegration.Internal
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using MicroKernel;
	using MicroKernel.Facilities;
	using NHibernate;


	/// <summary>
	/// Default implementation of <see cref="ISessionFactoryResolver"/>
	/// that always queries the kernel instance for the session factory instance.
	/// <para>
	/// This gives a chance to developers replace the session factory instance 
	/// during the application lifetime.
	/// </para>
	/// </summary>
	/// <remarks>
	/// Inspired on Cuyahoga project
	/// </remarks>
	public class SessionFactoryResolver : ISessionFactoryResolver
	{
		private readonly IKernel kernel;
		private readonly IDictionary alias2Key = new HybridDictionary(true);

		/// <summary>
		/// Constructs a SessionFactoryResolver
		/// </summary>
		/// <param name="kernel">
		/// Kernel instance supplied by the container itself
		/// </param>
		public SessionFactoryResolver(IKernel kernel)
		{
			this.kernel = kernel;
		}

		/// <summary>
		/// Associated the alias with the component key
		/// </summary>
		/// <param name="alias">
		/// The alias associated with the session 
		/// factory on the configuration node
		/// </param>
		/// <param name="componentKey">
		/// The component key associated with 
		/// the session factory on the kernel
		/// </param>
		public void RegisterAliasComponentIdMapping(String alias, String componentKey)
		{
			if (alias2Key.Contains(alias))
			{
				throw new ArgumentException("A mapping already exists for " +
											"the specified alias: " + alias);
			}

			alias2Key.Add(alias, componentKey);
		}

		/// <summary>
		/// Returns a session factory instance associated with the
		/// specified alias.
		/// </summary>
		/// <param name="alias">
		/// The alias associated with the session 
		/// factory on the configuration node
		/// </param>
		/// <returns>A session factory instance</returns>
		/// <exception cref="FacilityException">
		/// If the alias is not associated with a session factory
		/// </exception>
		public ISessionFactory GetSessionFactory(String alias)
		{
			var componentKey = alias2Key[alias] as String;

			if (componentKey == null)
			{
				throw new FacilityException("An ISessionFactory component was " +
											"not mapped for the specified alias: " + alias);
			}

			return kernel.Resolve<ISessionFactory>(componentKey);
		}
	}
}
