using System;
using System.Windows;

namespace Ecng.Xaml.Fonts
{
	public partial class ColorPicker
	{
		private readonly ColorPickerViewModel _viewModel;

		public static readonly RoutedEvent ColorChangedEvent = EventManager.RegisterRoutedEvent(
			"ColorChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ColorPicker));

		public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
			"SelectedColor", typeof(FontColor), typeof(ColorPicker), new UIPropertyMetadata(null));

		public ColorPicker()
		{
			InitializeComponent();
			_viewModel = new ColorPickerViewModel();
			DataContext = _viewModel;
		}

		public event RoutedEventHandler ColorChanged
		{
			add { AddHandler(ColorChangedEvent, value); }
			remove { RemoveHandler(ColorChangedEvent, value); }
		}

		public FontColor SelectedColor
		{
			get { return (FontColor)this.GetValue(SelectedColorProperty) ?? AvailableColors.GetFontColor("Black"); }

			set
			{
				_viewModel.SelectedFontColor = value;
				SetValue(SelectedColorProperty, value);
			}
		}

		private void RaiseColorChangedEvent()
		{
			var newEventArgs = new RoutedEventArgs(ColorPicker.ColorChangedEvent);
			RaiseEvent(newEventArgs);
		}

		private void superCombo_DropDownClosed(object sender, EventArgs e)
		{
			SetValue(SelectedColorProperty, _viewModel.SelectedFontColor);
			RaiseColorChangedEvent();
		}
	}
}