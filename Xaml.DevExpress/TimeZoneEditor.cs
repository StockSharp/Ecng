namespace Ecng.Xaml.DevExp
{
	using System;

	using DevExpress.Xpf.Editors;

	public class TimeZoneEditor : ComboBoxEdit
	{
		public TimeZoneEditor()
		{
			ItemsSource = TimeZoneInfo.GetSystemTimeZones();
			DisplayMember = nameof(TimeZoneInfo.DisplayName);
			IsTextEditable = false;
		}
	}
}