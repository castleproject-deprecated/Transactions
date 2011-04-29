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
	/// <summary>
	/// Depicts a synchronization contract.
	/// <para>
	/// Code can be executed before and after the 
	/// transaction completes
	/// </para>
	/// </summary>
	public interface ISynchronization
	{
		/// <summary>
		/// Implementors may have code executing
		/// just before the transaction completes or rolls back.
		/// There be dragons: if a resource fails BeforeCompletion
		/// could be called twice, as it's first called before commit
		/// and then if the transaction has made changes and needs to 
		/// be rolled back because of one of its resources failing,
		/// then again, this method will be called.
		/// </summary>
		void BeforeCompletion();

		/// <summary>
		/// Implementors may have code executing
		/// just after the transaction completes
		/// </summary>
		void AfterCompletion();
	}
}
