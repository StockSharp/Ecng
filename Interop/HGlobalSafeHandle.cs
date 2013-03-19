using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Ecng.Interop
{
	public class HGlobalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
	{
		public HGlobalSafeHandle(IntPtr ptr)
			: base(true)
		{
			SetHandle(ptr);
		}

		protected override bool ReleaseHandle()
		{
			Marshal.FreeHGlobal(DangerousGetHandle());
			return true;
		}
	}
}
