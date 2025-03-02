namespace Ecng.Interop
{
	using System;
	using System.Windows.Forms;

	using Microsoft.Win32;

	/// <summary>
	/// Provides Windows API utility methods.
	/// </summary>
	[CLSCompliant(false)]
	public static class WinApi
	{
		/// <summary>
		/// Retrieves the screen boundaries (left, top, width, height) for the screen that contains the specified window handle.
		/// </summary>
		/// <param name="hwnd">The handle of the window.</param>
		/// <param name="left">Output parameter that returns the left coordinate of the screen.</param>
		/// <param name="top">Output parameter that returns the top coordinate of the screen.</param>
		/// <param name="width">Output parameter that returns the width of the screen.</param>
		/// <param name="height">Output parameter that returns the height of the screen.</param>
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

		/// <summary>
		/// Updates the auto-run registry entry for a given application.
		/// </summary>
		/// <param name="appName">The name of the application.</param>
		/// <param name="path">The full path to the application executable.</param>
		/// <param name="enabled">True to enable auto-run; false to disable it.</param>
		/// <exception cref="InvalidOperationException">Thrown when the autorun registry key cannot be found.</exception>
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