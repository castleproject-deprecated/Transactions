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

using System;
using System.Security;
using Castle.Services.Transaction.Internal;
using Microsoft.Win32.SafeHandles;

namespace Castle.Services.Transaction.IO
{
	[SecurityCritical]
	public sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
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
				return NativeMethods.FindClose(this);
			}
			return (IsInvalid || IsClosed);
		}

		protected override void Dispose(bool disposing)
		{
			if (!(IsInvalid || IsClosed))
			{
				NativeMethods.FindClose(this);
			}
			base.Dispose(disposing);
		}
	}
}