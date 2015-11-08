namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	public class EnumComboBox : ComboBox
	{
		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="EnumType"/>.
		/// </summary>
		public static readonly DependencyProperty EnumTypeProperty =
			DependencyProperty.Register("EnumType", typeof(Type), typeof(EnumComboBox), new PropertyMetadata(EnumTypePropertyChanged));

		private static void EnumTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d as EnumComboBox;
			if (ctrl == null)
				return;

			ctrl.SetDataSource((Type)e.NewValue);
		}

		public Type EnumType
		{
			get { return (Type)GetValue(EnumTypeProperty); }
			set { SetValue(EnumTypeProperty, value); }
		}
	}

	public static class EnumComboBoxHelper
	{
		public class EnumerationMember : NotifiableObject
		{
			private string _description;

			public string Description
			{
				get { return _description; }
				set
				{
					_description = value;
					NotifyChanged("Description");
				}
			}

			private object _value;

			public object Value
			{
				get { return _value; }
				set
				{
					_value = value;
					NotifyChanged("Value");
				}
			}

			public override string ToString()
			{
				return Description;
			}
		}

		private static readonly Dictionary<Type, Dictionary<object, string>> _names = new Dictionary<Type, Dictionary<object, string>>();
		private static readonly Dictionary<ComboBox, ObservableCollection<EnumerationMember>> _sources = new Dictionary<ComboBox, ObservableCollection<EnumerationMember>>();

		public static IEnumerable<T> GetDataSource<T>(this ComboBox comboBox)
			where T : struct
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

			return (IEnumerable<T>)comboBox.ItemsSource;
		}

		public static void SetDataSource<T>(this ComboBox comboBox)
			where T : struct
		{
			comboBox.SetDataSource(typeof(T));
		}

		public static void SetDataSource(this ComboBox comboBox, Type enumType)
		{
			comboBox.SetDataSource(enumType, enumType.GetValues());
		}

		public static void SetDataSource<T>(this ComboBox comboBox, IEnumerable<T> dataSource)
			where T : struct
		{
			comboBox.SetDataSource(typeof(T), dataSource.Cast<object>());
		}

		public static void SetDataSource(this ComboBox comboBox, Type enumType, IEnumerable<object> dataSource)
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

			if (dataSource == null)
				throw new ArgumentNullException(nameof(dataSource));

			var dict = _names.SafeAdd(enumType, key => enumType
				.GetValues()
				.ToDictionary(f => f, f => f.GetDisplayName()));

			GetItemsSource(comboBox).AddRange(dataSource.Select(item => new EnumerationMember { Description = dict[item], Value = item }));
		}

		public static ObservableCollection<EnumerationMember> GetItemsSource(this ComboBox comboBox)
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

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
			return (T?)comboBox.GetSelectedValue();
		}

		public static object GetSelectedValue(this ComboBox comboBox)
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

			return comboBox.SelectedItem != null ? ((EnumerationMember)comboBox.SelectedItem).Value : null;
		}

		public static void SetSelectedValue<T>(this ComboBox comboBox, T? value)
			where T : struct
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

			comboBox.SelectedValue = value;
		}
	}
}