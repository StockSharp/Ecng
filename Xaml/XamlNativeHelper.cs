namespace Ecng.Xaml
{
	using System;
	using System.Drawing;
	using System.Drawing.Imaging;
	using System.Runtime.InteropServices;
	using System.Windows;
	using System.Windows.Forms;
	using System.Windows.Interop;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;

	using WinApplication = System.Windows.Forms.Application;
	using WinColor = System.Drawing.Color;
	using WpfColor = System.Windows.Media.Color;
	using WpfPoint = System.Windows.Point;
	using WinPoint = System.Drawing.Point;

	public static class XamlNativeHelper
	{
		private static readonly SynchronizedSet<Window> _nativeOwners = new SynchronizedSet<Window>();

		internal static Window GetNativeOwner()
		{
			// http://stackoverflow.com/questions/6690848/completely-hide-wpf-window-on-startup
			var wnd = new Window
			{
				Width = 0,
				Height = 0,
				WindowStyle = WindowStyle.None,
				ShowInTaskbar = false,
				ShowActivated = false,
				Visibility = Visibility.Hidden,
			};
			wnd.Show();
			wnd.SetOwnerHandle(WinApi.GetTopMostWindow().Handle);
			_nativeOwners.Add(wnd);
			return wnd;
		}

		internal static void TryCloseNativeOwner(this Window wnd)
		{
			if (_nativeOwners.Remove(wnd))
				wnd.Close();
		}

		public static IntPtr GetOwnerHandle(this Window wnd)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			return new WindowInteropHelper(wnd).Owner;
		}

		public static Window SetOwnerHandle(this Window wnd, IntPtr ownerHandle)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			new WindowInteropHelper(wnd).Owner = ownerHandle;
			return wnd;
		}

		public static bool ShowModalNative(this Window wnd, Control control)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			if (control == null)
				throw new ArgumentNullException("control");

			return wnd.SetOwnerHandle(control.Handle).ShowDialog() == true;
		}

		public static bool ShowModalNative(this CommonDialog dlg, Window owner)
		{
			if (dlg == null)
				throw new ArgumentNullException("dlg");

			if (owner == null)
				throw new ArgumentNullException("owner");

			return dlg.ShowDialog(owner.GetIWin32Window()) == DialogResult.OK;
		}

		public static bool ShowModalNative(this Form form, Window owner)
		{
			if (form == null)
				throw new ArgumentNullException("form");

			if (owner == null)
				throw new ArgumentNullException("owner");

			return form.ShowDialog(owner.GetIWin32Window()) == DialogResult.OK;
		}

		public static bool ShowModalNative(this Microsoft.Win32.CommonDialog dlg, Window owner)
		{
			if (dlg == null)
				throw new ArgumentNullException("dlg");

			if (owner == null)
				throw new ArgumentNullException("owner");

			return dlg.ShowDialog(owner) == true;
		}

		public static BitmapSource ToBitmapSource(this Bitmap source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var hBitmap = source.GetHbitmap();
			using (hBitmap.MakeDisposable(DeleteObject))
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		[DllImport("gdi32.dll")]
		private static extern void DeleteObject(IntPtr hObject);

		public static Bitmap ToBitmap(this BitmapSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			const System.Drawing.Imaging.PixelFormat format = System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
			var bmp = new Bitmap(source.PixelWidth, source.PixelHeight, format);
			var data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, format);
			using (data.MakeDisposable(bmp.UnlockBits))
			{
				source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
				return bmp;
			}
		}

		public static IWin32WindowEx GetIWin32Window(this Visual visual)
		{
			if (visual == null)
				throw new ArgumentNullException("visual");

			var source = (HwndSource)PresentationSource.FromVisual(visual);

			if (source == null)
				throw new ArgumentException("visual");

			return new NativeWindow(source.Handle);
		}

		private sealed class NativeWindow : IWin32WindowEx
		{
			private readonly IntPtr _handle;

			public NativeWindow(IntPtr handle)
			{
				_handle = handle;
			}

			#region System.Windows.Interop.IWin32Window Members

			IntPtr System.Windows.Interop.IWin32Window.Handle
			{
				get { return _handle; }
			}

			#endregion

			#region System.Windows.Forms.IWin32Window Members

			IntPtr System.Windows.Forms.IWin32Window.Handle
			{
				get { return _handle; }
			}

			#endregion
		}

		public static WpfPoint ToWpf(this WinPoint point)
		{
			return new WpfPoint(point.X, point.Y);
		}

		public static WinPoint ToWin(this WpfPoint point)
		{
			return new WinPoint(point.X.To<int>(), point.Y.To<int>());
		}

		public static WpfColor ToWpf(this WinColor c)
		{
			return WpfColor.FromArgb(c.A, c.R, c.G, c.B);
		}

		public static System.Drawing.Color ToWin(this WpfColor c)
		{
			return WinColor.FromArgb(c.A, c.R, c.G, c.B);
		}
	}
}