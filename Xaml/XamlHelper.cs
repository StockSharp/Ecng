namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Markup;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Resources;
	using System.Windows.Threading;
#if !SILVERLIGHT
	using System.Linq;
	using System.Threading;
	using System.ComponentModel;
	using System.Windows.Data;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Xml;
	using System.Windows.Controls.Primitives;

	using Ecng.Interop;
#endif
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Serialization;

	public static class XamlHelper
	{
		#region Bounds

		public static Rectangle<int> GetBounds(this FrameworkElement elem)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			var location = elem.GetLocation();
			var size = elem.GetSize();
			return new Rectangle<int>(location, size);
		}

		public static void SetBounds(this FrameworkElement elem, Rectangle<int> bounds)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (bounds == null)
				throw new ArgumentNullException("bounds");

			elem.SetLocation(bounds.Location);
			elem.SetSize(bounds.Size);
		}

		#endregion

		#region Size

		public static Size<int> GetSize(this FrameworkElement elem)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			return new Size<int>(elem.Width.To<int>(), elem.Height.To<int>());
		}

		public static void SetSize(this FrameworkElement elem, Size<int> size)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			if (size == null)
				throw new ArgumentNullException("size");

			elem.Width = size.Width;
			elem.Height = size.Height;
		}

		#endregion

		#region Location

		public static Point<int> GetLocation(this DependencyObject elem)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			return new Point<int>(elem.GetValue(Canvas.LeftProperty).To<int>(), elem.GetValue(Canvas.TopProperty).To<int>());
		}

		public static void SetLocation(this DependencyObject elem, Point<int> location)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			if (location == null)
				throw new ArgumentNullException("location");

			elem.SetValue(Canvas.LeftProperty, location.X.To<double>());
			elem.SetValue(Canvas.TopProperty, location.Y.To<double>());
		}

		#endregion

		#region Visibility

		public static void SetVisibility(this UIElement elem, bool isVisible)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			elem.Visibility = (isVisible) ? Visibility.Visible : Visibility.Collapsed;
		}

		public static bool GetVisibility(this UIElement elem)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			return (elem.Visibility == Visibility.Visible);
		}

		#endregion

		#region Url

		private static readonly BitmapImage _empty = new BitmapImage();

		public static void SetUrl(this ImageBrush brush, Uri url)
		{
			if (brush == null)
				throw new ArgumentNullException("brush");

			brush.ImageSource = CreateSource(url);
		}

		public static void SetUrl(this Image img, Uri url)
		{
			if (img == null)
				throw new ArgumentNullException("img");

			img.Source = CreateSource(url);
		}

		public static void SetEmptyUrl(this Image img)
		{
			if (img == null)
				throw new ArgumentNullException("img");

			img.Source = _empty;
		}

		public static void SetEmptyUrl(this ImageBrush brush)
		{
			if (brush == null)
				throw new ArgumentNullException("brush");

			brush.ImageSource = _empty;
		}

		private readonly static Dictionary<Uri, ImageSource> _sourceCache = new Dictionary<Uri, ImageSource>();

		private static ImageSource CreateSource(Uri url)
		{
			if (url == null)
				throw new ArgumentNullException("url");

			return _sourceCache.SafeAdd(url, key => new BitmapImage(url));
		}

		public static void SetUrl(this ImageSource source, Uri url)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var bmp = ((BitmapImage)source);
			bmp.UriSource = url;
		}

		public static Uri GetUrl(this ImageBrush brush)
		{
			if (brush == null)
				throw new ArgumentNullException("brush");

			return brush.ImageSource.GetUrl();
		}

		public static Uri GetUrl(this Image img)
		{
			if (img == null)
				throw new ArgumentNullException("img");

#if SILVERLIGHT
			return img.Source.GetUrl();
#else
			return ((IUriContext)img).BaseUri;
#endif
		}

		public static Uri GetUrl(this ImageSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			return ((BitmapImage)source).UriSource;
		}

		#endregion

		public static T Find<T>(this FrameworkElement elem, string name)
			where T : FrameworkElement
		{
			return elem.Find<T>(name, true);
		}

		public static T Find<T>(this FrameworkElement elem, string name, bool throwException)
			where T : FrameworkElement
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			var retVal = (T)elem.FindName(name);

			if (throwException && retVal == null)
				throw new ArgumentException("Element with name '{0}' doesn't exits.".Put(name), "name");

			return retVal;
		}

