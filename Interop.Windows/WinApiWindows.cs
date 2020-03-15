namespace Ecng.Interop
{
	using System;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Windows.Forms;

	using Ecng.Common;

	using ManagedWinapi.Windows;

	///<summary>
	///</summary>
	[CLSCompliant(false)]
	public static class WinApiWindows
	{
		///<summary>
		///</summary>
		[StructLayout(LayoutKind.Sequential)]
		public struct MenuItemInfo
		{
			///<summary>
			///</summary>
			public uint cbSize;
			///<summary>
			///</summary>
			public WinApi.MenuItemInfoMasks fMask;
			///<summary>
			///</summary>
			public WinApi.MenuItemInfoTypes fType;
			///<summary>
			///</summary>
			public WinApi.MenuItemInfoStates fState;
			///<summary>
			///</summary>
			public int wID;
			///<summary>
			///</summary>
			public IntPtr hSubMenu;
			///<summary>
			///</summary>
			public IntPtr hBmpChecked;
			///<summary>
			///</summary>
			public IntPtr hBmpUnchecked;
			///<summary>
			///</summary>
			public int dwItemData;
			///<summary>
			///</summary>
			public string dwTypeData;
			///<summary>
			///</summary>
			public int cch;
			///<summary>
			///</summary>
			public IntPtr hBmpItem;
		}

		///<summary>
		///</summary>
		///<param name="wnd"></param>
		public static string GetText(this SystemWindow wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			var len = wnd.HWnd.SendMessage((int)WM.GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
			var retVal = new StringBuilder(len);
			wnd.HWnd.SendMessage((int)WM.GETTEXT, new IntPtr(len + 1), retVal);
			return retVal.ToString();
		}

		public static int SendMessage<TMessage, TWParam, TLParam>(this SystemWindow wnd, TMessage message, TWParam wParam, TLParam lParam)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			return wnd.HWnd.SendMessage(message.To<int>(), wParam.To<IntPtr>(), lParam.To<IntPtr>());
		}

		public static int PostMessage<TMessage, TWParam, TLParam>(this SystemWindow wnd, TMessage message, TWParam wParam, TLParam lParam)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			return wnd.HWnd.PostMessage(message.To<int>(), wParam.To<IntPtr>(), lParam.To<IntPtr>());
		}

		///<summary>
		///</summary>
		///<param name="wnd"></param>
		///<param name="text"></param>
		///<exception cref="ArgumentNullException"></exception>
		public static void SetText(this SystemWindow wnd, string text)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			wnd.HWnd.SendMessage((int)WM.SETTEXT, 0, text);
		}

		///<summary>
		///</summary>
		///<param name="wnd"></param>
		///<param name="elem"></param>
		///<exception cref="ArgumentNullException"></exception>
		public static void Command(this SystemWindow wnd, SystemWindow elem)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

			wnd.SendMessage(WM.COMMAND, elem.DialogID, 0);
		}

		public static void PressKeyButton(this SystemWindow window, VirtualKeys key)
		{
			window.SendMessage(WM.KEYDOWN, (int)key, 0);
			window.SendMessage(WM.KEYUP, (int)key, 0);
		}

		public static int GetProcessId(this SystemWindow wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			wnd.HWnd.GetWindowThreadProcessId(out int pid);
			return pid;
		}

		/// <summary>
		/// Searches for the topmost visible form of your app in all the forms opened in the current Windows session.
		/// </summary>
		/// <returns>The Form that is currently TopMost, or null</returns>
		public static Form GetTopMostWindow()
		{
			// http://social.msdn.microsoft.com/Forums/en/winforms/thread/99df9c07-c117-465a-9207-fa3534982021
			return GetTopMostWindow(Application.OpenForms[0].Handle);
		}

		/// <summary>
		/// Searches for the topmost visible form of your app in all the forms opened in the current Windows session.
		/// </summary>
		/// <param name="hWndMainFrm">Handle of the main form</param>
		/// <returns>The Form that is currently TopMost, or null</returns>
		public static Form GetTopMostWindow(IntPtr hWndMainFrm)
		{
			// http://stackoverflow.com/questions/1000847/how-to-get-the-handle-of-the-topmost-form-in-a-winform-app

			Form frm = null;

			var hwnd = WinApi.GetTopWindow(IntPtr.Zero);
			if (hwnd != IntPtr.Zero)
			{
				while ((!WinApi.IsWindowVisible(hwnd) || frm == null) && hwnd != hWndMainFrm)
				{
					// Get next window under the current handler
					hwnd = WinApi.GetNextWindow(hwnd, WinApi.GW_HWNDNEXT);

					try
					{
						frm = (Form)Control.FromHandle(hwnd);
					}
					catch
					{
						// Weird behaviour: In some cases, trying to cast to a Form a handle of an object 
						// that isn't a form will just return null. In other cases, will throw an exception.
					}
				}
			}

			return frm;
		}
	}
}