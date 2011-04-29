#region License
//  Copyright 2004-2010 Castle Project - http:www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http:www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 
#endregion
namespace Castle.Facilities.AutoTx
{
	using System;
	using System.Reflection;
	using Core;
	using Core.Interceptor;
	using Core.Logging;
	using DynamicProxy;
	using MicroKernel;
	using Services.Transaction;

	/// <summary>
	/// Intercepts call for transactional components, coordinating
	/// the transaction creation, commit/rollback accordingly to the 
	/// method execution. Rollback is invoked if an exception is threw.
	/// </summary>
	[Transient]
	public class TransactionInterceptor : IInterceptor, IOnBehalfAware
	{
		private readonly IKernel kernel;
		private readonly TransactionMetaInfoStore infoStore;
		private TransactionMetaInfo metaInfo;
		private ILogger logger = NullLogger.Instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionInterceptor"/> class.
		/// </summary>
		/// <param name="kernel">The kernel.</param>
		/// <param name="infoStore">The info store.</param>
		public TransactionInterceptor(IKernel kernel, TransactionMetaInfoStore infoStore)
		{
            this.kernel = kernel;
			this.infoStore = infoStore;
		}

		/// <summary>
		/// Gets or sets the logger.
		/// </summary>
		/// <value>The logger.</value>
		public ILogger Logger
		{
			get { return logger; }
			set { logger = value; }
		}

		#region IOnBehalfAware

		/// <summary>
		/// Sets the intercepted component's ComponentModel.
		/// </summary>
		/// <param name="target">The target's ComponentModel</param>
		public void SetInterceptedComponentModel(ComponentModel target)
		{
			metaInfo = infoStore.GetMetaFor(target.Implementation);
		}

		#endregion

		/// <summary>
		/// Intercepts the specified invocation and creates a transaction
		/// if necessary.
		/// </summary>
		/// <param name="invocation">The invocation.</param>
		/// <returns></returns>
		public void Intercept(IInvocation invocation)
		{
			MethodInfo methodInfo;
			if (invocation.Method.DeclaringType.IsInterface)
				methodInfo = invocation.MethodInvocationTarget;
			else
				methodInfo = invocation.Method;

			if (metaInfo == null || !metaInfo.Contains(methodInfo))
			{
				invocation.Proceed();
				return;
			}

			var attr = metaInfo.GetTransactionAttributeFor(methodInfo);
			var manager = kernel.Resolve<ITransactionManager>();
			var transaction = manager.CreateTransaction(attr.TransactionMode, attr.IsolationMode, attr.Distributed, attr.ReadOnly);

			if (transaction == null)
			{
				invocation.Proceed();
				return;
			}

			transaction.Begin();

			bool rolledback = false;

			try
			{
				if (metaInfo.ShouldInject(methodInfo))
				{
					var parameters = methodInfo.GetParameters();

					for (int i = 0; i < parameters.Length; i++)
					{
						if (parameters[i].ParameterType == typeof(ITransaction))
						{
							invocation.SetArgumentValue(i, transaction);
						}
					}
				}
				invocation.Proceed();

				if (transaction.IsRollbackOnlySet)
				{
					logger.DebugFormat("Rolling back transaction {0}", transaction.GetHashCode());

					rolledback = true;
					transaction.Rollback();
				}
				else
				{
					logger.DebugFormat("Committing transaction {0}", transaction.GetHashCode());

					transaction.Commit();
				}
			}
			catch(TransactionException ex)
			{
				// Whoops. Special case, let's throw without 
				// attempt to rollback anything

				if (logger.IsFatalEnabled)
				{
					logger.Fatal("Fatal error during transaction processing", ex);
				}

				throw;
			}
			catch(Exception)
			{
				if (!rolledback)
				{
					if (logger.IsDebugEnabled)
						logger.DebugFormat("Rolling back transaction {0} due to exception on method {2}.{1}", transaction.GetHashCode(), methodInfo.Name, methodInfo.DeclaringType.Name);

					transaction.Rollback();
				}

				throw;
			}
			finally
			{
				manager.Dispose(transaction);
			}
		}
	}
}