#if !SILVERLIGHT
		public static T FindLogicalChild<T>(this DependencyObject obj)
			where T : DependencyObject
		{
			if (obj == null)
				return null;

			if (obj is T)
				return (T)obj;

			foreach (var child in LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>())
			{
				if (child is T)
				{
					return (T)child;
				}
				else
				{
					var childOfChild = FindLogicalChild<T>(child);
				
					if (childOfChild != null)
						return childOfChild;
				}
			}

			return null;
		}
#endif

		public static T FindVisualChild<T>(this DependencyObject obj)
			where T : DependencyObject
		{
			var count = VisualTreeHelper.GetChildrenCount(obj);
			for (var i = 0; i < count; i++)
			{
				var child = VisualTreeHelper.GetChild(obj, i);

				if (child is T)
				{
					return (T)child;
				}
				else
				{
					var childOfChild = FindVisualChild<T>(child);
				
					if (childOfChild != null)
						return childOfChild;
				}
			}
			return null;
		}

		#region ZIndex

		public static int GetZIndex(this UIElement elem)
		{
#if SILVERLIGHT
			return Canvas.GetZIndex(elem);
#else
			return Panel.GetZIndex(elem);
#endif
		}

		public static void SetZIndex(this UIElement elem, int index)
		{
#if SILVERLIGHT
			Canvas.SetZIndex(elem, index);
#else
			Panel.SetZIndex(elem, index);
#endif
		}

		#endregion

		#region Column

		public static int GetColumn(this FrameworkElement elem)
		{
			return Grid.GetColumn(elem);
		}

		public static void SetColumn(this FrameworkElement elem, int index)
		{
			Grid.SetColumn(elem, index);
		}

		#endregion

		#region Row

		public static int GetRow(this FrameworkElement elem)
		{
			return Grid.GetRow(elem);
		}

		public static void SetRow(this FrameworkElement elem, int index)
		{
			Grid.SetRow(elem, index);
		}

		#endregion

		#region ColumnSpan

		public static int GetColumnSpan(this FrameworkElement elem)
		{
			return Grid.GetColumnSpan(elem);
		}

		public static void SetColumnSpan(this FrameworkElement elem, int index)
		{
			Grid.SetColumnSpan(elem, index);
		}

		#endregion

		#region RowSpan

		public static int GetRowSpan(this FrameworkElement elem)
		{
			return Grid.GetRowSpan(elem);
		}

		public static void SetRowSpan(this FrameworkElement elem, int index)
		{
			Grid.SetRowSpan(elem, index);
		}

		#endregion

		#region ToolTip

		public static void SetToolTip<T>(this DependencyObject obj, T value)
		{
			ToolTipService.SetToolTip(obj, value);
		}

		public static T GetToolTip<T>(this DependencyObject obj)
		{
			return (T)ToolTipService.GetToolTip(obj);
		}

		#endregion

		public static T ToXaml<T>(this string xamlCode)
		{
#if SILVERLIGHT
			return (T)XamlReader.Load(xamlCode);
#else
			return (T)XamlReader.Load(XmlReader.Create(xamlCode));
#endif
		}

