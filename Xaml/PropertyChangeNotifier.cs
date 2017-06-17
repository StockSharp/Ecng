namespace Ecng.Xaml
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Data;

	public sealed class PropertyChangeNotifier : DependencyObject, IDisposable
	{
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(PropertyChangeNotifier),
			new FrameworkPropertyMetadata(null, OnPropertyChanged));

		private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((PropertyChangeNotifier)d).ValueChanged?.Invoke();
		}

		[Bindable(true)]
		public object Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		private readonly WeakReference _propertySource;

		public DependencyObject PropertySource
		{
			get
			{
				try
				{
					return _propertySource.IsAlive ? _propertySource.Target as DependencyObject : null;
				}
				catch
				{
					return null;
				}
			}
		}

		public event Action ValueChanged;

		public PropertyChangeNotifier(DependencyObject propertySource, string path)
			: this(propertySource, new PropertyPath(path))
		{
		}

		public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
			: this(propertySource, new PropertyPath(property))
		{
		}

		public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
		{
			if (propertySource == null)
				throw new ArgumentNullException(nameof(propertySource));

			if (property == null)
				throw new ArgumentNullException(nameof(property));

			_propertySource = new WeakReference(propertySource);

			var binding = new Binding
			{
				Path = property,
				Mode = BindingMode.OneWay,
				Source = propertySource
			};
			BindingOperations.SetBinding(this, ValueProperty, binding);
		}

		public void Dispose()
		{
			BindingOperations.ClearBinding(this, ValueProperty);
		}
	}
}
