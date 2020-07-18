namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
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

		public static IItemsSource ToItemsSource(this object val, Type itemValueType, bool? excludeObsolete = null, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null, Func<object, string> getName = null, Func<object, string> getDescription = null)
			=> ItemsSourceBase.Create(val, itemValueType, excludeObsolete, sortOrder, filter, getName, getDescription);

		public static IItemsSource ToItemsSource<T>(this IEnumerable<T> val, bool excludeObsolete = true, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null, Func<T, string> getName = null, Func<T, string> getDescription = null)
			=> new ItemsSourceBase<T>(val, excludeObsolete, sortOrder, filter, getName, getDescription);

		public static void SetItemsSource<T>(this ComboBoxEditEx cb)
			where T : Enum
		{
			cb.SetItemsSource(typeof(T));
		}

		public static void SetItemsSource(this ComboBoxEditEx cb, Type enumType)
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			if (!enumType.IsEnum)
				throw new ArgumentException($"{enumType.FullName} is not an enum!");

			cb.SetItemsSource(enumType.GetValues().ToItemsSource(enumType));
		}

		public static void SetItemsSource<T>(this ComboBoxEditEx cb, IEnumerable<T> values, Func<T, string> getName = null, Func<T, string> getDescription = null)
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.SetItemsSource(values.ToItemsSource(getName: getName, getDescription: getDescription));
		}

		public static void SetItemsSource(this ComboBoxEditEx cb, IItemsSource source)
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.ItemsSource = source;
		}

		public static T GetSelected<T>(this ComboBoxEditEx cb) => (T)cb.Value;

		public static IEnumerable<T> GetSelecteds<T>(this ComboBoxEditEx cb) => cb.GetSelected<IEnumerable<T>>();

		public static void SetSelected<T>(this ComboBoxEditEx cb, T value) => cb.Value = value;
	}
}