namespace Ecng.Xaml
{
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Threading;

	/// <summary>
	/// http://www.11011.net/wpf-binding-properties.
	/// </summary>
	public class Proxy : FrameworkElement
	{
		static Proxy()
		{
			var inMetadata = new FrameworkPropertyMetadata((p, args) =>
			{
				if (BindingOperations.GetBinding(p, OutProperty) != null)
					((Proxy)p).Out = args.NewValue;
			})
			{
				BindsTwoWayByDefault = false,
				DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};

			InProperty = DependencyProperty.Register("In", typeof(object), typeof(Proxy), inMetadata);

			var outMetadata = new FrameworkPropertyMetadata((p, args) =>
			{
				var source = DependencyPropertyHelper.GetValueSource(p, args.Property);

				if (source.BaseValueSource != BaseValueSource.Local)
				{
					var proxy = (Proxy)p;
					var expected = proxy.In;

					if (!object.ReferenceEquals(args.NewValue, expected))
						XamlHelper.CurrentThreadDispatcher.GuiAsync(() => proxy.Out = proxy.In, DispatcherPriority.DataBind);
				}
			})
			{
				BindsTwoWayByDefault = true,
				DefaultUpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};

			OutProperty = DependencyProperty.Register("Out", typeof(object), typeof(Proxy), outMetadata);
		}

		public static readonly DependencyProperty InProperty;
		public static readonly DependencyProperty OutProperty;

		public Proxy()
		{
			Visibility = Visibility.Collapsed;
		}

		public object In
		{
			get { return GetValue(InProperty); }
			set { SetValue(InProperty, value); }
		}

		public object Out
		{
			get { return GetValue(OutProperty); }
			set { SetValue(OutProperty, value); }
		}
	}
}