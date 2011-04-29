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
	using System.Runtime.InteropServices;

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("79427A2B-F895-40e0-BE79-B57DC82ED231")]
	internal interface IKernelTransaction
	{
		/// <summary>
		/// Gets a safe transaction handle. If we instead use IntPtr we 
		/// might not release the transaction handle properly.
		/// </summary>
		/// <param name="handle"></param>
		void GetHandle([Out] out SafeTxHandle handle);
	}
}