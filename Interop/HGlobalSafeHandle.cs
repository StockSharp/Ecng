namespace Ecng.Interop;

using System;

using Microsoft.Win32.SafeHandles;

/// <summary>
/// Represents a safe handle for unmanaged memory allocated with HGlobal.
/// </summary>
public class HGlobalSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	/// <summary>
	/// Initializes a new instance of the <see cref="HGlobalSafeHandle"/> class with the specified pointer.
	/// </summary>
	/// <param name="ptr">An <see cref="IntPtr"/> that represents the allocated unmanaged memory.</param>
	public HGlobalSafeHandle(IntPtr ptr)
		: base(true)
	{
		SetHandle(ptr);
	}

	/// <summary>
	/// Releases the unmanaged memory by freeing the HGlobal allocation.
	/// </summary>
	/// <returns>true if the handle is released successfully; otherwise, false.</returns>
	protected override bool ReleaseHandle()
	{
		DangerousGetHandle().FreeHGlobal();
		return true;
	}
}
