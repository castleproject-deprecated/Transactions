namespace Castle.NHibIntegration.AutoClosing
{
	using System.Linq;
	using Core;
	using MicroKernel;
	using MicroKernel.Facilities;
	using MicroKernel.ModelBuilder.Inspectors;

	public class SessionAttributeComponentInspector : MethodMetaInspector
	{
		private INhMetaInfoStore _metaStore;

		public override void ProcessModel(IKernel kernel, ComponentModel model)
		{
			if (_metaStore == null)
			{
				_metaStore = kernel.Resolve<INhMetaInfoStore>();
			}

			if (PrepareAndValidate(model))
			{
				AddInterceptor(model);
			}
		}

		private bool PrepareAndValidate(ComponentModel model)
		{
			var problemMethod = _metaStore.SanityCheck(model.Implementation);

			if (problemMethod.Any())
			{
				throw new FacilityException(string.Format("The class {0} wants to use auto close interception, " +
				                                          "however the methods must be marked as virtual in order to do so. " +
				                                          "Please correct the following methods: {1}", model.Implementation.FullName,
					string.Join(", ", problemMethod.Select(m => m.Name).ToArray())));
			}

			var meta = _metaStore.GetMetaFromType(model.Implementation);

			return meta != null;
		}

		private void AddInterceptor(ComponentModel model)
		{
			model.Dependencies.Add(new DependencyModel(ObtainNodeName(), typeof(AutoSessionCloseInterceptor), isOptional: false));
			model.Interceptors.Add(new InterceptorReference(typeof(AutoSessionCloseInterceptor)));
		}

		protected override string ObtainNodeName()
		{
			return "autoclose-interceptor";
		}
	}
}