#region License
//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 
#endregion

namespace Castle.Services.Transaction
{
	using System;

	///<summary>
	/// A resource adapter for a file transaction.
	///</summary>
	public class FileResourceAdapter : IResource, IDisposable
	{
		private readonly IFileTransaction _Transaction;

		///<summary>
		/// c'tor
		///</summary>
		///<param name="transaction"></param>
		public FileResourceAdapter(IFileTransaction transaction)
		{
			_Transaction = transaction;
		}

		/// <summary>
		/// Gets the transaction this resouce adapter is an
		/// adapter for.
		/// </summary>
		public IFileTransaction Transaction
		{
			get { return _Transaction; }
		}

		/// <summary>
		/// Implementors should start the
		///             transaction on the underlying resource
		/// </summary>
		public void Start()
		{
			_Transaction.Begin();
		}

		/// <summary>
		/// Implementors should commit the
		///             transaction on the underlying resource
		/// </summary>
		public void Commit()
		{
			_Transaction.Commit();
		}

		/// <summary>
		/// Implementors should rollback the
		///             transaction on the underlying resource
		/// </summary>
		public void Rollback()
		{
			_Transaction.Rollback();
		}

		public void Dispose()
		{
			_Transaction.Dispose();
		}
	}
}