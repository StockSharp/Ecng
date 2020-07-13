namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.Grid;

	using Ecng.Common;
	using Ecng.ComponentModel;
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

		public static IItemsSource ToItemsSource<T>(this IEnumerable<T> values)
			where T : Enum
			=> new EnumSource<T>(values);

		public static void SetItemsSource<T>(this ComboBoxEditEx cb)
			where T : Enum
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.ItemsSource = new EnumSource<T>();
		}

		public static void SetItemsSource(this ComboBoxEditEx cb, Type enumType)
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.ItemsSource = typeof(EnumSource<>).Make(enumType).CreateInstance<object>();
		}

		public static void SetItemsSource<T>(this ComboBoxEditEx cb, IEnumerable<T> enums)
			where T : Enum
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.ItemsSource = enums;//.ToItemsSource().Values;
		}

		public static void SetItemsSource<T>(this ComboBoxEditEx cb, IItemsSource source)
			where T : Enum
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.DisplayMember = nameof(IItemsSourceItem.DisplayName);
			cb.ValueMember = nameof(IItemsSourceItem.Value);
			cb.ItemsSource = source.Values;
		}

		public static T GetSelected<T>(this ComboBoxEditEx cb) => (T)cb.Value;

		public static IEnumerable<T> GetSelecteds<T>(this ComboBoxEditEx cb) => cb.GetSelected<IEnumerable<T>>();

		public static void SetSelected<T>(this ComboBoxEditEx cb, T value) => cb.Value = value;
	}
}