namespace Ecng.UI
{
	using System;
	using System.Windows;
	using System.Windows.Browser;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	using Ecng.ComponentModel;
	using Ecng.Xaml;

	public static class SilverlightHelper
	{
		public static void Shift(this FrameworkElement elem, Point<int> diff)
			//where T : struct, IEquatable<T>
			//where TOperator : IOperator<T>, new()
		{
			elem.SetLocation(elem.GetLocation() + diff);
		}

		public static Point<int> GetPositionEx(this MouseEventArgs e)
		{
			return e.GetPositionEx(null);
		}

		public static Point<int> GetPositionEx(this MouseEventArgs e, UIElement elem)
		{
			if (e == null)
				throw new ArgumentNullException("e");

			return new Point<int>((int)e.GetPosition(elem).X, (int)e.GetPosition(elem).Y);
		}

		public static Point<int> GetAbsoluteLocation(this FrameworkElement elem, FrameworkElement top)
		{
			var location = elem.GetLocation();

			var parent = elem.Parent as FrameworkElement;
			if (parent != null && parent != top)
			{
				var parentLocation = parent.GetAbsoluteLocation(top);
				return location + parentLocation;
			}
			else
				return location;
		}

		public static Rectangle<int> GetAbsoluteBounds(this FrameworkElement elem, FrameworkElement top)
		{
			var location = elem.GetAbsoluteLocation(top);
			var size = elem.GetSize();
			return new Rectangle<int>(location, size);
		}

		public static string UserAgent
		{
			get
			{
				return Application.Current.IsRunningOutOfBrowser ? "OOB" : HtmlPage.BrowserInformation.UserAgent;
			}
		}

		public static ImageSource LoadFromRes(this Type type, string resName)
		{
			var sr = type.GetResourceStream(resName);
			var bmp = new BitmapImage();
			bmp.SetSource(sr.Stream);
			return bmp;
		}
	}
}