#if !SILVERLIGHT
		public static Uri GetResourceUrl(this string resName)
		{
			return Assembly.GetEntryAssembly().GetResourceUrl(resName);
		}

		// Boilerplate code to register attached property "bool? DialogResult"
		public static bool? GetDialogResult(DependencyObject obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			return (bool?)obj.GetValue(DialogResultProperty);
		}

		public static void SetDialogResult(DependencyObject obj, bool? value)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			obj.SetValue(DialogResultProperty, value);
		}

		// http://stackoverflow.com/questions/1759372/where-is-button-dialogresult-in-wpf
		public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(XamlHelper), new UIPropertyMetadata
		{
			PropertyChangedCallback = (obj, e) =>
			{
				// Implementation of DialogResult functionality
				var button = (Button)obj;

				button.Click += (sender, e2) =>
				{
					button.GetWindow().DialogResult = GetDialogResult(button);
				};
			}
		});
#endif
		public static Uri GetIconUrl(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			var attr = type.GetAttribute<IconAttribute>();
			return attr == null ? null : (attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(type));
		}

		public static Uri GetResourceUrl(this string resName, Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return type.Assembly.GetResourceUrl(resName);
		}

		private static Uri GetResourceUrl(this Assembly assembly, string resName)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			if (resName.IsEmpty())
				throw new ArgumentNullException("resName");

			var name = assembly.FullName;
			return new Uri("/" + name.Substring(0, name.IndexOf(',')) + ";component/" + resName, UriKind.Relative);
		}

		public static StreamResourceInfo GetResourceStream(this Type type, string resName)
		{
			return Application.GetResourceStream(resName.GetResourceUrl(type));
		}

#if SILVERLIGHT
		public static void GuiAsync(this DependencyObject obj, Action action)
#else
		public static void GuiAsync(this DispatcherObject obj, Action action)
#endif
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			obj.Dispatcher.GuiAsync(action);
		}

		public static void GuiAsync(this Dispatcher dispatcher, Action action)
		{
			dispatcher.GuiAsync(action, DispatcherPriority.Normal);
		}

		public static void GuiAsync(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher");

			if (action == null)
				throw new ArgumentNullException("action");

			if (dispatcher.CheckAccess())
				action();
			else
				dispatcher.BeginInvoke(action, priority);
		}

		public static Size<int> GetActualSize(this FrameworkElement elem)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			return new Size<int>((int)elem.ActualWidth, (int)elem.ActualHeight);
		}

