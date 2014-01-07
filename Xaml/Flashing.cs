﻿namespace Ecng.Xaml
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows;
	using System.Windows.Interop;

	// http://www.jarloo.com/flashing-a-wpf-window/
	public static class Flashing
	{
		#region Window Flashing API Stuff

		private const uint FLASHW_STOP = 0; //Stop flashing. The system restores the window to its original state.
		private const uint FLASHW_CAPTION = 1; //Flash the window caption.
		private const uint FLASHW_TRAY = 2; //Flash the taskbar button.
		private const uint FLASHW_ALL = 3; //Flash both the window caption and taskbar button.
		private const uint FLASHW_TIMER = 4; //Flash continuously, until the FLASHW_STOP flag is set.
		private const uint FLASHW_TIMERNOFG = 12; //Flash continuously until the window comes to the foreground.

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO
		{
			public uint cbSize; //The size of the structure in bytes.
			public IntPtr hwnd; //A Handle to the Window to be Flashed. The window can be either opened or minimized.
			public uint dwFlags; //The Flash Status.
			public uint uCount; // number of times to flash the window
			public uint dwTimeout; //The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		#endregion

		public static void StartFlashing(this Window win, int count = int.MaxValue)
		{
			if (win == null)
				throw new ArgumentNullException("win");

			//Don't flash if the window is active
			if (win.IsActive)
				return;

			var h = new WindowInteropHelper(win);

			var info = new FLASHWINFO
			{
				hwnd = h.Handle,
				dwFlags = FLASHW_ALL | FLASHW_TIMER,
				uCount = (uint)count,
				dwTimeout = 0
			};

			info.cbSize = (uint)Marshal.SizeOf(info);
			FlashWindowEx(ref info);
		}

		public static void StopFlashing(this Window win)
		{
			var h = new WindowInteropHelper(win);

			var info = new FLASHWINFO
			{
				hwnd = h.Handle,
				dwFlags = FLASHW_STOP,
				uCount = uint.MaxValue,
				dwTimeout = 0
			};
			info.cbSize = (uint)Marshal.SizeOf(info);

			FlashWindowEx(ref info);
		}
	}
}