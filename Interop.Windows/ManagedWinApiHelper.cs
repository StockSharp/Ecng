namespace Ecng.Interop
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.InteropServices;
	using System.Runtime.ExceptionServices;
	using System.Text;

	using Microsoft.Win32;

	using Ecng.Common;
	using Ecng.Reflection;

	using ManagedWinapi.Windows;
	using ManagedWinapi.Windows.Contents;

	[CLSCompliant(false)]
	public static class ManagedWinApiHelper
	{
		public static SystemMenu GetMenu(this SystemWindow window)
		{
			if (window is null)
				throw new ArgumentNullException(nameof(window));

			var ptr = window.HWnd.GetMenu();

			return ptr != IntPtr.Zero ? new SystemMenu(ptr, window) : null;
		}

		public static IEnumerable<string> GetListBoxItems(this SystemListBox lb)
		{
			if (lb is null)
				throw new ArgumentNullException(nameof(lb));

			var items = new List<string>();

			for (var i = 0; i < lb.Count; i++)
				items.Add(lb[i]);

			return items;
		}

		public static IEnumerable<string> GetListContentItems(this ListContent content)
		{
			if (content is null)
				throw new ArgumentNullException(nameof(content));

			var items = new List<string>();

			for (var i = 0; i < content.Count; i++)
				items.Add(content[i]);

			return items;
		}

		//http://msdn.microsoft.com/en-us/magazine/dd419661.aspx
		[HandleProcessCorruptedStateExceptions]
		internal static IList<SystemMenuItem> GetMenuItems(this IntPtr hMenu, SystemWindow window)
		{
			if (window is null)
				throw new ArgumentNullException(nameof(window));

			var items = new List<SystemMenuItem>();

			if (hMenu != IntPtr.Zero)
			{
				var count = hMenu.GetMenuItemCount();

				if (count == -1)
					throw new Win32Exception();

				for (var i = 0; i < count; i++)
				{
					var info = new WinApi.MenuItemInfo();
					info.cbSize = (uint)Marshal.SizeOf(info);
					info.fMask =
						WinApi.MenuItemInfoMasks.MIIM_TYPE |
						WinApi.MenuItemInfoMasks.MIIM_ID |
						WinApi.MenuItemInfoMasks.MIIM_STATE |
						WinApi.MenuItemInfoMasks.MIIM_SUBMENU;

					try
					{
						if (hMenu.GetMenuItemInfo((uint)i, true, ref info))
						{
							var text = new StringBuilder(info.cch + 1);
							hMenu.GetMenuString(i, text, text.Capacity, WinApi.MF_BYPOSITION);
							info.dwTypeData = text.ToString();
							items.Add(new SystemMenuItem(info, window));
						}
					}
					catch (AccessViolationException) // MDI
					{
						continue;
					}
				}
			}

			return items;
		}

		public static void SelectListBoxItem(this SystemListBox lb, int index)
		{
			if (lb is null)
				throw new ArgumentNullException(nameof(lb));

			var lbWnd = lb.SystemWindow;

			lbWnd.SendMessage(Messages.LB_SETCURSEL, index, 0);
			lbWnd.Parent.SendMessage(WM.COMMAND, WinApi.MakeParam(lbWnd.DialogID, WinApi.LBN_SELCHANGE), lbWnd.HWnd);
		}

		public static void SelectMultiListBoxItem(this SystemListBox lb, int index, bool value)
		{
			if (lb is null)
				throw new ArgumentNullException(nameof(lb));

			var lbWnd = lb.SystemWindow;

			lbWnd.SendMessage(Messages.LB_SETSEL, value ? 1 : 0, index);
			lbWnd.Parent.SendMessage(WM.COMMAND, WinApi.MakeParam(lbWnd.DialogID, WinApi.LBN_SELCHANGE), lbWnd.HWnd);
		}

		public static SystemComboBox ToComboBox(this SystemWindow window)
		{
			return window.Cast<SystemComboBox>("ComboBox");
		}

		public static SystemListView ToListView(this SystemWindow window)
		{
			return window.Cast<SystemListView>("ListView", "SysListView32");
		}

		public static SystemTreeView ToTreeView(this SystemWindow window)
		{
			return window.Cast<SystemTreeView>("TreeView");
		}

		public static SystemListBox ToListBox(this SystemWindow window)
		{
			return window.Cast<SystemListBox>("ListBox");
		}

		private static T Cast<T>(this SystemWindow window, params string[] classNames)
		{
			if (window is null)
				throw new ArgumentNullException(nameof(window));

			if (!classNames.Contains(window.ClassName))
				throw new ArgumentException($"Window has invalid class name '{window.ClassName}'.", nameof(window));

			return ReflectionHelper.CreateInstance<SystemWindow, T>(window);
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