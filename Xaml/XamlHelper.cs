namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;
	using System.Security;
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

	using Microsoft.Win32;

	using Ookii.Dialogs.Wpf;

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
				throw new ArgumentNullException(nameof(bounds));

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
				throw new ArgumentNullException(nameof(elem));

			return new Size<int>(elem.Width.To<int>(), elem.Height.To<int>());
		}

		public static void SetSize(this FrameworkElement elem, Size<int> size)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

			if (size == null)
				throw new ArgumentNullException(nameof(size));

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
				throw new ArgumentNullException(nameof(elem));

			return new Point<int>(elem.GetValue(Canvas.LeftProperty).To<int>(), elem.GetValue(Canvas.TopProperty).To<int>());
		}

		public static void SetLocation(this DependencyObject elem, Point<int> location)
		//where T : struct, IEquatable<T>
		//where TOperator : IOperator<T>, new()
		{
			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

			if (location == null)
				throw new ArgumentNullException(nameof(location));

			elem.SetValue(Canvas.LeftProperty, location.X.To<double>());
			elem.SetValue(Canvas.TopProperty, location.Y.To<double>());
		}

		#endregion

		#region Visibility

		public static void SetVisibility(this UIElement elem, bool isVisible)
		{
			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

			elem.Visibility = (isVisible) ? Visibility.Visible : Visibility.Collapsed;
		}

		public static bool GetVisibility(this UIElement elem)
		{
			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

			return (elem.Visibility == Visibility.Visible);
		}

		#endregion

		#region Url

		private static readonly BitmapImage _empty = new BitmapImage();

		public static void SetUrl(this ImageBrush brush, Uri url)
		{
			if (brush == null)
				throw new ArgumentNullException(nameof(brush));

			brush.ImageSource = CreateSource(url);
		}

		public static void SetUrl(this Image img, Uri url)
		{
			if (img == null)
				throw new ArgumentNullException(nameof(img));

			img.Source = CreateSource(url);
		}

		public static void SetEmptyUrl(this Image img)
		{
			if (img == null)
				throw new ArgumentNullException(nameof(img));

			img.Source = _empty;
		}

		public static void SetEmptyUrl(this ImageBrush brush)
		{
			if (brush == null)
				throw new ArgumentNullException(nameof(brush));

			brush.ImageSource = _empty;
		}

		private static readonly Dictionary<Uri, ImageSource> _sourceCache = new Dictionary<Uri, ImageSource>();

		private static ImageSource CreateSource(Uri url)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));

			return _sourceCache.SafeAdd(url, key => new BitmapImage(url));
		}

		public static void SetUrl(this ImageSource source, Uri url)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var bmp = ((BitmapImage)source);
			bmp.UriSource = url;
		}

		public static Uri GetUrl(this ImageBrush brush)
		{
			if (brush == null)
				throw new ArgumentNullException(nameof(brush));

			return brush.ImageSource.GetUrl();
		}

		public static Uri GetUrl(this Image img)
		{
			if (img == null)
				throw new ArgumentNullException(nameof(img));

#if SILVERLIGHT
			return img.Source.GetUrl();
#else
			return ((IUriContext)img).BaseUri;
#endif
		}

		public static Uri GetUrl(this ImageSource source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

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
				throw new ArgumentNullException(nameof(elem));

			var retVal = (T)elem.FindName(name);

			if (throwException && retVal == null)
				throw new ArgumentException("Element with name '{0}' doesn't exits.".Put(name), nameof(name));

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
			return obj.FindVisualChilds<T>().FirstOrDefault();
		}

		public static IEnumerable<T> FindVisualChilds<T>(this DependencyObject obj)
			where T : DependencyObject
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
			{
				var child = VisualTreeHelper.GetChild(obj, i);
				if (child is T)
					yield return (T)child;

				foreach (var childOfChild in FindVisualChilds<T>(child))
					yield return childOfChild;
			}
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
				throw new ArgumentNullException(nameof(obj));

			return (bool?)obj.GetValue(DialogResultProperty);
		}

		public static void SetDialogResult(DependencyObject obj, bool? value)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

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
				throw new ArgumentNullException(nameof(type));

			var attr = type.GetAttribute<IconAttribute>();
			return attr == null ? null : (attr.IsFullPath ? new Uri(attr.Icon, UriKind.Relative) : attr.Icon.GetResourceUrl(type));
		}

		public static Uri GetResourceUrl(this string resName, Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type.Assembly.GetResourceUrl(resName);
		}

		private static Uri GetResourceUrl(this Assembly assembly, string resName)
		{
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			if (resName.IsEmpty())
				throw new ArgumentNullException(nameof(resName));

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
				throw new ArgumentNullException(nameof(obj));

			obj.Dispatcher.GuiAsync(action);
		}

		public static void GuiAsync(this Dispatcher dispatcher, Action action)
		{
			dispatcher.GuiAsync(action, DispatcherPriority.Normal);
		}

		public static void GuiAsync(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
		{
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));

			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (dispatcher.CheckAccess())
				action();
			else
				dispatcher.BeginInvoke(action, priority);
		}

		public static Size<int> GetActualSize(this FrameworkElement elem)
		{
			if (elem == null)
				throw new ArgumentNullException(nameof(elem));

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
				throw new ArgumentNullException(nameof(obj));

			obj.Dispatcher.GuiSync(action);
		}

		public static T GuiSync<T>(this DispatcherObject obj, Func<T> func)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			return obj.Dispatcher.GuiSync(func);
		}

		public static void GuiSync(this Dispatcher dispatcher, Action action)
		{
			dispatcher.GuiSync(action, DispatcherPriority.Normal);
		}

		public static void GuiSync(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
		{
			if (dispatcher == null)
				throw new ArgumentNullException(nameof(dispatcher));

			if (action == null)
				throw new ArgumentNullException(nameof(action));

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
				throw new ArgumentNullException(nameof(dispatcher));

			if (func == null)
				throw new ArgumentNullException(nameof(func));

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
				throw new ArgumentNullException(nameof(window));

			return window.GetImage(((FrameworkElement)window.Content).GetActualSize());
		}

		public static BitmapSource GetImage(this FrameworkElement elem)
		{
			return elem.GetImage(elem.GetActualSize());
		}

		public static BitmapSource GetImage(this Visual visual, Size<int> size)
		{
			if (size == null)
				throw new ArgumentNullException(nameof(size));

			return visual.GetImage(size.Width, size.Height);
		}

		public static BitmapSource GetImage(this Visual visual, int width, int height)
		{
			if (visual == null)
				throw new ArgumentNullException(nameof(visual));

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

		public static void SaveImage(this BitmapSource image, Stream file)
		{
			if (image == null)
				throw new ArgumentNullException(nameof(image));

			if (file == null)
				throw new ArgumentNullException(nameof(file));

			//using (var stream = File.Create(filePath))
			//{
			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(image));
			encoder.Save(file);
			//}
		}

		public static BitmapImage ToImage(byte[] imageData)
		{
			using (var mem = new MemoryStream(imageData))
			{
				mem.Position = 0;

				var image = new BitmapImage();

				image.BeginInit();
				image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.UriSource = null;
				image.StreamSource = mem;
				image.EndInit();

				//image.Freeze();

				return image;
			}
		}

		//public static void CopyImage(this BitmapSource image)
		//{
		//    if (image == null)
		//        throw new ArgumentNullException("image");

		//    Clipboard.SetImage(image);
		//}

		/// <summary>
		/// Cast value to specified type.
		/// </summary>
		/// <typeparam name="T">Return type.</typeparam>
		/// <param name="value">Source value.</param>
		/// <returns>Casted value.</returns>
		public static T WpfCast<T>(this object value)
		{
			return value == DependencyProperty.UnsetValue ? default(T) : value.To<T>();
		}

		public static ICollectionView MakeFilterable<T>(this IEnumerable<T> source, Func<T, bool> filter)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

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
			return Application.Current == null ? wnd.ShowDialog() == true : wnd.ShowModal(Application.Current.MainWindow);
		}

		public static bool ShowModal(this Window wnd, DependencyObject obj)
		{
			return wnd.ShowModal(obj.GetWindow());
		}

		public static bool ShowModal(this VistaFileDialog dlg, DependencyObject obj)
		{
			return dlg.ShowDialog(obj.GetWindow()) == true;
		}

		public static bool ShowModal(this VistaFolderBrowserDialog dlg, DependencyObject obj)
		{
			return dlg.ShowDialog(obj.GetWindow()) == true;
		}

		public static bool ShowModal(this Window wnd, Window owner)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

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
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		public static void BringToFront(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

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
			{
				// https://stackoverflow.com/a/17678542
				Clipboard.SetDataObject(value.To<string>());
			}
			else if (value is BitmapSource)
				Clipboard.SetImage(value.To<BitmapSource>());
			else
				throw new NotSupportedException();
		}

		public static void TryCopyToClipboard<T>(this T value, int attempts = 5)
		{
			if (attempts < 1)
				throw new ArgumentOutOfRangeException(nameof(attempts));

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
				throw new ArgumentNullException(nameof(child));

			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

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
				throw new ArgumentNullException(nameof(window));

			window.Closing += OnHideableClosing;
		}

		public static void DeleteHideable(this Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

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
				throw new ArgumentNullException(nameof(app));

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

		public static void SetBindings(this DependencyObject obj, DependencyProperty property, object dataObject, string path, BindingMode mode = BindingMode.TwoWay, IValueConverter converter = null, object parameter = null)
		{
			BindingOperations.SetBinding(obj, property, new Binding(path)
			{
				Source = dataObject,
				Mode = mode,
				Converter = converter,
				ConverterParameter = parameter
			});
		}

		public static void SetBindings(this DependencyObject obj, DependencyProperty property, object dataObject, PropertyPath path, BindingMode mode = BindingMode.TwoWay, IValueConverter converter = null, object parameter = null)
		{
			BindingOperations.SetBinding(obj, property, new Binding
			{
				Source = dataObject,
				Path = path,
				Mode = mode,
				Converter = converter,
				ConverterParameter = parameter
			});
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
				throw new ArgumentNullException(nameof(application));

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
				throw new ArgumentNullException(nameof(settings));

			return Color.FromArgb(settings.GetValue<byte>("A"), settings.GetValue<byte>("R"), settings.GetValue<byte>("G"), settings.GetValue<byte>("B"));
		}

		#endregion

		public static bool IsDesignMode(this DependencyObject obj)
		{
			return DesignerProperties.GetIsInDesignMode(obj);
		}

		// http://stackoverflow.com/a/3190790
		public static void HideScriptErrors(this WebBrowser wb, bool hide)
		{
			var fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
			if (fiComWebBrowser == null)
				return;

			var objComWebBrowser = fiComWebBrowser.GetValue(wb);
			if (objComWebBrowser == null)
			{
				wb.Loaded += (o, s) => HideScriptErrors(wb, hide); //In case we are to early
				return;
			}

			objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
		}

		//
		// http://www.codeproject.com/Articles/793687/Configuring-the-emulation-mode-of-an-Internet-Expl
		//

		private const string _internetExplorerRootKey = @"Software\Microsoft\Internet Explorer";
		private const string _browserEmulationKey = _internetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

		public static bool SetBrowserEmulationVersion(this BrowserEmulationVersion version)
		{
			return version.SetBrowserEmulationVersion(Environment.GetCommandLineArgs()[0]);
		}

		public static bool SetBrowserEmulationVersion(this BrowserEmulationVersion version, string appName)
		{
			try
			{
				using (var key = Registry.CurrentUser.CreateSubKey(_browserEmulationKey))
				{
					var programName = Path.GetFileName(appName);

					if (version != BrowserEmulationVersion.Default)
					{
						// if it's a valid value, update or create the value
						key.SetValue(programName, (int)version, RegistryValueKind.DWord);
					}
					else
					{
						// otherwise, remove the existing value
						key.DeleteValue(programName, false);
					}

					return true;
				}
			}
			catch (SecurityException)
			{
				// The user does not have the permissions required to read from the registry key.
			}
			catch (UnauthorizedAccessException)
			{
				// The user does not have the necessary registry rights.
			}

			return false;
		}

		//private static readonly double _standartDpi = 96.0;
		//private static readonly double _currentDpi = new System.Windows.Forms.TextBox().CreateGraphics().DpiX;
		//private static readonly double _scale = _standartDpi / _currentDpi;

		//
		// https://www.devexpress.com/Support/Center/Question/Details/T422976
		//
		public static BitmapImage RenderDrawing(this DrawingImage drawingImage, Size drawingImageSize)
		{
			if (drawingImage == null)
				throw new ArgumentNullException(nameof(drawingImage));

			double currentDpi = new System.Windows.Forms.TextBox().CreateGraphics().DpiX;

			var dpiScale = currentDpi / 96;

			var renderTargetBitmap =
				new RenderTargetBitmap((int)Math.Ceiling(drawingImageSize.Width * dpiScale),
					(int)Math.Ceiling(drawingImageSize.Height * dpiScale), currentDpi,
					currentDpi,
					PixelFormats.Pbgra32);

			var drawingVisual = new DrawingVisual();

			using (var drawingContext = drawingVisual.RenderOpen())
				drawingContext.DrawImage(drawingImage, new Rect(default(Point), drawingImageSize));
			renderTargetBitmap.Render(drawingVisual);

			var pngBitmapEncoder = new PngBitmapEncoder();
			pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

			var memoryStream = new MemoryStream();
			pngBitmapEncoder.Save(memoryStream);
			memoryStream.Seek(0, SeekOrigin.Begin);

			var bitmapImage = new BitmapImage();

			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.StreamSource = memoryStream;
			bitmapImage.EndInit();

			return bitmapImage;
		}

		public static Brush GetBrush(this DrawingImage source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return source.Drawing.GetBrush();
		}

		private static Brush GetBrush(this Drawing drawing)
		{
			switch (drawing)
			{
				case GeometryDrawing gd:
					return gd.Brush;
				case DrawingGroup dg:
				{
					foreach (var child in dg.Children)
					{
						var brush = GetBrush(child);

						if (brush != null)
							return brush;
					}

					return null;
				}
				default:
					return null;
			}
		}

		public static void UpdateBrush(this DrawingImage source, Brush brush)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			//source = source.Clone();
			source.Drawing.UpdateBrush(brush);
			//return source;
		}

		private static void UpdateBrush(this Drawing drawing, Brush brush)
		{
			if (brush == null)
				throw new ArgumentNullException(nameof(brush));

			switch (drawing)
			{
				case GeometryDrawing gd:
					gd.Brush = brush;
					break;
				case DrawingGroup dg:
					dg.UpdateBrush(brush);
					break;
			}
		}

		private static void UpdateBrush(this DrawingGroup source, Brush brush)
		{
			foreach (var child in source.Children)
				UpdateBrush(child, brush);
		}
	}

	public enum BrowserEmulationVersion
	{
		Default = 0,
		Version7 = 7000,
		Version8 = 8000,
		Version8Standards = 8888,
		Version9 = 9000,
		Version9Standards = 9999,
		Version10 = 10000,
		Version10Standards = 10001,
		Version11 = 11000,
		Version11Edge = 11001
	}
}