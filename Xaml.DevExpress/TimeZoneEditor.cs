namespace Ecng.Xaml.DevExp
{
	using System;

	using DevExpress.Xpf.Editors;

	using Ecng.Localization;
	
	public class TimeZoneEditor : ComboBoxEdit
	{
		public TimeZoneEditor()
		{
			ItemsSource = TimeZoneInfo.GetSystemTimeZones();
			DisplayMember = nameof(TimeZoneInfo.DisplayName);
			IsTextEditable = false;

			var btnReset = new ButtonInfo
			{
				GlyphKind = GlyphKind.Cancel,
				Content = "Reset".Translate()
			};
			btnReset.Click += (s, a) => EditValue = null;
			Buttons.Add(btnReset);
		}

		public TimeZoneInfo TimeZone
		{
			get => (TimeZoneInfo)EditValue;
			set => EditValue = value;
		}
	}
}