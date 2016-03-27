namespace Castle.NHibIntegration
{
	using System.Configuration;
	using Core;
	using MicroKernel;
	using MicroKernel.ComponentActivator;
	using MicroKernel.Context;
	using NHibernate;

	/// <summary>
	/// Postpones the initiation of SessionFactory until Resolve
	/// </summary>
	public class SessionFactoryActivator : DefaultComponentActivator
	{
		/// <summary>
		/// Constructor for SessionFactoryActivator
		/// </summary>
		/// <param name="model"></param>
		/// <param name="kernel"></param>
		/// <param name="onCreation"></param>
		/// <param name="onDestruction"></param>
		public SessionFactoryActivator(ComponentModel model, IKernel kernel,
			ComponentInstanceDelegate onCreation, ComponentInstanceDelegate onDestruction)
			: base(model, kernel, onCreation, onDestruction)
		{
		}

		/// <summary>
		/// Calls the contributors
		/// </summary>
		protected virtual void RaiseCreatingSessionFactory()
		{
			var configuration = Model.ExtendedProperties[NhConstants.SessionFactoryConfiguration] as Configuration;
			var contributors = Kernel.ResolveAll<IConfigurationContributor>();
			foreach (var contributor in contributors)
			{
				contributor.Process(Model.Name, configuration);
			}
		}

		/// <summary>
		/// Creates the <see cref="ISessionFactory"/> from the configuration
		/// </summary>
		/// <param name="context"></param>
		/// <param name="burden"> </param>
		/// <returns></returns>
		public override object Create(CreationContext context, Burden burden)
		{
			RaiseCreatingSessionFactory();
			var configuration = Model.ExtendedProperties[NhConstants.SessionFactoryConfiguration]
				as NHibernate.Cfg.Configuration;

			var factory = configuration.BuildSessionFactory();

			burden.SetRootInstance(factory);

			return factory;
		}
	}
}