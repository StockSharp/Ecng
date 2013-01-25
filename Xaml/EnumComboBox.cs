namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	public static class EnumComboBox
	{
		private static readonly Dictionary<Type, PairSet<object, string>> _names = new Dictionary<Type, PairSet<object, string>>();
		private static readonly Dictionary<ComboBox, ObservableCollection<EnumerationMember>> _sources = new Dictionary<ComboBox,ObservableCollection<EnumerationMember>>();

		public class EnumerationMember
		{
			public string Description { get; set; }
			public object Value { get; set; }

			public override string ToString()
			{
				return Description;
			}
		}

		public static IEnumerable<T> GetDataSource<T>(this ComboBox comboBox)
			where T : struct
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			return (IEnumerable<T>)comboBox.ItemsSource;
		}

		public static void SetDataSource<T>(this ComboBox comboBox)
			where T : struct
		{
			comboBox.SetDataSource(Enumerator.GetValues<T>());
		}

		public static void SetDataSource<T>(this ComboBox comboBox, IEnumerable<T> dataSource)
			where T : struct
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			if (dataSource == null)
				throw new ArgumentNullException("dataSource");

			var set = _names.SafeAdd(typeof(T), key =>
			{
				var retVal = new PairSet<object, string>();

				foreach (var enumField in Enumerator.GetValues<T>())
					retVal.Add(enumField, enumField.GetDisplayName());

				return retVal;
			});

			GetItemsSource(comboBox).AddRange(dataSource.Select(item => new EnumerationMember { Description = set.GetValue(item), Value = item }));
		}

		public static ObservableCollection<EnumerationMember> GetItemsSource(this ComboBox comboBox)
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			return _sources.SafeAdd(comboBox, key =>
			{
				comboBox.DisplayMemberPath = "Description";
				comboBox.SelectedValuePath = "Value";

				var source = new ObservableCollection<EnumerationMember>();
				comboBox.ItemsSource = source;
				return source;
			});
		}

		public static T? GetSelectedValue<T>(this ComboBox comboBox)
			where T : struct
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			return comboBox.SelectedItem != null ? (T)((EnumerationMember)comboBox.SelectedItem).Value : (T?)null;
		}

		public static void SetSelectedValue<T>(this ComboBox comboBox, T? value)
			where T : struct
		{
			if (comboBox == null)
				throw new ArgumentNullException("comboBox");

			comboBox.SelectedValue = value;
		}
	}
}