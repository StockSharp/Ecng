namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	public class TimeZoneComboBox : ComboBox
	{
		public TimeZoneComboBox()
		{
			ItemsSource = TimeZoneInfo.GetSystemTimeZones();
			SelectedItem = TimeZoneInfo.Utc;
		}

		public static readonly DependencyProperty SelectedTimeZoneProperty =
			DependencyProperty.Register(nameof(SelectedTimeZone), typeof(TimeZoneInfo), typeof(TimeZoneComboBox), new UIPropertyMetadata(TimeZoneInfo.Utc, (s, e) =>
			{
				var ctrl = (TimeZoneComboBox)s;
				ctrl.SelectedItem = e.NewValue;
			}));

		public TimeZoneInfo SelectedTimeZone
		{
			get => (TimeZoneInfo)GetValue(SelectedTimeZoneProperty);
			set => SetValue(SelectedTimeZoneProperty, value);
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			SelectedTimeZone = (TimeZoneInfo)SelectedItem;
			base.OnSelectionChanged(e);
		}
	}
}