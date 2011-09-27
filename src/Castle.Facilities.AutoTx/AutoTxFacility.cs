#region license

// Copyright 2004-2010 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using Castle.Facilities.AutoTx.Registration;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.Core.Logging;
using Castle.Services.Transaction;
using Castle.Services.Transaction.Activities;
using Castle.Services.Transaction.IO;

namespace Castle.Facilities.AutoTx
{
	///<summary>
	///	<para>A facility for automatically handling transactions using the lightweight
	///		transaction manager. This facility does not depend on
	///		any other facilities.</para>
	///	<para>
	///		Install the facility in your container with <code>c.AddFacility&lt;AutoTxFacility&gt;</code>
	///	</para>
	///</summary>
	public class AutoTxFacility : AbstractFacility
	{
		protected override void Init()
		{
			ILogger logger = NullLogger.Instance;
            
			// check we have a logger factory
			if (Kernel.HasComponent(typeof(ILoggerFactory)))
			{
				// get logger factory
				ILoggerFactory loggerFactory = Kernel.Resolve<ILoggerFactory>();
				// get logger
				logger = loggerFactory.Create(typeof(AutoTxFacility));
			}

			if(logger.IsDebugEnabled)
				logger.Debug("initializing AutoTxFacility");

			Kernel.Register(
				// the interceptor needs to be created for every method call
				Component.For<TransactionInterceptor>()
					.Named("transaction.interceptor")
					.LifeStyle.Transient,
				Component.For<ITransactionMetaInfoStore>()
					.ImplementedBy<TransactionClassMetaInfoStore>()
					.Named("transaction.metaInfoStore")
					.LifeStyle.Singleton,
				Component.For<ITransactionManager>()
					.ImplementedBy<TransactionManager>()
					.Named("transaction.manager")
					.LifeStyle.Singleton
					.Forward(typeof (TransactionManager)),
				// the activity manager shouldn't have the same lifestyle as TransactionInterceptor, as it
				// calls a static .Net/Mono framework method, and it's the responsibility of
				// that framework method to keep track of the call context.
				Component.For<IActivityManager>()
					.ImplementedBy<CallContextActivityManager>()
					.LifeStyle.Singleton,
				Component.For<IDirectoryAdapter>()
					.ImplementedBy<DirectoryAdapter>()
					.LifeStyle.PerTransaction(),
				Component.For<IFileAdapter>()
					.ImplementedBy<FileAdapter>()
					.LifeStyle.PerTransaction(),
				Component.For<IMapPath>()
					.ImplementedBy<MapPathImpl>()
					.LifeStyle.Transient
				);

			// TODO: Inspect already existing components!
			Kernel.ComponentModelBuilder.AddContributor(new TransactionalComponentInspector());

			if (logger.IsDebugEnabled)
				logger.Debug(
					@"Initialized AutoTxFacility:

If you are experiencing problems, go to https://github.com/haf/ and file a ticket for the Transactions project.
You can enable verbose logging for .Net by adding this to you .config file:

	<system.diagnostics>
		<sources>
			<source name=""System.Transactions"" switchValue=""Information"">
				<listeners>
					<add name=""tx"" type=""Castle.Services.Transaction.Internal.TxTraceListener, Castle.Services.Transaction""/>
				</listeners>
			</source>
		</sources>
	</system.diagnostics>

If you wish to e.g. roll back a transaction from within a transactional method you can resolve/use the ITransactionManager's
CurrentTransaction property and invoke Rollback on it. Be ready to catch TransactionAbortedException from the caller.
");
		}
	}
}