#if !SILVERLIGHT
		public static Dispatcher CurrentThreadDispatcher
		{
			get
			{
				var dispatcher = Dispatcher.FromThread(Thread.CurrentThread);

				if (dispatcher == null)
					throw new InvalidOperationException("Current thread is not a GUI.");

				return dispatcher;
			}
		}

		public static void GuiSync(this DispatcherObject obj, Action action)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			obj.Dispatcher.GuiSync(action);
		}

		public static T GuiSync<T>(this DispatcherObject obj, Func<T> func)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			return obj.Dispatcher.GuiSync(func);
		}

		public static void GuiSync(this Dispatcher dispatcher, Action action)
		{
			dispatcher.GuiSync(action, DispatcherPriority.Normal);
		}

		public static void GuiSync(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher");

			if (action == null)
				throw new ArgumentNullException("action");

			if (dispatcher.CheckAccess())
				action();
			else
				dispatcher.Invoke(action, priority);
		}

		public static T GuiSync<T>(this Dispatcher dispatcher, Func<T> func)
		{
			return dispatcher.GuiSync(func, DispatcherPriority.Normal);
		}

		public static T GuiSync<T>(this Dispatcher dispatcher, Func<T> func, DispatcherPriority priority)
		{
			if (dispatcher == null)
				throw new ArgumentNullException("dispatcher");

			if (func == null)
				throw new ArgumentNullException("func");

			return dispatcher.CheckAccess() ? func() : dispatcher.Invoke(func, priority).To<T>();
		}

		// http://stackoverflow.com/questions/4502037/where-is-the-application-doevents-in-wpf
		public static void DoEvents()
		{
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
		}

		public static BitmapSource GetImage(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			return window.GetImage(((FrameworkElement)window.Content).GetActualSize());
		}

		public static BitmapSource GetImage(this FrameworkElement elem)
		{
			return elem.GetImage(elem.GetActualSize());
		}

		public static BitmapSource GetImage(this Visual visual, Size<int> size)
		{
			if (size == null)
				throw new ArgumentNullException("size");

			return visual.GetImage(size.Width, size.Height);
		}

		public static BitmapSource GetImage(this Visual visual, int width, int height)
		{
			if (visual == null)
				throw new ArgumentNullException("visual");

			var drawingVisual = new DrawingVisual();

			using (var drawingContext = drawingVisual.RenderOpen())
			{
				var sourceBrush = new VisualBrush(visual);
				drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(width, height)));
			}

			var source = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
			source.Render(drawingVisual);
			return source;
		}

		public static void SaveImage(this BitmapSource image, string filePath)
		{
			if (image == null)
				throw new ArgumentNullException("image");

			using (var stream = File.Create(filePath))
			{
				var encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(image));
				encoder.Save(stream);
			}
		}

		//public static void CopyImage(this BitmapSource image)
		//{
		//    if (image == null)
		//        throw new ArgumentNullException("image");

		//    Clipboard.SetImage(image);
		//}

		public static ICollectionView MakeFilterable<T>(this IEnumerable<T> source, Func<T, bool> filter)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			if (filter == null)
				throw new ArgumentNullException("filter");

			var view = CollectionViewSource.GetDefaultView(source);
			view.Filter = i => filter((T)i);
			return view;
		}

		public static Window GetWindow(this DependencyObject obj)
		{
			var wnd = Window.GetWindow(obj);

			// если WPF хостица в WinForms через ElementHost
			if (wnd == null && Application.Current == null)
				wnd = XamlNativeHelper.GetNativeOwner();

			return wnd;
		}

		public static bool ShowModal(this Window wnd)
		{
			return wnd.ShowModal(Application.Current.MainWindow);
		}

		public static bool ShowModal(this Window wnd, DependencyObject obj)
		{
			return wnd.ShowModal(obj.GetWindow());
		}

		public static bool ShowModal(this Window wnd, Window owner)
		{
			if (wnd == null)
				throw new ArgumentNullException("wnd");

			if (owner == null)
				throw new ArgumentNullException("owner");

			wnd.Owner = owner;

			try
			{
				return wnd.ShowDialog() == true;
			}
			finally
			{
				owner.TryCloseNativeOwner();
			}
		}

		public static void ShowOrHide(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		public static void BringToFront(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			if (window.WindowState == WindowState.Minimized)
				window.GetOwnerHandle().ShowWindow(WindowShowStyle.Restore);

			if (window.Visibility == Visibility.Visible)
				window.Activate();
			else
				window.Show();
		}

		public static void CopyToClipboard<T>(this T value)
		{
			if (value is byte[])
				Clipboard.SetAudio(value.To<byte[]>());
			else if (value is Stream)
				Clipboard.SetAudio(value.To<Stream>());
			else if (value is string)
				Clipboard.SetText(value.To<string>());
			else if (value is BitmapSource)
				Clipboard.SetImage(value.To<BitmapSource>());
			else
				throw new NotSupportedException();
		}

		public static void TryCopyToClipboard<T>(this T value, int attempts = 5)
		{
			if (attempts < 1)
				throw new ArgumentOutOfRangeException("attempts");

			while (attempts > 0)
			{
				try
				{
					CopyToClipboard(value);
					return;
				}
				catch (COMException)
				{
					//Clipboard can be already opened
					Thread.Sleep(5);
					attempts--;
				}
			}
		}

		public static T SetCenter<T>(this T child, Window parent)
			where T : Window
		{
			if (child == null)
				throw new ArgumentNullException("child");

			if (parent == null)
				throw new ArgumentNullException("parent");

			// проверить корректность родителя
			if (parent.Left > SystemParameters.VirtualScreenWidth || parent.Left < 0 || parent.Left.IsNaN())
			{
				parent.Left = (int)Math.Abs((SystemParameters.VirtualScreenWidth - parent.Width) / 2);
			}

			if (parent.Top > SystemParameters.VirtualScreenHeight || parent.Top < 0 || parent.Top.IsNaN())
			{
				parent.Top = (int)Math.Abs((SystemParameters.VirtualScreenHeight - parent.Height) / 2);
			}

			// взять центр родителя
			var widthCenter = parent.Left + parent.Width / 2;
			var heigthCenter = parent.Top + parent.Height / 2;

			if (child.Left > SystemParameters.VirtualScreenWidth || child.Left < 0 || child.Left.IsNaN())
			{
				child.Left = (int)Math.Abs(widthCenter - child.Width / 2);
			}

			if (child.Top > SystemParameters.VirtualScreenHeight || child.Top < 0 || child.Top.IsNaN())
			{
				child.Top = (int)Math.Abs(heigthCenter - child.Height / 2);
			}

			return child;
		}

		public static void MakeHideable(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			window.Closing += OnHideableClosing;
		}

		public static void DeleteHideable(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			window.Closing -= OnHideableClosing;
		}

		private static void OnHideableClosing(object sender, CancelEventArgs e)
		{
			e.Cancel = true;
			((Window)sender).Hide();
		}

		public static IEnumerable<Window> GetActiveWindows(this Application app)
		{
			if (app == null)
				throw new ArgumentNullException("app");

			return
				from window in app.Windows.Cast<Window>()
					where window.IsActive
				select window;
		}

		public static Window GetActiveWindow(this Application app)
		{
			return app.GetActiveWindows().FirstOrDefault();
		}

		public static Window GetActiveOrMainWindow(this Application app)
		{
			return app.GetActiveWindow() ?? app.MainWindow;
		}

		public static void SetBindings(this DependencyObject obj, DependencyProperty property, object dataObject, string propertyName, BindingMode mode = BindingMode.TwoWay, IValueConverter converter = null, object parameter = null)
		{
			var binding = new Binding(propertyName)
			{
				Source = dataObject,
				Mode = mode,
				Converter = converter,
				ConverterParameter = parameter
			};
			BindingOperations.SetBinding(obj, property, binding);
		}

		#region Menu

		public static void AddSubItems(this ItemsControl item, IEnumerable<MenuItem> items, Action<MenuItem> clicked)
		{
			foreach (var menuItem in items)
			{
				menuItem.Click += (s, a) => clicked((MenuItem)a.OriginalSource);
				item.Items.Add(menuItem);
			}
		}

		public static void ShowMenu(this UIElement ctrl, ContextMenu menu)
		{
			menu.Placement = PlacementMode.Bottom;
			menu.PlacementTarget = ctrl;
			menu.IsOpen = true;
		}

		#endregion
#endif

		public static void Restart(this Application application)
		{
			if (application == null)
				throw new ArgumentNullException("application");

			Process.Start(Application.ResourceAssembly.Location);
			application.Shutdown();
		}

		#region Color

		public static SettingsStorage ToStorage(this Color color)
		{
			var storage = new SettingsStorage();

			storage.SetValue("A", color.A);
			storage.SetValue("R", color.R);
			storage.SetValue("G", color.G);
			storage.SetValue("B", color.B);

			return storage;
		}

		public static Color ToTransparent(this Color color, byte alpha)
		{
			return Color.FromArgb(alpha, color.R, color.G, color.B);
		}

		public static Color ToColor(this SettingsStorage settings)
		{
			if (settings == null)
				throw new ArgumentNullException("settings");

			return Color.FromArgb(settings.GetValue<byte>("A"), settings.GetValue<byte>("R"), settings.GetValue<byte>("G"), settings.GetValue<byte>("B"));
		}

		#endregion

		public static bool IsDesignMode(this DependencyObject obj)
		{
			return DesignerProperties.GetIsInDesignMode(obj);
		}
	}
}