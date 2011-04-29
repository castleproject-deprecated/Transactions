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

using Castle.Facilities.AutoTx.Lifestyles;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.Services.Transaction;
using Castle.Services.Transaction.Activities;
using Castle.Services.Transaction.IO;
using log4net;

namespace Castle.Facilities.AutoTx
{
	/// <summary>
	/// 	A facility for automatically handling transactions using the lightweight
	/// 	transaction manager. This facility does not depend on
	///		any other facilities.
	/// </summary>
	public class AutoTxFacility : AbstractFacility
	{
		private static readonly ILog _Logger = LogManager.GetLogger(typeof (AutoTxFacility));

		protected override void Init()
		{
			_Logger.Debug("initializing AutoTxFacility");

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
					.LifeStyle.Singleton,
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

			Kernel.ComponentModelBuilder.AddContributor(new TransactionalComponentInspector());

			_Logger.Debug("initialized AutoTxFacility");
		}
	}
}