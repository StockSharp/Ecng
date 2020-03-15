namespace Ecng.Interop
{
	using System;

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
			DangerousGetHandle().FreeHGlobal();
			return true;
		}
	}
}
