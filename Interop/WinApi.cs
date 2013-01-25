namespace Ecng.Interop
{
	using System;
	using System.Diagnostics;
	using System.Management;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Windows.Forms;

	using Ecng.Common;

	using ManagedWinapi.Windows;

	///<summary>
	///</summary>
	[CLSCompliant(false)]
	public static class WinApi
	{
		// ReSharper disable InconsistentNaming
		[Flags]
		public enum MenuItemInfoMasks : uint
		{
			MIIM_BITMAP = 0x00000080,
			MIIM_CHECKMARKS = 0x00000008,
			MIIM_DATA = 0x00000020,
			MIIM_FTYPE = 0x00000100,
			MIIM_ID = 0x00000002,
			MIIM_STATE = 0x00000001,
			MIIM_STRING = 0x00000040,
			MIIM_SUBMENU = 0x00000004,
			MIIM_TYPE = 0x00000010,
		}

		[Flags]
		public enum MenuItemInfoTypes : uint
		{
			MFT_STRING = 0x00000000,
			MFT_BITMAP = 0x00000004,
			MFT_MENUBARBREAK = 0x00000020,
			MFT_MENUBREAK = 0x00000040,
			MFT_OWNERDRAW = 0x00000100,
			MFT_RADIOCHECK = 0x00000200,
			MFT_SEPARATOR = 0x00000800,
			MFT_RIGHTORDER = 0x00002000,
			MFT_RIGHTJUSTIFY = 0x00004000,
		}

		[Flags]
		public enum MenuItemInfoStates : uint
		{
			MFS_GRAYED = 0x00000003,
			MFS_DISABLED = MFS_GRAYED,
			MFS_CHECKED = 0x00000008,
			MFS_HILITE = 0x00000080,
			MFS_ENABLED = 0x00000000,
			MFS_UNCHECKED = 0x00000000,
			MFS_UNHILITE = 0x00000000,
			MFS_DEFAULT = 0x00001000,
		}

		/// <summary>
		/// HT is just a placeholder for HT (HitTest) definitions
		/// </summary>
		public class HT
		{
			public const int HTERROR = (-2);
			public const int HTTRANSPARENT = (-1);
			public const int HTNOWHERE = 0;
			public const int HTCLIENT = 1;
			public const int HTCAPTION = 2;
			public const int HTSYSMENU = 3;
			public const int HTGROWBOX = 4;
			public const int HTSIZE = HTGROWBOX;
			public const int HTMENU = 5;
			public const int HTHSCROLL = 6;
			public const int HTVSCROLL = 7;
			public const int HTMINBUTTON = 8;
			public const int HTMAXBUTTON = 9;
			public const int HTLEFT = 10;
			public const int HTRIGHT = 11;
			public const int HTTOP = 12;
			public const int HTTOPLEFT = 13;
			public const int HTTOPRIGHT = 14;
			public const int HTBOTTOM = 15;
			public const int HTBOTTOMLEFT = 16;
			public const int HTBOTTOMRIGHT = 17;
			public const int HTBORDER = 18;
			public const int HTREDUCE = HTMINBUTTON;
			public const int HTZOOM = HTMAXBUTTON;
			public const int HTSIZEFIRST = HTLEFT;
			public const int HTSIZELAST = HTBOTTOMRIGHT;

			public const int HTOBJECT = 19;
			public const int HTCLOSE = 20;
			public const int HTHELP = 21;
		}

		/// <summary>
		/// VK is just a placeholder for VK (VirtualKey) general definitions
		/// </summary>
		public class VK
		{
			public const int VK_SHIFT = 0x10;
			public const int VK_CONTROL = 0x11;
			public const int VK_MENU = 0x12;
			public const int VK_ESCAPE = 0x1B;

			public static bool IsKeyPressed(int KeyCode)
			{
				return (GetAsyncKeyState(KeyCode) & 0x0800) == 0;
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern short GetAsyncKeyState(int vKey);

		[DllImport("user32.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr GetDesktopWindow();

		public const int MF_BYCOMMAND = 0x00000000;
		public const int MF_BYPOSITION = 0x00000400;

		public const int LBN_SELCHANGE = 1;

		// ReSharper restore InconsistentNaming

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
			public MenuItemInfoMasks fMask;
			///<summary>
			///</summary>
			public MenuItemInfoTypes fType;
			///<summary>
			///</summary>
			public MenuItemInfoStates fState;
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
		///<param name="hWnd"></param>
		///<param name="wMsg"></param>
		///<param name="wParam"></param>
		///<param name="lParam"></param>
		///<returns></returns>
		[DllImport("user32.dll")]
		public static extern int SendMessage(this IntPtr hWnd, int wMsg, int wParam, [MarshalAs(UnmanagedType.LPStr)]string lParam);

		///<summary>
		///</summary>
		///<param name="hWnd"></param>
		///<returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetMenu(this IntPtr hWnd);

		///<summary>
		///</summary>
		///<param name="hMenu"></param>
		///<param name="nPos"></param>
		///<returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint GetMenuItemID(this IntPtr hMenu, int nPos);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetMenuItemCount(this IntPtr hMenu);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetMenuString(this IntPtr hMenu, int itemNo, [Out, MarshalAs(UnmanagedType.LPStr)]StringBuilder text, int length, int flags);

		///<summary>
		///</summary>
		///<param name="hMenu"></param>
		///<param name="uItem"></param>
		///<param name="fByPosition"></param>
		///<param name="lpmii"></param>
		///<returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool GetMenuItemInfo(this IntPtr hMenu, uint uItem, bool fByPosition, ref MenuItemInfo lpmii);

		///<summary>
		///</summary>
		///<param name="hMenu"></param>
		///<param name="nPos"></param>
		///<returns></returns>
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetSubMenu(this IntPtr hMenu, int nPos);

		///<summary>
		///</summary>
		///<param name="hWnd"></param>
		///<param name="msg"></param>
		///<param name="wParam"></param>
		///<param name="lParam"></param>
		///<param name="lpCallback"></param>
		///<param name="dwData"></param>
		///<returns></returns>
		[DllImport("user32.dll")]
		public static extern bool SendMessageCallback(this IntPtr hWnd, int msg, int wParam, int lParam, SendAsyncProc lpCallback, UIntPtr dwData);

		///<summary>
		///</summary>
		///<param name="hwnd"></param>
		///<param name="uMsg"></param>
		///<param name="dwData"></param>
		///<param name="lResult"></param>
		public delegate void SendAsyncProc(IntPtr hwnd, uint uMsg, UIntPtr dwData, IntPtr lResult);

		///<summary>
		///</summary>
		///<param name="hwnd"></param>
		///<param name="uMsg"></param>
		///<param name="dwData"></param>
		///<param name="lResult"></param>
		public static void SendMessageCallback(IntPtr hwnd, uint uMsg, UIntPtr dwData, IntPtr lResult)
		{
		}

		///<summary>
		///</summary>
		///<param name="wnd"></param>
		public static string GetText(this SystemWindow wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			var len = SendMessage(wnd.HWnd, (int)WM.GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero);
			var retVal = new StringBuilder(len);
			SendMessage(wnd.HWnd, (int)WM.GETTEXT, new IntPtr(len + 1), retVal);
			return retVal.ToString();
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int SendMessage(this IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int PostMessage(this IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

		//[DllImport("user32.dll", SetLastError = true)]
		//public static extern int SendMessage(this IntPtr hWnd, WM wMsg, int wParam, int lParam);

		//[DllImport("user32.dll", SetLastError = true)]
		//public static extern int SendMessage(this IntPtr hWnd, Messages wMsg, int wParam, int lParam);

		//[DllImport("user32.dll", SetLastError = true)]
		//public static extern int PostMessage(this IntPtr hWnd, WM wMsg, int wParam, int lParam);

		//[DllImport("user32.dll", SetLastError = true)]
		//public static extern int PostMessage(this IntPtr hWnd, Messages wMsg, int wParam, int lParam);

		public static int SendMessage<TMessage, TWParam, TLParam>(this SystemWindow wnd, TMessage message, TWParam wParam, TLParam lParam)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			return wnd.HWnd.SendMessage(message.To<int>(), wParam.To<IntPtr>(), lParam.To<IntPtr>());
		}

		public static int PostMessage<TMessage, TWParam, TLParam>(this SystemWindow wnd, TMessage message, TWParam wParam, TLParam lParam)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			return wnd.HWnd.PostMessage(message.To<int>(), wParam.To<IntPtr>(), lParam.To<IntPtr>());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="wMsg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern int SendNotifyMessage(this IntPtr hWnd, uint wMsg, UIntPtr wParam, IntPtr lParam);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="threadId"></param>
		/// <param name="wMsg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern bool PostThreadMessage(uint threadId, uint wMsg, UIntPtr wParam, IntPtr lParam);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="wMsg"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		public static extern int SendMessage(this IntPtr hWnd, int wMsg, IntPtr wParam, StringBuilder lParam);

		///<summary>
		///</summary>
		///<param name="wnd"></param>
		///<param name="text"></param>
		///<exception cref="ArgumentNullException"></exception>
		public static void SetText(this SystemWindow wnd, string text)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			wnd.HWnd.SendMessage((int)WM.SETTEXT, 0, text);
		}

		/// <summary>
		/// allocates a new console for the calling process.
		/// </summary>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero.
		/// To get extended error information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool AllocConsole();

		/// <summary>
		/// Detaches the calling process from its console
		/// </summary>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero.
		/// To get extended error information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("kernel32", SetLastError = true)]
		public static extern bool FreeConsole();

		/// <summary>
		/// Attaches the calling process to the console of the specified process.
		/// </summary>
		/// <param name="dwProcessId">[in] Identifier of the process, usually will be ATTACH_PARENT_PROCESS</param>
		/// <returns>If the function succeeds, the return value is nonzero.
		/// If the function fails, the return value is zero.
		/// To get extended error information, call Marshal.GetLastWin32Error.</returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AttachConsole(uint dwProcessId);

		/// <summary>Identifies the console of the parent of the current process as the console to be attached.
		/// always pass this with AttachConsole in .NET for stability reasons and mainly because
		/// I have NOT tested interprocess attaching in .NET so dont blame me if it doesnt work! </summary>
		public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

		/// <summary>
		/// calling process is already attached to a console
		/// </summary>
		public const int ERROR_ACCESS_DENIED = 5;

		/// <summary>
		/// calling process does not have a console
		/// </summary>
		public const int ERROR_INVALID_HANDLE = 6;

		/// <summary>
		/// calling process does not exist
		/// </summary>
		public const int ERROR_GEN_FAILURE = 31;

		/// <summary>
		/// Allocate a console if application started from within windows GUI.
		/// Detects the presence of an existing console associated with the application and
		/// attaches itself to it if available.
		/// </summary>
		public static bool AllocateConsole()
		{
			if (AttachConsole(ATTACH_PARENT_PROCESS))
				return true;
			else
			{
				if (Marshal.GetLastWin32Error() == ERROR_ACCESS_DENIED)
					return true;
			}

			return AllocConsole();
		}

		///<summary>
		///</summary>
		///<param name="wnd"></param>
		///<param name="elem"></param>
		///<exception cref="ArgumentNullException"></exception>
		public static void Command(this SystemWindow wnd, SystemWindow elem)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			if (elem == null)
				throw new ArgumentNullException("elem");

			wnd.SendMessage(WM.COMMAND, elem.DialogID, 0);
		}

		public static bool Equals(this Process firstProcess, Process secondProcess)
		{
			if (firstProcess == null)
				throw new ArgumentNullException("firstProcess");

			if (secondProcess == null)
				throw new ArgumentNullException("secondProcess");

			return firstProcess.Id == secondProcess.Id;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dllname"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr LoadLibrary([In]string dllname);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hModule"></param>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern void FreeLibrary([In]IntPtr hModule);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hModule"></param>
		/// <param name="procName"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetProcAddress([In]IntPtr hModule, [In]string procName);

		public static int MakeParam<TLo, THi>(TLo loWord, THi hiWord)
		{
			return (loWord.To<int>() & 0xFFFF) + ((hiWord.To<int>() & 0xFFFF) << 16);
		}

		public static int HiWord(this int iValue)
		{
			return ((iValue >> 16) & 0xFFFF);
		}

		public static int LoWord(this int iValue)
		{
			return (iValue & 0xFFFF);
		}

		public static void PressKeyButton(this SystemWindow window, VirtualKeys key)
		{
			window.SendMessage(WM.KEYDOWN, (int)key, 0);
			window.SendMessage(WM.KEYUP, (int)key, 0);
		}

		//
		// http://benreichelt.net/blog/2006/01/31/get-process-user-name/
		//
		public static string GetOwner(this Process process)
		{
			if (process == null)
				throw new ArgumentNullException("process");

			var query = "Select * From Win32_Process Where ProcessID = " + process.Id;
			var searcher = new ManagementObjectSearcher(query);
			var processList = searcher.Get();

			foreach (ManagementObject obj in processList)
			{
				var argList = new[] { string.Empty };
				var returnVal = obj.InvokeMethod("GetOwner", argList).To<int>();
				if (returnVal == 0)
					return argList[0];
			}

			return string.Empty;
		}

		public static string GetFileName(this Process process)
		{
			if (process == null)
				throw new ArgumentNullException("process");

			return process.MainModule.FileName;
		}

		public static bool WaitForInputIdle(this Process process, TimeSpan timeOut)
		{
			if (process == null)
				throw new ArgumentNullException("process");

			return process.WaitForInputIdle((int)timeOut.TotalMilliseconds);
		}

		//
		// http://pinvoke.net/default.aspx/user32/ShowWindow.html
		//

		/// <summary>Shows a Window</summary>
		/// <remarks>
		/// <para>To perform certain special effects when showing or hiding a 
		/// window, use AnimateWindow.</para>
		///<para>The first time an application calls ShowWindow, it should use 
		///the WinMain function's nCmdShow parameter as its nCmdShow parameter. 
		///Subsequent calls to ShowWindow must use one of the values in the 
		///given list, instead of the one specified by the WinMain function's 
		///nCmdShow parameter.</para>
		///<para>As noted in the discussion of the nCmdShow parameter, the 
		///nCmdShow value is ignored in the first call to ShowWindow if the 
		///program that launched the application specifies startup information 
		///in the structure. In this case, ShowWindow uses the information 
		///specified in the STARTUPINFO structure to show the window. On 
		///subsequent calls, the application must call ShowWindow with nCmdShow 
		///set to SW_SHOWDEFAULT to use the startup information provided by the 
		///program that launched the application. This behavior is designed for 
		///the following situations: </para>
		///<list type="">
		///    <item>Applications create their main window by calling CreateWindow 
		///    with the WS_VISIBLE flag set. </item>
		///    <item>Applications create their main window by calling CreateWindow 
		///    with the WS_VISIBLE flag cleared, and later call ShowWindow with the 
		///    SW_SHOW flag set to make it visible.</item>
		///</list></remarks>
		/// <param name="hWnd">Handle to the window.</param>
		/// <param name="nCmdShow">Specifies how the window is to be shown. 
		/// This parameter is ignored the first time an application calls 
		/// ShowWindow, if the program that launched the application provides a 
		/// STARTUPINFO structure. Otherwise, the first time ShowWindow is called, 
		/// the value should be the value obtained by the WinMain function in its 
		/// nCmdShow parameter. In subsequent calls, this parameter can be one of 
		/// the WindowShowStyle members.</param>
		/// <returns>
		/// If the window was previously visible, the return value is nonzero. 
		/// If the window was previously hidden, the return value is zero.
		/// </returns>
		[DllImport("user32.dll")]
		public static extern bool ShowWindow(this IntPtr hWnd, WindowShowStyle nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetWindowThreadProcessId(this IntPtr hWnd, out int lpdwProcessId);

		public static int GetProcessId(this SystemWindow wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			int pid;
			GetWindowThreadProcessId(wnd.HWnd, out pid);
			return pid;
		}

		public const int GW_HWNDNEXT = 2; // The next window is below the specified window
		public const int GW_HWNDPREV = 3; // The previous window is above

		[DllImport("user32.dll")]
		static extern IntPtr GetTopWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindow", SetLastError = true)]
		public static extern IntPtr GetNextWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.U4)] int wFlag);

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

			var hwnd = GetTopWindow(IntPtr.Zero);
			if (hwnd != IntPtr.Zero)
			{
				while ((!IsWindowVisible(hwnd) || frm == null) && hwnd != hWndMainFrm)
				{
					// Get next window under the current handler
					hwnd = GetNextWindow(hwnd, GW_HWNDNEXT);

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