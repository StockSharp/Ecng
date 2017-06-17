namespace Ecng.Xaml
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows;
	using System.Windows.Controls.Primitives;
	using System.Windows.Interop;

	/// <summary>
	/// Non topmost popup.
	/// </summary>
	/// <remarks>http://chriscavanagh.wordpress.com/2008/08/13/non-topmost-wpf-popup/</remarks>
	public class NonTopmostPopup : Popup
	{
		public static DependencyProperty TopmostProperty = Window.TopmostProperty.AddOwner(typeof(NonTopmostPopup), new FrameworkPropertyMetadata(false, OnTopmostChanged));

		public bool Topmost
		{
			get => (bool)GetValue(TopmostProperty);
			set => SetValue(TopmostProperty, value);
		}

		private static void OnTopmostChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			((NonTopmostPopup)obj).UpdateWindow();
		}

		protected override void OnOpened(EventArgs e)
		{
			UpdateWindow();
		}

		private void UpdateWindow()
		{
			var source = PresentationSource.FromVisual(Child);

			if (source == null)
				return;

			var hwnd = ((HwndSource)source).Handle;
			RECT rect;

			if (GetWindowRect(hwnd, out rect))
			{
				SetWindowPos(hwnd, Topmost ? -1 : -2, rect.Left, rect.Top, (int)Width, (int)Height, 0);
			}
		}

		#region P/Invoke imports & definitions

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32", EntryPoint = "SetWindowPos")]
		private static extern int SetWindowPos(IntPtr hWnd, int hwndInsertAfter, int x, int y, int cx, int cy, int wFlags);

		#endregion
	}
}
