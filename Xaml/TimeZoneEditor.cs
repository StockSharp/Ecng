namespace Ecng.Xaml
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

			this.AddClearButton();
		}

		public TimeZoneInfo TimeZone
		{
			get => (TimeZoneInfo)EditValue;
			set => EditValue = value;
		}
	}
}