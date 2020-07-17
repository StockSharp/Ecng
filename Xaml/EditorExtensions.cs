using System.ComponentModel;
using MoreLinq;

namespace Ecng.Xaml
{
	using System;
	using System.Collections;
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

		public static IItemsSource ToItemsSource(this object val, bool? excludeObsolete = null, ListSortDirection? sortOrder = null, Func<IItemsSourceItem, bool> filter = null)
			=> ItemsSourceBase.Create(val, excludeObsolete, sortOrder, filter);

		public static void SetItemsSource<T>(this ComboBoxEditEx cb)
			where T : Enum
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.SetItemsSource(Enumerator.GetValues<T>().ToItemsSource());
		}

		public static void SetItemsSource(this ComboBoxEditEx cb, Type enumType)
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			if (!enumType.IsEnum)
				throw new ArgumentException($"{enumType.FullName} is not an enum!");

			cb.SetItemsSource(enumType.GetValues().ToItemsSource());
		}

		public static void SetItemsSource<T>(this ComboBoxEditEx cb, IEnumerable<T> values)
		{
			if (cb is null)
				throw new ArgumentNullException(nameof(cb));

			cb.SetItemsSource(values.ToItemsSource());
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