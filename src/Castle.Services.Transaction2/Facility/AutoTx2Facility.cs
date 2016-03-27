namespace Castle.Services.Transaction2.Facility
{
	using System;
	using System.Transactions;
	using Core.Logging;
	using Internal;
	using MicroKernel.Registration;
	using Transaction;

	public class AutoTx2Facility : Castle.MicroKernel.Facilities.AbstractFacility
	{
		private Type _activityManagerImpl = typeof(AsyncLocalActivityManager);

		public AutoTx2Facility WithActivityManager<T>() where T : IActivityManager2
		{
			_activityManagerImpl = typeof(T);
			return this;
		}

		protected override void Init()
		{
			ILogger logger = NullLogger.Instance;

			// check we have a logger factory
			if (Kernel.HasComponent(typeof(ILoggerFactory)))
			{
				// get logger factory
				var loggerFactory = Kernel.Resolve<ILoggerFactory>();
				// get logger
				logger = loggerFactory.Create(typeof(AutoTx2Facility));
			}

			if (logger.IsDebugEnabled)
				logger.Debug("initializing AutoTxFacility");

			Kernel.Register(
				// the interceptor needs to be created for every method call
				Component.For<TransactionInterceptor>()
					.LifeStyle.Transient,
				Component.For<ITransactionMetaInfoStore>()
					.ImplementedBy<TransactionClassMetaInfoStore>()
					.Named("transaction.metaInfoStore2")
					.LifeStyle.Singleton,
				Component.For<ITransactionManager2>()
					.ImplementedBy<TransactionManager2>()
					.Named("transaction.manager2")
					.LifeStyle.Singleton
					.Forward(typeof(TransactionManager2)),
				// the activity manager shouldn't have the same lifestyle as TransactionInterceptor, as it
				// calls a static .Net/Mono framework method, and it's the responsibility of
				// that framework method to keep track of the call context.
				Component.For<IActivityManager2>()
					.ImplementedBy(_activityManagerImpl)
					.LifeStyle.Singleton
				);

			Kernel.ComponentModelBuilder.AddContributor(new TransactionalComponentInspector());
		}
	}
}
