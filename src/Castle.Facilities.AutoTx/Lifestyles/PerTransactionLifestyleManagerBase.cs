﻿#region license

// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Castle.Core;
using Castle.Core.Logging;
using Castle.MicroKernel;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Lifestyle;
using Castle.Services.Transaction;

namespace Castle.Facilities.AutoTx.Lifestyles
{
	/// <summary>
	/// 	This lifestyle manager is responsible for disposing components
	/// 	at the same time as the transaction is completed, i.e. the transction
	/// 	either Aborts, becomes InDoubt or Commits.
	/// </summary>
	[Serializable]
	public abstract class PerTransactionLifestyleManagerBase : AbstractLifestyleManager
	{
		private ILogger _Logger;

		private readonly Dictionary<string, Tuple<uint, object>> _Storage = new Dictionary<string, Tuple<uint, object>>();

		protected readonly ITransactionManager _Manager;
		protected bool _Disposed;
		private bool evicting;

		public PerTransactionLifestyleManagerBase(ITransactionManager manager)
		{
			Contract.Requires(manager != null);
			Contract.Ensures(_Manager != null);
			_Manager = manager;
		}

		public override void Init(IComponentActivator componentActivator, IKernel kernel, ComponentModel model)
		{
			base.Init(componentActivator, kernel, model);

			// check ILoggerFactory is registered (logging is enabled)
			if (kernel.HasComponent(typeof(ILoggerFactory))) 
			{
				// get logger factory instance
				ILoggerFactory loggerFactory = kernel.Resolve<ILoggerFactory>();
				// create logger
				_Logger = loggerFactory.Create(GetType());
			}
			else
				_Logger = NullLogger.Instance;
		}

		// this method is not thread-safe
		[SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly",
			Justification = "Can't 'seal' a member I'm overriding")]
		public override void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool managed)
		{
			Contract.Ensures(!managed || _Disposed);

			if (!managed)
				return;

			if (_Disposed)
			{
				if (_Logger.IsInfoEnabled) 
				{
					_Logger.Info(
						"repeated call to Dispose. will show stack-trace if logging is in debug mode as the next log line. this method call is idempotent");

					if (_Logger.IsDebugEnabled)
						_Logger.Debug(new StackTrace().ToString());
				}

				return;
			}

			try
			{
				lock (ComponentActivator)
				{
					if (_Logger.IsWarnEnabled && _Storage.Count > 0)
					{
						var items = string.Join(
							string.Format(", {0}", Environment.NewLine),
							_Storage
								.Select(x => string.Format("(id: {0}, item: {1})", x.Key, x.Value.ToString()))
								.ToArray());

						_Logger.WarnFormat("Storage contains {0} items! Items: {{ {1} }}",
						                   _Storage.Count,
						                   items);
					}

					// release all items
					foreach (var tuple in _Storage)
						Evict(tuple.Value.Item2);

					_Storage.Clear();
				}
			}
			finally
			{
				_Disposed = true;
			}
		}

		public override bool Release(object instance)
		{
			if (!evicting)
				return false;

			return base.Release(instance);
		}

		private void Evict(object instance)
		{
			using (new EvictionScope(this))
				Kernel.ReleaseComponent(instance);
		}

		public override object Resolve(CreationContext context)
		{
			Contract.Ensures(Contract.Result<object>() != null);

			if (_Logger.IsDebugEnabled)
				_Logger.DebugFormat("resolving service '{0}', which wants model '{1}' in a PerTransaction lifestyle", 
					context.Handler.Service, Model.Service);

			if (_Disposed)
				throw new ObjectDisposedException("PerTransactionLifestyleManagerBase",
				                                  "You cannot resolve with a disposed lifestyle.");

			if (!GetSemanticTransactionForLifetime().HasValue)
				throw new MissingTransactionException(
					string.Format("No transaction in context when trying to instantiate model '{0}' for resolve type '{1}'. "
						+ "If you have verified that your call stack contains a method with the [Transaction] attribute, "
						+ "then also make sure that you have registered the AutoTx Facility.", 
						Model.Service, context.Handler.Service));

			var transaction = GetSemanticTransactionForLifetime().Value;

			Contract.Assume(transaction.State != TransactionState.Disposed,
			                "because then it would not be active but would have been popped");

			Tuple<uint, object> instance;
			// unique key per the model service and per top transaction identifier
			var localIdentifier = transaction.LocalIdentifier;
			var key = localIdentifier + "|" + Model.Service.GetHashCode();

			if (!_Storage.TryGetValue(key, out instance))
			{
				lock (ComponentActivator)
				{
					if (_Logger.IsDebugEnabled)
						_Logger.DebugFormat("component for key '{0}' not found in per-tx storage. creating new instance.", key);

					if (!_Storage.TryGetValue(key, out instance))
					{
						instance = _Storage[key] = Tuple.Create(1u, base.Resolve(context));

						transaction.Inner.TransactionCompleted += (sender, args) =>
						{
							var id = localIdentifier;
							if (_Logger.IsDebugEnabled)
								_Logger.DebugFormat(
									"transaction#{0} completed, maybe releasing object '{1}'", id,
									instance);

							lock (ComponentActivator)
							{
								var counter = _Storage[key];

								if (counter.Item1 == 1)
								{
									if (_Logger.IsDebugEnabled)
										_Logger.DebugFormat("last item of '{0}' per-tx; releasing it",
															counter.Item2);

									// this might happen if the transaction outlives the service; the transaction might also notify transaction fron a timer, i.e.
									// not synchronously.
									if (!_Disposed)
									{
										Contract.Assume(_Storage.Count > 0);

										_Storage.Remove(key);
										Evict(counter.Item2);
									}
								}
								else
								{
									if (_Logger.IsDebugEnabled)
										_Logger.DebugFormat("{0} item(s) of '{1}' left in per-tx storage",
															counter.Item1 - 1, counter.Item2);

									_Storage[key] = Tuple.Create(counter.Item1 - 1, counter.Item2);
								}
							}
						};
					}
				}
			}

			Contract.Assume(instance.Item2 != null, "resolve throws otherwise");

			return instance.Item2;
		}

		private class EvictionScope : IDisposable
		{
			private readonly PerTransactionLifestyleManagerBase owner;

			public EvictionScope(PerTransactionLifestyleManagerBase owner)
			{
				this.owner = owner;
				this.owner.evicting = true;
			}

			public void Dispose()
			{
				owner.evicting = false;
			}
		}

		/// <summary>
		/// 	Gets the 'current' transaction; a semantic defined by the inheritors of this class.
		/// </summary>
		/// <returns>Maybe a current transaction as can be found in the transaction manager.</returns>
		protected internal abstract Maybe<ITransaction> GetSemanticTransactionForLifetime();
	}
}