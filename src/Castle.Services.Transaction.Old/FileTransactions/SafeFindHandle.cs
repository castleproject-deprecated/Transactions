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
	using System.Runtime.InteropServices;
	using System.Security.Permissions;
	using Microsoft.Win32.SafeHandles;

	[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
	internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		internal SafeFindHandle()
			: base(true)
		{
		}

		public SafeFindHandle(IntPtr preExistingHandle, bool ownsHandle)
			: base(ownsHandle)
		{
			SetHandle(preExistingHandle);
		}

		protected override bool ReleaseHandle()
		{
			if (!(IsInvalid || IsClosed))
			{
				return FindClose(this);
			}
			return (IsInvalid || IsClosed);
		}

		protected override void Dispose(bool disposing)
		{
			if (!(IsInvalid || IsClosed))
			{
				FindClose(this);
			}
			base.Dispose(disposing);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool FindClose(SafeHandle hFindFile);
	}
}