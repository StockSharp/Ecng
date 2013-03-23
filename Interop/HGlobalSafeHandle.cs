namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;

	using Microsoft.Win32.SafeHandles;

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
