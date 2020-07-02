namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Collections;
	using System.Collections.Generic;
	using System.Windows.Data;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Helpers;
	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.PropertyGrid;
	using DevExpress.Xpf.Editors.Themes;
	using DevExpress.Xpf.Editors.EditStrategy;
	using DevExpress.Xpf.Editors.Services;
	using DevExpress.Xpf.Editors.Validation.Native;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;

	/// <summary>
	/// The drop-down list to select single value.
	/// </summary>
	public class ComboBoxEditEx : ComboBoxEdit
	{
		/// <summary>
		/// Default converter that is used to convert item to tooltip.
		/// </summary>
		public static readonly IValueConverter DefaultItemTooltipConverter = new DefaultItemToTooltipConverter();

		static ComboBoxEditEx()
		{
			ComboBoxEditExSettings.RegisterCustomEdit();
			IsTextEditableProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(false));
			ImmediatePopupProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(true));
		}

		/// <summary>Current value.</summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(ComboBoxEditEx));
		/// <summary>Item tooltip converter.</summary>
		public static readonly DependencyProperty ItemTooltipConverterProperty = DependencyProperty.Register(nameof(ItemTooltipConverter), typeof(IValueConverter), typeof(ComboBoxEditEx), new PropertyMetadata(DefaultItemTooltipConverter));
		/// <summary>Is nullable.</summary>
		public static readonly DependencyProperty IsNullableProperty = DependencyProperty.Register(nameof(IsNullable), typeof(bool), typeof(ComboBoxEditEx), new PropertyMetadata(false, (o, args) => ((ComboBoxEditEx)o).OnIsNullableChanged()));
		/// <summary>Item value type.</summary>
		public static readonly DependencyProperty ItemValueTypeProperty = DependencyProperty.Register(nameof(ItemValueType), typeof(Type), typeof(ComboBoxEditEx), new PropertyMetadata(null));

		/// <summary>Current value.</summary>
		public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
		/// <summary>Item tooltip converter.</summary>
		public IValueConverter ItemTooltipConverter { get => (IValueConverter) GetValue(ItemTooltipConverterProperty); set => SetValue(ItemTooltipConverterProperty, value); }
		/// <summary>Is nullable.</summary>
		public bool IsNullable { get => (bool) GetValue(IsNullableProperty); set => SetValue(IsNullableProperty, value); }
		/// <summary>Item value type.</summary>
		public Type ItemValueType { get => (Type) GetValue(ItemValueTypeProperty); set => SetValue(ItemValueTypeProperty, value); }

		/// <summary>
		/// Get default item container style.
		/// </summary>
		protected virtual Style GetDefaultItemContainerStyle() => (Style)FindResource(new EditorListBoxThemeKeyExtension {ResourceKey = EditorListBoxThemeKeys.DefaultItemStyle });

		/// <summary>Initializes a new instance of the <see cref="ComboBoxEditEx"/>. </summary>
		public ComboBoxEditEx()
		{
			var mb = new MultiBinding
			{
				Mode = BindingMode.OneWay,
				Converter = new ToolTipConverter(),
				Bindings =
				{
					new Binding(".") { Mode = BindingMode.OneWay },
					new Binding(nameof(ItemTooltipConverter)) { Source = this, Mode = BindingMode.OneWay }
				}
			};

			// ReSharper disable once VirtualMemberCallInConstructor
			ItemContainerStyle = new Style(typeof(ComboBoxEditItem), GetDefaultItemContainerStyle()) { Setters = { new Setter(ToolTipProperty, mb) } };
		}

		private void OnIsNullableChanged()
		{
			this.RemoveClearButton();
			if(IsNullable)
				this.AddClearButton();
		}

		/// <inheritdoc />
		protected override void OnValueMemberChanged(string valueMember)                { base.OnValueMemberChanged(valueMember);      UpdateBindings(); }
		/// <inheritdoc />
		protected override void ItemsSourceChanged(object itemsSource)                  { base.ItemsSourceChanged(itemsSource);        UpdateBindings(); }
		/// <inheritdoc />
		protected override void OnEditModeChanged(EditMode oldValue, EditMode newValue) { base.OnEditModeChanged(oldValue, newValue);  UpdateBindings(); }

		/// <summary>
		/// Auto update bindings when dependency properties changed.
		/// </summary>
		protected virtual void UpdateBindings()
		{
			var pg = PropertyGridHelper.GetPropertyGrid(this);
			var itemsSource = ItemsSource;

			if (EditMode != EditMode.Standalone || itemsSource == null || pg != null)
			{
				BindingOperations.ClearBinding(this, EditValueProperty);
				return;
			}

			var itemType = (itemsSource.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)).Select(i => i.GetGenericArguments()[0])).FirstOrDefault();
			if(itemType == null)
				return;

			BindingOperations.SetBinding(this, EditValueProperty, new Binding(nameof(Value))
			{
				Source = this,
				Mode = BindingMode.TwoWay,
				Converter = new ComboBoxEditExItemConverter(this, itemType),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});

			if(itemType.IsNullable())
			{
				this.RemoveClearButton();
				this.AddClearButton();
			}
		}

		/// <inheritdoc />
		protected override BaseEditSettings CreateEditorSettings() => new ComboBoxEditExSettings();

		/// <summary>Get typed selected value.</summary>
		public T GetSelectedValue<T>() => Value is T val ? val : default;

		/// <inheritdoc />
		protected override string GetDisplayText(object editValue, bool applyFormatting) => base.GetDisplayText(TryConvertStringEnum(editValue), applyFormatting);

		protected override EditStrategyBase CreateEditStrategy() => new ComboBoxEditExStrategy(this);

		object TryConvertStringEnum(object value)
		{
			if (!(value is string str))
				return value;

			var ut = ItemValueType?.GetUnderlyingType() ?? ItemValueType;
			return ut?.IsEnum == true ? Enum.Parse(ut, str, true) : value;
		}

		class ComboBoxEditExValueContainerService : TextInputValueContainerService
		{
			public ComboBoxEditExValueContainerService(TextEditBase editor) : base(editor) { }

			public override void SetEditValue(object editValue, UpdateEditorSource updateSource)
			{
				var ivt = ((ComboBoxEditEx) OwnerEdit).ItemValueType;

				if(editValue == null || ivt == null)
				{
					base.SetEditValue(editValue, updateSource);
					return;
				}

				if (editValue is IEnumerable ie)
				{
					var arr = (IList) typeof(List<>).Make(ivt).CreateInstance();
					editValue = arr;

					foreach(var o in ie.Cast<object>())
						arr.Add(o);
				}

				base.SetEditValue(editValue, updateSource);
			}
		}

		class ComboBoxEditExStrategy : ComboBoxEditStrategy
		{
			protected override ValueContainerService CreateValueContainerService() {
				return new ComboBoxEditExValueContainerService(Editor);
			}

			public ComboBoxEditExStrategy(ComboBoxEdit editor) : base(editor) { }

			protected override void RegisterUpdateCallbacks() {
				base.RegisterUpdateCallbacks();
				PropertyUpdater.Register(BaseEdit.EditValueProperty, baseValue => baseValue, baseValue =>
				{
					var e = (ComboBoxEditEx) Editor;
					var editValue = SelectorUpdater.GetEditValueFromBaseValue(e.TryConvertStringEnum(baseValue));
					var ivt = e.ItemValueType;

					if (editValue == null || ivt == null)
						return editValue;

					if (!(editValue is IEnumerable ie))
						return editValue;

					var arr = (IList) typeof(List<>).Make(ivt).CreateInstance();

					foreach(var o in ie.Cast<object>())
						arr.Add(o);

					return arr;

				});
			}
		}

		private sealed class ComboBoxEditExItemConverter : IValueConverter
		{
			readonly ComboBoxEditEx _parent;
			readonly Type _valueType;

			public ComboBoxEditExItemConverter(ComboBoxEditEx parent, Type itemType)
			{
				if(parent == null) throw new ArgumentNullException(nameof(parent));
				if(itemType == null) throw new ArgumentNullException(nameof(itemType));

				_parent = parent;
				var valueMember = parent.ValueMember;

				if(valueMember.IsEmptyOrWhiteSpace())
				{
					_valueType = itemType;
				}
				else
				{
					var prop = itemType.GetProperty(valueMember, BindingFlags.Public | BindingFlags.Instance);
					_valueType = prop == null ? null : prop.PropertyType;
				}
			}

			// Converts property value IEnumerable<value_type> to combobox selected items IEnumerable
			object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;

			// Converts combobox selected items IEnumerable<object> to property value IEnumerable<value_type>
			object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if(_valueType == null || value == null)
					return null;

				var isConvertible = typeof(IConvertible).IsAssignableFrom(_valueType);

				var isMultiSelect = _parent is SubsetComboBox;
				if (!isMultiSelect)
					return isConvertible ? Convert.ChangeType(value, _valueType) : value;

				var items = (value as IEnumerable<object>)?.ToArray();
				if(items == null)
					return null;

				var arr = _valueType.CreateArray(items.Length);

				for (var i = 0; i < items.Length; ++i)
					arr.SetValue(isConvertible ? Convert.ChangeType(items[i], _valueType) : items[i], i);

				return arr;
			}
		}

		class ToolTipConverter : IMultiValueConverter
		{
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			{
				if(values?.Length != 2 || !(values[1] is IValueConverter conv))
					return null;

				return conv.Convert(values[0], targetType, parameter, culture);
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
		}

		class DefaultItemToTooltipConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return
					value is IItemsSourceItem item ? item.Description :
					value is Enum e ? e.GetFieldDescription() : null;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)  => throw new NotSupportedException();
		}
	}

	/// <summary>
	/// The drop-down list to select a set of fields.
	/// </summary>
	public class SubsetComboBox : ComboBoxEditEx
	{
		readonly Dictionary<object, int> _valueOrder = new Dictionary<object, int>();

		static SubsetComboBox() => SubsetComboBoxSettings.RegisterCustomEdit();

		/// <inheritdoc />
		public SubsetComboBox() => StyleSettings = new CheckedComboBoxStyleSettings();

		/// <inheritdoc />
		protected override Style GetDefaultItemContainerStyle() => (Style)FindResource(new EditorListBoxThemeKeyExtension {ResourceKey = EditorListBoxThemeKeys.CheckBoxItemStyle });

		/// <inheritdoc />
		protected override void UpdateBindings()
		{
			base.UpdateBindings();

			_valueOrder.Clear();

			if (!(ItemsSource is IList itemsList))
				return;

			for(var i = 0; i < itemsList.Count; ++i)
			{
				var val = Settings.GetValueFromItem(itemsList[i]);
				if(val != null)
					_valueOrder[val] = i;
			}
		}

		/// <inheritdoc />
		protected override string GetDisplayText(object editValue, bool applyFormatting)
		{
			if(editValue is IList itemsList)
				editValue = itemsList.Cast<object>().OrderBy(GetValueOrder).ToList();

			return base.GetDisplayText(editValue, applyFormatting);
		}

		/// <inheritdoc />
		protected override BaseEditSettings CreateEditorSettings() => new SubsetComboBoxSettings();

		/// <summary>Get typed selected values.</summary>
		public IEnumerable<T> GetSelectedValues<T>() => (Value as IEnumerable)?.Cast<T>();

		int GetValueOrder(object val) =>
			val == null ? -1 :
			_valueOrder.TryGetValue(val, out var order) ? order :
			-1;
	}

	/// <summary>
	/// Edit settings for <see cref="ComboBoxEditEx"/>.
	/// </summary>
	public class ComboBoxEditExSettings : ComboBoxEditSettings
	{
		static ComboBoxEditExSettings()
		{
			RegisterCustomEdit();
			IsTextEditableProperty.OverrideMetadata(typeof(ComboBoxEditExSettings), new FrameworkPropertyMetadata(false));
			ImmediatePopupProperty.OverrideMetadata(typeof(ComboBoxEditExSettings), new FrameworkPropertyMetadata(true));
		}

		internal static void RegisterCustomEdit() => EditorSettingsProvider.Default.RegisterUserEditor(typeof(ComboBoxEditEx), typeof(ComboBoxEditExSettings), () => new ComboBoxEditEx(), () => new ComboBoxEditExSettings());

		/// <summary>Current value.</summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(ComboBoxEditExSettings));
		/// <summary>Item tooltip converter.</summary>
		public static readonly DependencyProperty ItemTooltipConverterProperty = DependencyProperty.Register(nameof(ItemTooltipConverter), typeof(IValueConverter), typeof(ComboBoxEditExSettings), new PropertyMetadata(ComboBoxEditEx.DefaultItemTooltipConverter));
		/// <summary>Item value type.</summary>
		public static readonly DependencyProperty ItemValueTypeProperty = DependencyProperty.Register(nameof(ItemValueType), typeof(Type), typeof(ComboBoxEditExSettings), new PropertyMetadata(null));

		/// <summary>Current value.</summary>
		public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
		/// <summary>Item tooltip converter.</summary>
		public IValueConverter ItemTooltipConverter { get => (IValueConverter) GetValue(ItemTooltipConverterProperty); set => SetValue(ItemTooltipConverterProperty, value); }
		/// <summary>Item value type.</summary>
		public Type ItemValueType { get => (Type) GetValue(ItemValueTypeProperty); set => SetValue(ItemValueTypeProperty, value); }

		/// <inheritdoc />
		protected override void AssignToEditCore(IBaseEdit e)
		{
			if (e is ComboBoxEditEx editor)
			{
				SetValueFromSettings(ValueProperty, () => editor.Value = Value);
				SetValueFromSettings(ItemTooltipConverterProperty, () => editor.ItemTooltipConverter = ItemTooltipConverter);
				SetValueFromSettings(ItemValueTypeProperty, () => editor.ItemValueType = ItemValueType);
			}

			base.AssignToEditCore(e);
		}
	}

	/// <summary>
	/// Edit settings for <see cref="SubsetComboBox"/>.
	/// </summary>
	public class SubsetComboBoxSettings : ComboBoxEditExSettings
	{
		static SubsetComboBoxSettings() => RegisterCustomEdit();

		/// <inheritdoc />
		public SubsetComboBoxSettings() => StyleSettings = new CheckedComboBoxStyleSettings();

		internal new static void RegisterCustomEdit() => EditorSettingsProvider.Default.RegisterUserEditor(typeof(SubsetComboBox), typeof(SubsetComboBoxSettings), () => new SubsetComboBox(), () => new SubsetComboBoxSettings());
	}
}
