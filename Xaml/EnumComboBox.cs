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
			DependencyProperty.Register(nameof(EnumType), typeof(Type), typeof(EnumComboBox), new PropertyMetadata(EnumTypePropertyChanged));

		private static void EnumTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is EnumComboBox ctrl))
				return;

			ctrl.SetDataSource((Type)e.NewValue);
		}

		public Type EnumType
		{
			get => (Type)GetValue(EnumTypeProperty);
			set => SetValue(EnumTypeProperty, value);
		}
	}

	public static class EnumComboBoxHelper
	{
		public class EnumerationMember : NotifiableObject
		{
			private string _description;

			public string Description
			{
				get => _description;
				set
				{
					_description = value;
					NotifyChanged(nameof(Description));
				}
			}

			private object _value;

			public object Value
			{
				get => _value;
				set
				{
					_value = value;
					NotifyChanged(nameof(Value));
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

		public static void SetDataSource<T>(this ComboBox comboBox, bool addNullable = false)
			where T : struct
		{
			comboBox.SetDataSource(typeof(T), addNullable);
		}

		public static void SetDataSource(this ComboBox comboBox, Type enumType, bool addNullable = false)
		{
			comboBox.SetDataSource(enumType, enumType.GetValues(), addNullable);
		}

		public static void SetDataSource<T>(this ComboBox comboBox, IEnumerable<T> dataSource, bool addNullable = false)
			where T : struct
		{
			comboBox.SetDataSource(typeof(T), dataSource.Cast<object>(), addNullable);
		}

		public static void SetDataSource(this ComboBox comboBox, Type enumType, IEnumerable<object> dataSource, bool addNullable = false)
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

			if (dataSource == null)
				throw new ArgumentNullException(nameof(dataSource));

			var dict = _names.SafeAdd(enumType, key => enumType
				.GetValues()
				.ToDictionary(f => f, f => f.GetDisplayName()));

			var members = dataSource.Select(item => new EnumerationMember
			{
				Description = dict[item],
				Value = item
			}).ToList();

			if (addNullable)
				members.Insert(0, new EnumerationMember());

			GetItemsSource(comboBox).AddRange(members);
		}

		public static ObservableCollection<EnumerationMember> GetItemsSource(this ComboBox comboBox)
		{
			if (comboBox == null)
				throw new ArgumentNullException(nameof(comboBox));

			return _sources.SafeAdd(comboBox, key =>
			{
				comboBox.DisplayMemberPath = nameof(EnumerationMember.Description);
				comboBox.SelectedValuePath = nameof(EnumerationMember.Value);

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

			return ((EnumerationMember)comboBox.SelectedItem)?.Value;
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