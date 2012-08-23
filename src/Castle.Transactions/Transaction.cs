﻿#region license

// Copyright 2004-2012 Castle Project, Henrik Feldt &contributors - https://github.com/castleproject
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
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using System.Transactions;
using Castle.Core.Logging;
using Castle.Transactions.Internal;

namespace Castle.Transactions
{
	using Castle.Transactions.Helpers;

	[Serializable]
	public class Transaction : ITransaction, IDependentAware
	{
		readonly ILogger _Logger = NullLogger.Instance;

		private TransactionState _State = TransactionState.Default;

		private readonly ITransactionOptions _CreationOptions;
		private readonly CommittableTransaction _Committable;
		private readonly DependentTransaction _Dependent;
		private List<Task> _DependentTasks;
		private readonly string _LocalIdentifier;

		[NonSerialized] private readonly Action _OnDispose;

		public Transaction(CommittableTransaction committable, uint stackDepth, ITransactionOptions creationOptions,
		                   Action onDispose, ILogger logger)
		{
			Contract.Requires(logger != null);
			Contract.Requires(creationOptions != null);
			Contract.Requires(committable != null);
			Contract.Ensures(_State == TransactionState.Active);
			Contract.Ensures(((ITransaction)this).State == TransactionState.Active);

			_Committable = committable;
			_CreationOptions = creationOptions;
			_OnDispose = onDispose;
			_State = TransactionState.Active;
			_LocalIdentifier = committable.TransactionInformation.LocalIdentifier + ":" + stackDepth;
			_Logger = logger;
		}

		public Transaction(DependentTransaction dependent, uint stackDepth, ITransactionOptions creationOptions, Action onDispose, ILogger logger)
		{
			Contract.Requires(logger != null);
			Contract.Requires(creationOptions != null);
			Contract.Requires(dependent != null);
			Contract.Ensures(_State == TransactionState.Active);
			Contract.Ensures(((ITransaction)this).State == TransactionState.Active);

			_Dependent = dependent;
			_CreationOptions = creationOptions;
			_OnDispose = onDispose;
			_State = TransactionState.Active;
			_LocalIdentifier = dependent.TransactionInformation.LocalIdentifier + ":" + stackDepth;
			_Logger = logger;
		}

		[ContractInvariantMethod]
		private void Invariant()
		{
			Contract.Invariant(_Dependent != null || _Committable != null); // mutual exclusion (A->B) ^ (B->A)
			Contract.Invariant(_Committable != null || _Dependent != null);
			Contract.Invariant(_DependentTasks == null || Contract.ForAll(_DependentTasks, t => t!=null));
			Contract.Invariant(!string.IsNullOrEmpty(_LocalIdentifier));
		}

		/**
		 * Possible state changes
		 * 
		 * Default -> Constructed
		 * Constructed -> Disposed
		 * Constructed -> Active
		 * Active -> CommittedOrCompleted (depends on whether we are committable or not)
		 * Active -> InDoubt
		 * Active -> Aborted
		 * Aborted -> Disposed	# an active transaction may be disposed and then dispose must take take of aborting
		 */

		void ITransaction.Dispose()
		{
			try
			{
				try
				{
					if (_State == TransactionState.Active)
						InnerRollback();
				}
				finally
				{
					// the question is; does committable transaction object to being disposed on exceptions?
					((IDisposable) this).Dispose(); 
				}
			}
			finally
			{
				_State = TransactionState.Disposed;
			}
		}

		TransactionState ITransaction.State
		{
			get
			{
				Contract.Ensures(Contract.Result<TransactionState>() == _State);
				return _State;
			}
		}

		ITransactionOptions ITransaction.CreationOptions
		{
			get { return _CreationOptions; }
		}

		System.Transactions.Transaction ITransaction.Inner
		{
			get
			{
				Contract.Assume(_Committable != null || _Dependent != null);
				return _Committable ?? (System.Transactions.Transaction) _Dependent;
			}
		}

		private System.Transactions.Transaction Inner
		{
			get { return _Committable ?? (System.Transactions.Transaction) _Dependent; }
		}

		//Maybe<IRetryPolicy> ITransaction.FailedPolicy
		//{
		//    get { return Maybe.None<IRetryPolicy>(); }
		//}

		string ITransaction.LocalIdentifier
		{
			get { return _LocalIdentifier; }
		}

		void ITransaction.Rollback()
		{
			InnerRollback();
		}

		private void InnerRollback()
		{
			Contract.Ensures(_State == TransactionState.Aborted);

			try
			{
				_Logger.InfoFormat("rolling back tx#{0}", _LocalIdentifier);
				Inner.Rollback();
			}
			finally
			{
				_State = TransactionState.Aborted;
			}
		}

		internal Action beforeTopComplete;
		void ITransaction.Complete()
		{
			try
			{
				if (_Committable != null)
				{
					_Logger.DebugFormat("committing committable tx#{0}", _LocalIdentifier);

					if (beforeTopComplete != null) 
						beforeTopComplete();

					if (_DependentTasks != null && _CreationOptions.DependentOption == DependentCloneOption.BlockCommitUntilComplete)
						Task.WaitAll(_DependentTasks.ToArray()); // this might throw, and then we don't set the state to completed
					else if (_DependentTasks != null && _CreationOptions.DependentOption == DependentCloneOption.RollbackIfNotComplete)
						_DependentTasks.Do(x => x.IgnoreExceptions()).Run();

					_Committable.Commit();
				}
				else
				{
					_Logger.DebugFormat("completing dependent tx#{0}", _LocalIdentifier);

					_Dependent.Complete();
				}

				_State = TransactionState.CommittedOrCompleted;
			}
			catch (TransactionInDoubtException e)
			{
				_State = TransactionState.InDoubt;
				throw new TransactionException("Transaction in doubt. See inner exception and help link for details",
				                               e, new Uri("http://support.microsoft.com/kb/899115/EN-US/"));
			}
			catch (TimeoutException e)
			{
				_State = TransactionState.Aborted;
				_Logger.Warn("transaction timed out", e);
			}
			catch (TransactionAbortedException e)
			{
				_State = TransactionState.Aborted;
				_Logger.Warn("transaction aborted", e);
				throw;
			}
			catch (AggregateException e)
			{
				_State = TransactionState.Aborted;
				_Logger.Warn("dependent transactions failed, so we are not performing the rollback (as they will have notified their parent!)", e);
				throw;
			}
			catch (Exception e)
			{
				InnerRollback();

				// we might have lost connection to the database
				// or any other myriad of problems might have occurred
				throw new TransactionException("Unknown Transaction Problem. Read the Inner Exception", e,
				                               new Uri("http://stackoverflow.com/questions/224689/transactions-in-net"));
			}
		}

		void IDependentAware.RegisterDependent(Task task)
		{
			Contract.Ensures(_DependentTasks != null);
			Contract.Ensures(Contract.ForAll(_DependentTasks, t => t != null));

			if (_DependentTasks == null)
				// the number of processor cores * 2 is a reasonable assumption if people are using the Fork=true option.
				_DependentTasks = new List<Task>(Environment.ProcessorCount * 2);
			
			_DependentTasks.Add(task);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool isManaged)
		{
			if (!isManaged)
				return;

			_Logger.Debug("disposing");

			if (_DependentTasks != null) 
				_DependentTasks.Clear();

			try
			{
				if (_OnDispose != null) 
					_OnDispose();

				Inner.Dispose();
			}
			finally
			{
				_State = TransactionState.Disposed;
			}
		}
	}
}