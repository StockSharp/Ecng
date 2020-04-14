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

	using Ecng.Interop;
#endif
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Localization;

	using Microsoft.Win32;

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

			if (obj is T t)
				return t;

			foreach (var child in LogicalTreeHelper.GetChildren(obj).OfType<DependencyObject>())
			{
				if (child is T t2)
				{
					return t2;
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
				if (child is T t)
					yield return t;

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
			return value == DependencyProperty.UnsetValue ? default : value.To<T>();
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

			// åñëè WPF õîñòèöà â WinForms ÷åðåç ElementHost
			//if (wnd == null && Application.Current == null)
			//	wnd = XamlNativeHelper.GetNativeOwner();

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

		public static bool ShowModal(this Window wnd, Window owner)
		{
			if (wnd == null)
				throw new ArgumentNullException(nameof(wnd));

			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			wnd.Owner = owner;

			//try
			//{
				return wnd.ShowDialog() == true;
			//}
			//finally
			//{
			//	owner.TryCloseNativeOwner();
			//}
		}

		public static bool ShowModal(this CommonDialog dlg, DependencyObject obj)
		{
			return dlg.ShowModal(obj.GetWindow());
		}

		public static bool ShowModal(this CommonDialog dlg, Window owner)
		{
			if (dlg == null)
				throw new ArgumentNullException(nameof(dlg));

			if (owner == null)
				throw new ArgumentNullException(nameof(owner));

			return dlg.ShowDialog(owner) == true;
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

			if (parent.Left > SystemParameters.VirtualScreenWidth || parent.Left < 0 || parent.Left.IsNaN())
			{
				parent.Left = (int)Math.Abs((SystemParameters.VirtualScreenWidth - parent.Width) / 2);
			}

			if (parent.Top > SystemParameters.VirtualScreenHeight || parent.Top < 0 || parent.Top.IsNaN())
			{
				parent.Top = (int)Math.Abs((SystemParameters.VirtualScreenHeight - parent.Height) / 2);
			}

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

		public static void SetMultiBinding(this DependencyObject obj, DependencyProperty prop, IMultiValueConverter conv, object param, params Binding[] bindings)
		{
			if(!(bindings?.Length > 1))
				throw new ArgumentException(nameof(bindings));

			var mb = new MultiBinding { Converter = conv ?? throw new ArgumentNullException(nameof(conv)) };

			foreach (var b in bindings)
			{
				b.Mode = BindingMode.OneWay;
				mb.Bindings.Add(b);
			}

			BindingOperations.SetBinding(obj, prop, mb);
		}

		private static readonly List<IDisposable> _listeners = new List<IDisposable>();

		public static IDisposable AddPropertyListener(this DependencyObject sourceObject, DependencyProperty property, Action<DependencyPropertyChangedEventArgs> onChanged)
		{
			var l = new DependencyPropertyListener(sourceObject, property, onChanged);
			_listeners.Add(l);
			return l;
		}

		/// <summary>
		/// Checks if supplied dispatcher/dispatcher object OR current thread is associated with Dispatcher.
		/// </summary>
		/// <param name="obj">Any DispatcherObject or Dispatcher or anything else (to check using Dispatcher.FromThread())</param>
		public static void EnsureUIThread(this object obj)
		{
			if (((obj as DispatcherObject)?.Dispatcher ?? obj as Dispatcher ?? CurrentThreadDispatcher)?.CheckAccess() != true)
				throw new InvalidOperationException("Operation is allowed for UI thread only.");
		}

		private class DependencyPropertyListener : DependencyObject, IDisposable
		{
			private static readonly DependencyProperty _proxyProperty = DependencyProperty.Register("Proxy", typeof(object), typeof(DependencyPropertyListener), new PropertyMetadata(null, OnProxyChanged));

			private readonly Action<DependencyPropertyChangedEventArgs> _action;
			private bool _isDisposed;

			public DependencyPropertyListener(DependencyObject source, DependencyProperty property, Action<DependencyPropertyChangedEventArgs> action)
			{
				this.SetBindings(_proxyProperty, source, new PropertyPath(property), BindingMode.OneWay);
				_action = action;
			}

			void IDisposable.Dispose()
			{
				if (_isDisposed)
					return;

				_isDisposed = true;
				BindingOperations.ClearBinding(this, _proxyProperty);
			}

			private static void OnProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
			{
				var listener = (DependencyPropertyListener) d;
				if (listener._isDisposed)
					return;

				listener._action?.Invoke(e);
			}
		}
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

		public static Color ToColorEx(this object value)
		{
			if (value is int intVal)
				return ToColor(intVal);
			else
				return ((SettingsStorage)value).ToColor();
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

		////
		//// https://www.devexpress.com/Support/Center/Question/Details/T422976
		////
		//public static BitmapImage RenderDrawing(this DrawingImage drawingImage, Size drawingImageSize)
		//{
		//	if (drawingImage == null)
		//		throw new ArgumentNullException(nameof(drawingImage));

		//	double currentDpi = new System.Windows.Forms.TextBox().CreateGraphics().DpiX;

		//	var dpiScale = currentDpi / 96;

		//	var renderTargetBitmap =
		//		new RenderTargetBitmap((int)Math.Ceiling(drawingImageSize.Width * dpiScale),
		//			(int)Math.Ceiling(drawingImageSize.Height * dpiScale), currentDpi,
		//			currentDpi,
		//			PixelFormats.Pbgra32);

		//	var drawingVisual = new DrawingVisual();

		//	using (var drawingContext = drawingVisual.RenderOpen())
		//		drawingContext.DrawImage(drawingImage, new Rect(default, drawingImageSize));
		//	renderTargetBitmap.Render(drawingVisual);

		//	var pngBitmapEncoder = new PngBitmapEncoder();
		//	pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

		//	var memoryStream = new MemoryStream();
		//	pngBitmapEncoder.Save(memoryStream);
		//	memoryStream.Seek(0, SeekOrigin.Begin);

		//	var bitmapImage = new BitmapImage();

		//	bitmapImage.BeginInit();
		//	bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		//	bitmapImage.StreamSource = memoryStream;
		//	bitmapImage.EndInit();

		//	return bitmapImage;
		//}

		public static Brush GetBrush(this DrawingImage source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return source.Drawing.GetBrush();
		}

		public static Pen GetPen(this DrawingImage source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return source.Drawing.GetPen();
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

		private static Pen GetPen(this Drawing drawing)
		{
			switch (drawing)
			{
				case GeometryDrawing gd:
					return gd.Pen;
				case DrawingGroup dg:
				{
					foreach (var child in dg.Children)
					{
						var pen = GetPen(child);

						if (pen != null)
							return pen;
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

		public static void UpdatePen(this DrawingImage source, Pen pen)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			source.Drawing.UpdatePen(pen);
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

		private static void UpdatePen(this Drawing drawing, Pen pen)
		{
			if (pen == null)
				throw new ArgumentNullException(nameof(pen));

			switch (drawing)
			{
				case GeometryDrawing gd:
					gd.Pen = pen;
					break;
				case DrawingGroup dg:
					dg.UpdatePen(pen);
					break;
			}
		}

		private static void UpdateBrush(this DrawingGroup source, Brush brush)
		{
			foreach (var child in source.Children)
				UpdateBrush(child, brush);
		}

		private static void UpdatePen(this DrawingGroup source, Pen pen)
		{
			foreach (var child in source.Children)
				UpdatePen(child, pen);
		}

		public static SettingsStorage Save(this GridLength value)
		{
			var storage = new SettingsStorage();

			storage.SetValue("Type", value.GridUnitType);
			storage.SetValue("Value", value.Value);

			return storage;
		}

		public static GridLength LoadGridLength(this SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			return new GridLength(storage.GetValue<double>("Value"), storage.GetValue<GridUnitType>("Type"));
		}

		public static bool IsWpfColor(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			return type == typeof(Color);
		}

		public static int ToInt(this Color color)
		{
			return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
		}

		public static Color ToColor(this int color)
		{
			return Color.FromArgb((byte)(color >> 24), (byte)(color >> 16), (byte)(color >> 8), (byte)(color));
		}

		public static Color? ToColor(this string color)
		{
			return (Color?)ColorConverter.ConvertFromString(color);
		}

		private const string _brushTypeKey = "Type";

		public static SettingsStorage ToStorage(this Brush brush)
		{
			var storage = new SettingsStorage();

			switch (brush)
			{
				case SolidColorBrush sb:
					storage.SetValue(_brushTypeKey, typeof(SolidColorBrush).GetTypeName(true));
					storage.SetValue(nameof(SolidColorBrush.Color), sb.Color.ToStorage());
					storage.SetValue(nameof(SolidColorBrush.Opacity), sb.Opacity);
					break;

				case LinearGradientBrush gb:
					storage.SetValue(_brushTypeKey, typeof(LinearGradientBrush).GetTypeName(true));
					storage.SetValue(nameof(LinearGradientBrush.ColorInterpolationMode), gb.ColorInterpolationMode);
					storage.SetValue(nameof(LinearGradientBrush.Opacity), gb.Opacity);
					storage.SetValue(nameof(LinearGradientBrush.StartPoint), gb.StartPoint);
					storage.SetValue(nameof(LinearGradientBrush.EndPoint), gb.EndPoint);
					storage.SetValue(nameof(LinearGradientBrush.SpreadMethod), gb.SpreadMethod);
					storage.SetValue(nameof(LinearGradientBrush.GradientStops), ToStorages(gb.GradientStops));
					break;

				case RadialGradientBrush rb:
					storage.SetValue(_brushTypeKey, typeof(RadialGradientBrush).GetTypeName(true));
					storage.SetValue(nameof(RadialGradientBrush.ColorInterpolationMode), rb.ColorInterpolationMode);
					storage.SetValue(nameof(RadialGradientBrush.Opacity), rb.Opacity);
					storage.SetValue(nameof(RadialGradientBrush.Center), rb.Center);
					storage.SetValue(nameof(RadialGradientBrush.GradientOrigin), rb.GradientOrigin);
					storage.SetValue(nameof(RadialGradientBrush.SpreadMethod), rb.SpreadMethod);
					storage.SetValue(nameof(RadialGradientBrush.GradientStops), ToStorages(rb.GradientStops));
					storage.SetValue(nameof(RadialGradientBrush.RadiusX), rb.RadiusX);
					storage.SetValue(nameof(RadialGradientBrush.RadiusY), rb.RadiusY);
					storage.SetValue(nameof(RadialGradientBrush.MappingMode), rb.MappingMode);
					break;

				case null:
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(brush), brush.GetType().GetTypeName(false), "Unsupported brush type.".Translate());
			}

			return storage;
		}

		public static SettingsStorage[] ToStorages(this GradientStopCollection items)
		{
			if (items is null)
				throw new ArgumentNullException(nameof(items));

			var storages = new List<SettingsStorage>();

			foreach (var item in items)
			{
				var storage = new SettingsStorage();
				storage.SetValue(nameof(GradientStop.Color), item.Color.ToStorage());
				storage.SetValue(nameof(GradientStop.Offset), item.Offset);
				storages.Add(storage);
			}

			return storages.ToArray();
		}

		public static Brush ToBrush(this SettingsStorage storage)
		{
			if (storage is null)
				throw new ArgumentNullException(nameof(storage));

			var type = storage.GetValue<string>(_brushTypeKey).To<Type>();

			if (type is null)
				return null;

			if (type == typeof(SolidColorBrush))
			{
				return new SolidColorBrush
				{
					Color = storage.GetValue<SettingsStorage>(nameof(SolidColorBrush.Color)).ToColor(),
					Opacity = storage.GetValue<double>(nameof(SolidColorBrush.Opacity))
				};
			}
			else if (type == typeof(LinearGradientBrush))
			{
				var brush = new LinearGradientBrush();

				brush.ColorInterpolationMode = storage.GetValue(nameof(LinearGradientBrush.ColorInterpolationMode), brush.ColorInterpolationMode);
				brush.Opacity = storage.GetValue(nameof(LinearGradientBrush.Opacity), brush.Opacity);
				brush.StartPoint = storage.GetValue(nameof(LinearGradientBrush.StartPoint), brush.StartPoint);
				brush.EndPoint = storage.GetValue(nameof(LinearGradientBrush.EndPoint), brush.EndPoint);
				brush.SpreadMethod = storage.GetValue(nameof(LinearGradientBrush.SpreadMethod), brush.SpreadMethod);

				LoadGradientStops(brush, storage.GetValue<IEnumerable<SettingsStorage>>(nameof(LinearGradientBrush.GradientStops)));

				return brush;
			}
			else if (type == typeof(RadialGradientBrush))
			{
				var brush = new RadialGradientBrush();
				brush.ColorInterpolationMode = storage.GetValue(nameof(RadialGradientBrush.ColorInterpolationMode), brush.ColorInterpolationMode);
				brush.Opacity = storage.GetValue(nameof(RadialGradientBrush.Opacity), brush.Opacity);
				brush.Center = storage.GetValue(nameof(RadialGradientBrush.Center), brush.Center);
				brush.GradientOrigin = storage.GetValue(nameof(RadialGradientBrush.GradientOrigin), brush.GradientOrigin);
				brush.SpreadMethod = storage.GetValue(nameof(RadialGradientBrush.SpreadMethod), brush.SpreadMethod);
				brush.RadiusX = storage.GetValue(nameof(RadialGradientBrush.RadiusX), brush.RadiusX);
				brush.RadiusY = storage.GetValue(nameof(RadialGradientBrush.RadiusY), brush.RadiusY);
				brush.MappingMode = storage.GetValue(nameof(RadialGradientBrush.MappingMode), brush.MappingMode);

				LoadGradientStops(brush, storage.GetValue<IEnumerable<SettingsStorage>>(nameof(RadialGradientBrush.GradientStops)));

				return brush;
			}
			else
				throw new InvalidOperationException("Unknown brush type {0}.".Translate().Put(type));
		}

		public static void LoadGradientStops(this GradientBrush brush, IEnumerable<SettingsStorage> storages)
		{
			if (brush is null)
				throw new ArgumentNullException(nameof(brush));

			if (storages is null)
				throw new ArgumentNullException(nameof(storages));

			foreach (var storage in storages)
			{
				var color = storage.GetValue<SettingsStorage>(nameof(GradientStop.Color)).ToColor();
				var offset = storage.GetValue<double>(nameof(GradientStop.Offset));

				brush.GradientStops.Add(new GradientStop(color, offset));
			}
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