﻿namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	public class TimeZoneComboBox : ComboBox
	{
		public TimeZoneComboBox()
		{
			ItemsSource = TimeZoneInfo.GetSystemTimeZones();
			SelectedItem = TimeZoneInfo.Utc;
		}

		public static readonly DependencyProperty SelectedTimeZoneProperty =
			DependencyProperty.Register("SelectedTimeZone", typeof(TimeZoneInfo), typeof(TimeZoneComboBox), new UIPropertyMetadata(TimeZoneInfo.Utc, (s, e) =>
			{
				var ctrl = (TimeZoneComboBox)s;
				ctrl.SelectedItem = e.NewValue;
			}));

		public TimeZoneInfo SelectedTimeZone
		{
			get { return (TimeZoneInfo)GetValue(SelectedTimeZoneProperty); }
			set { SetValue(SelectedTimeZoneProperty, value); }
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			SelectedTimeZone = (TimeZoneInfo)SelectedItem;
			base.OnSelectionChanged(e);
		}
	}

	public class TimeZoneEditor : TypeEditor<TimeZoneComboBox>
	{
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = TimeZoneComboBox.SelectedTimeZoneProperty;
		}
	}
}