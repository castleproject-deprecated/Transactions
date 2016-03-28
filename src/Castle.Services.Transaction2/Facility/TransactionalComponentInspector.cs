namespace Castle.Services.Transaction.Facility
{
	using System.Linq;
	using Core;
	using MicroKernel;
	using MicroKernel.Facilities;
	using MicroKernel.ModelBuilder.Inspectors;

	/// <summary>
	/// 	Transaction component inspector that selects the methods
	/// 	available to get intercepted with transactions.
	/// </summary>
	internal class TransactionalComponentInspector : MethodMetaInspector
	{
		private ITransactionMetaInfoStore _metaStore;

		public override void ProcessModel(IKernel kernel, ComponentModel model)
		{
			if (_metaStore == null)
				_metaStore = kernel.Resolve<ITransactionMetaInfoStore>();

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
				throw new FacilityException(string.Format("The class {0} wants to use transaction interception, " +
													  "however the methods must be marked as virtual in order to do so. " +
													  "Please correct the following methods: {1}", model.Implementation.FullName,
													  string.Join(", ", problemMethod.Select(m => m.Name).ToArray())));
			}

			var meta = _metaStore.GetMetaFromType(model.Implementation);

			return meta != null;
		}

		private void AddInterceptor(ComponentModel model)
		{
			model.Dependencies.Add(new DependencyModel(ObtainNodeName(), typeof(TransactionInterceptor), isOptional: false));
			model.Interceptors.Add(new InterceptorReference(typeof(TransactionInterceptor)));
		}

		protected override string ObtainNodeName()
		{
			return "transaction-interceptor";
		}
	}
}
