namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	using Microsoft.Win32;

	///<summary>
	///</summary>
	[CLSCompliant(false)]
	public static class WinApi
	{
		public static int ReleaseComObject(this object comObject)
			=> Marshal.ReleaseComObject(comObject);

		/// <summary>
		/// Wraps the specified COM object in an object of the specified type.
		/// </summary>
		/// <param name="target">The object to be wrapped.</param>
		/// <returns>The newly wrapped object.</returns>
		public static T Wrapper<T>(this object target)
			=> (T)Marshal.CreateWrapperOfType(target, typeof(T));

		public static void GetScreenParams(IntPtr hwnd, out int left, out int top, out int width, out int height)
		{
			var activeScreen = Screen.FromHandle(hwnd);
			var bounds = activeScreen.Bounds;

			left = bounds.Left;
			top = bounds.Top;
			width = bounds.Width;
			height = bounds.Height;
		}

		private const string _path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

		private static RegistryKey BaseKey => Registry.CurrentUser;

		public static void UpdateAutoRun(string appName, string path, bool enabled)
		{
			using var key = BaseKey.OpenSubKey(_path, true) ?? throw new InvalidOperationException($"autorun not found ({_path})");

			if (enabled)
				key.SetValue(appName, path);
			else
				key.DeleteValue(appName, false);
		}
	}
}