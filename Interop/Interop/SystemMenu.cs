namespace Ecng.Interop
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using ManagedWinapi.Windows;

	public class SystemMenuItem
	{
		private readonly SystemWindow _window;

		internal SystemMenuItem(WinApi.MenuItemInfo info, SystemWindow window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			_window = window;

			Info = info;

			Id = info.wID;
			IsEnabled = !info.fState.Contains(WinApi.MenuItemInfoStates.MFS_DISABLED);
			IsChecked = info.fState.Contains(WinApi.MenuItemInfoStates.MFS_CHECKED);

			if (info.fType.Contains(WinApi.MenuItemInfoTypes.MFT_STRING))
				Text = info.dwTypeData;

			Items = info.hSubMenu.GetMenuItems(window);
		}

		public IList<SystemMenuItem> Items { get; private set; }

		public bool IsEnabled { get; private set; }
		public bool IsChecked { get; private set; }
		public string Text { get; private set; }

		[CLSCompliant(false)]
		public WinApi.MenuItemInfo Info { get; private set; }
		public int Id { get; }

		public void Click()
		{
			_window.PostMessage(WM.COMMAND, Id, 0);
		}
	}

	public class SystemMenu
	{
		internal SystemMenu(IntPtr hMenu, SystemWindow window)
		{
			HMenu = hMenu;
			Items = hMenu.GetMenuItems(window);
		}

		public IList<SystemMenuItem> Items { get; private set; }
		public IntPtr HMenu { get; private set; }
	}
}