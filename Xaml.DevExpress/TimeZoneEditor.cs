namespace Ecng.Xaml.DevExp
{
	using System;

	using DevExpress.Xpf.Editors.Settings;

	class TimeZoneEditor : ComboBoxEditSettings
	{
		public TimeZoneEditor()
		{
			ItemsSource = TimeZoneInfo.GetSystemTimeZones();
		}
	}
}