namespace Ecng.Xaml
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Markup;
	using System.Windows.Media.Imaging;

	using DevExpress.Utils.Svg;
	using DevExpress.Xpf.Core;
	using DevExpress.Xpf.Core.Native;
	using DevExpress.Xpf.Editors.Internal;

	public class IconUriBindingExtension : MarkupExtension
	{
		private static readonly DependencyProperty IconSinkProperty = DependencyProperty.RegisterAttached("IconSink", typeof(Uri), typeof(IconUriBindingExtension), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));

		public BindingBase Binding { get; }

		public IconUriBindingExtension(BindingBase binding)
		{
			Binding = binding;
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

			if(!(target?.TargetObject is DependencyObject targetObject) || !(target.TargetProperty is DependencyProperty targetProperty))
				return this;

			BindingOperations.SetBinding(targetObject, IconSinkProperty, Binding);

			return CreateBinding().ProvideValue(serviceProvider);
		}

		static MultiBinding CreateBinding()
		{
			var multiBinding = new MultiBinding
			{
				Converter = new SvgImageConverter(),
				Mode = BindingMode.OneWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			};

			multiBinding.Bindings.Add(new Binding
			{
				Path = new PropertyPath(string.Empty), RelativeSource = RelativeSource.Self, Mode = BindingMode.OneWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});

			multiBinding.Bindings.Add(new Binding
			{
				Path = new PropertyPath("(0)", ThemeManager.TreeWalkerProperty),
				RelativeSource = RelativeSource.Self,
				Mode = BindingMode.OneWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});

			multiBinding.Bindings.Add(new Binding
			{
				Path = new PropertyPath("(0)", SvgImageHelper.StateProperty),
				RelativeSource = RelativeSource.Self,
				Mode = BindingMode.OneWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});

			multiBinding.Bindings.Add(new Binding
			{
				Path = new PropertyPath("(0)", WpfSvgPalette.PaletteProperty),
				RelativeSource = RelativeSource.Self,
				Mode = BindingMode.OneWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});

			multiBinding.Bindings.Add(new Binding
			{
				Path = new PropertyPath("(0)", IconSinkProperty),
				RelativeSource = RelativeSource.Self,
				Mode = BindingMode.OneWay,
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});

			return multiBinding;
		}

		class SvgImageConverter : IMultiValueConverter {
			Uri _uri;
			bool _isSvg;
			SvgImage _svgImage;

			static InplaceResourceProvider _themeHelper;
			static InplaceResourceProvider ThemeHelper => _themeHelper ??= new InplaceResourceProvider(Theme.DeepBlueName);

			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
				if(values.Length != 5 || !(values[4] is Uri uri))
					return null;

				if(!uri.IsAbsoluteUri)
					uri = new Uri("pack://application:,,," + uri, UriKind.Absolute);

				if (_uri != uri)
				{
					_uri = uri;
					_isSvg = uri.ToString().ToLowerInvariant().EndsWith(".svg");
					_svgImage = _isSvg && _uri != null ? SvgImageHelper.GetOrCreate(_uri, SvgImageHelper.CreateImage) : null;
				}

				if (!_isSvg)
					return _uri != null ? new BitmapImage(_uri) : null;

				var targetObject = values[0] as DependencyObject;
				//var inheritedPalette = values[3] as WpfSvgPalette;
				var palette = (values[1] is ThemeTreeWalker treeWalker ? treeWalker.InplaceResourceProvider : ThemeHelper).GetSvgPalette(targetObject);
				var state = values[2] as string;

				if (_svgImage != null)
				{
					var size = new Size(_svgImage.Width, _svgImage.Height);
					return WpfSvgRenderer.CreateImageSource(_svgImage, size, palette, state, true);
				}

				if (_uri == null)
					return null;

				_svgImage = SvgImageHelper.GetOrCreate(_uri);

				return _svgImage == null ? null : WpfSvgRenderer.CreateImageSource(_svgImage, new Size(_svgImage.Width, _svgImage.Height), palette, state, true);
			}

			object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
		}
	}
}
