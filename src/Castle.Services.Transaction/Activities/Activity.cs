﻿#region license

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

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using log4net;

namespace Castle.Services.Transaction.Activities
{
	/// <summary>
	/// 	Value-object that encapsulates a transaction and is serializable across
	/// 	app-domains.
	/// </summary>
	[Serializable]
	public sealed class Activity : MarshalByRefObject, IEquatable<Activity>
	{
		private static readonly ILog _Logger = LogManager.GetLogger(typeof (Activity));

		private readonly Guid _ActivityId = Guid.NewGuid();
		private readonly Stack<Tuple<ITransaction, string>> _Txs = new Stack<Tuple<ITransaction, string>>();
		private ITransaction _TopMost;

		public Activity()
		{
			Contract.Ensures(Count == 0);
		}

		public Maybe<ITransaction> TopTransaction
		{
			get
			{
				Contract.Ensures(Contract.Result<Maybe<ITransaction>>() != null);
				return _TopMost != null ? Maybe.Some(_TopMost) : Maybe.None<ITransaction>();
			}
		}

		public Maybe<ITransaction> CurrentTransaction
		{
			get
			{
				Contract.Ensures(Contract.Result<Maybe<ITransaction>>() != null);
				return _Txs.Count == 0 ? Maybe.None<ITransaction>() : Maybe.Some(_Txs.Peek().Item1);
			}
		}

		[ContractInvariantMethod]
		private void Invariant()
		{
			Contract.Invariant(_Txs != null);
			Contract.Invariant(_Txs.Count >= 0);
			Contract.Invariant(Count >= 0);
			Contract.Invariant(Count != 0 || _TopMost == null);
			Contract.Invariant(Count == 0 || _TopMost != null);
		}

		/// <summary>
		/// 	Push a transaction onto the stack of transactions.
		/// </summary>
		/// <param name = "transaction"></param>
		public void Push(ITransaction transaction)
		{
			Contract.Requires(transaction != null);
			Contract.Requires(transaction.State != TransactionState.Disposed);
			Contract.Ensures(Contract.OldValue(Count) + 1 == Count);
			Contract.Ensures(_TopMost != null);
			Contract.Ensures(Contract.OldValue(Count) != 0 || _TopMost == transaction);

			// I can't prove this because Push doesn't have those contracts
			//Contract.Ensures(Contract.Exists(_Txs, x => object.ReferenceEquals(x, transaction)));

			// I can't prove this because I can't reason about value/reference equality using reflection in Maybe
			//Contract.Ensures(object.ReferenceEquals(CurrentTransaction.Value, transaction));

			_Logger.DebugFormat("pushing tx#{0}", transaction.LocalIdentifier);

			if (Count == 0)
				_TopMost = transaction;

			_Txs.Push(Tuple.Create(transaction, transaction.LocalIdentifier));
		}

		/// <summary>
		/// 	Return the top-most transaction from the stack of transactions.
		/// </summary>
		/// <returns></returns>
		public ITransaction Pop()
		{
			Contract.Requires(Count > 0);
			// I can't prove this because Push doesn't have those contracts
			// Contract.Ensures(Contract.ForAll(_Txs, x => !object.ReferenceEquals(x, Contract.Result<ITransaction>())));
			Contract.Ensures(Contract.OldValue(Count) - 1 == Count);
			Contract.Ensures(Contract.Result<ITransaction>() != null);

			var ret = _Txs.Pop();

			_Logger.DebugFormat("popping tx#{0}", ret.Item2);

			if (Count == 0)
				_TopMost = null;

			Contract.Assume(ret.Item1 != null, "tuples are immutable");

			return ret.Item1;
		}

		public uint Count
		{
			get { return (uint) _Txs.Count; }
		}

		[Pure]
		public bool Equals(Activity other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return other.GetHashCode().Equals(GetHashCode());
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != typeof (Activity)) return false;
			return Equals((Activity) obj);
		}

		public override int GetHashCode()
		{
			return _ActivityId.GetHashCode();
		}

		public static bool operator ==(Activity left, Activity right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(Activity left, Activity right)
		{
			return !Equals(left, right);
		}
	}
}