namespace Ecng.Xaml
{
	using System;
	using System.Linq;
	using System.Windows;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.Grid;

	using Ecng.Localization;

	public static class EditorExtensions
	{
		public static void AddClearButton(this ButtonEdit edit, object emptyValue = null)
		{
			if (edit == null)
				throw new ArgumentNullException(nameof(edit));

			var btnReset = new ButtonInfo
			{
				GlyphKind = GlyphKind.Cancel,
				Content = "Reset".Translate()
			};
			btnReset.SetBindings(ContentElement.IsEnabledProperty, edit, nameof(edit.IsReadOnly), converter: new InverseBooleanConverter());
			btnReset.Click += (s, a) =>
			{
				if (edit.IsReadOnly)
					return;

				edit.SetCurrentValue(BaseEdit.EditValueProperty, emptyValue);
			};
			edit.Buttons.Add(btnReset);
		}

		public static void RemoveClearButton(this ButtonEdit edit)
		{
			if (edit == null)
				throw new ArgumentNullException(nameof(edit));

			var bi = edit.Buttons.FirstOrDefault(b => ((ButtonInfo)b).GlyphKind == GlyphKind.Cancel);
			if(bi != null)
				edit.Buttons.Remove(bi);
		}

		public static void AddClearButton(this ComboBoxEditSettings editSettings, object emptyValue = null)
		{
			if (editSettings == null)
				throw new ArgumentNullException(nameof(editSettings));

			var btnReset = new ButtonInfo
			{
				GlyphKind = GlyphKind.Cancel,
				Content = "Reset".Translate()
			};
			btnReset.Click += (sender, e) =>
			{
				var edit = BaseEdit.GetOwnerEdit((DependencyObject) sender);

				if (edit == null || edit.IsReadOnly)
					return;

				edit.SetCurrentValue(BaseEdit.EditValueProperty, emptyValue);
			};

			editSettings.Buttons.Add(btnReset);
		}

		public static T GetSelected<T>(this ComboBoxEditEx cb) => (T)cb.Value;

		public static void SetSelected<T>(this ComboBoxEditEx cb, T value) => cb.Value = value;
	}
}