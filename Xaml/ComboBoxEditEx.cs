namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.ComponentModel;
	using System.Collections;
	using System.Collections.Generic;
	using System.Windows.Data;
	using System.Globalization;
	using System.Linq;

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
	using Ecng.Localization;

	/// <summary>
	/// The drop-down list to select single value.
	/// </summary>
	public class ComboBoxEditEx : ComboBoxEdit
	{
		static ComboBoxEditEx()
		{
			IsTextEditableProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(false));
			ImmediatePopupProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(true));
			DisplayMemberProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(nameof(IItemsSourceItem.DisplayName)));
			ValueMemberProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(nameof(IItemsSourceItem.Value)));
			ItemsSourceProperty.OverrideMetadata(typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(null, null, (o, value) => ((ComboBoxEditEx)o).CoerceItemsSource(value)));

			ComboBoxEditExSettings.RegisterCustomEdit();
		}

		private static readonly DependencyPropertyKey SourceKey = DependencyProperty.RegisterReadOnly(nameof(Source), typeof(IItemsSource), typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(null));

		/// <summary>Current value.</summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(ComboBoxEditEx),
					new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
							(o, args) => ((ComboBoxEditEx)o).GetBindingExpression(ValueProperty)?.UpdateSource(),
							(o, val)  => ((ComboBoxEditEx)o).CoerceValueProperty(val),
							false, UpdateSourceTrigger.Explicit));

		/// <summary>Is nullable.</summary>
		public static readonly DependencyProperty IsNullableProperty = DependencyProperty.Register(nameof(IsNullable), typeof(bool), typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(false, (o, args) => ((ComboBoxEditEx)o).OnIsNullableChanged()));
		/// <summary>Show obsolete.</summary>
		public static readonly DependencyProperty ShowObsoleteProperty = DependencyProperty.Register(nameof(ShowObsolete), typeof(bool), typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(false, (o, args) => ((ComboBoxEditEx)o).OnShowObsoleteChanged()));
		/// <summary>Sort order.</summary>
		public static readonly DependencyProperty SortOrderProperty = DependencyProperty.Register(nameof(SortOrder), typeof(ListSortDirection?), typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(null, (o, args) => ((ComboBoxEditEx)o).OnSortOrderChanged()));
		/// <summary>Is searchable.</summary>
		public static readonly DependencyProperty IsSearchableProperty = DependencyProperty.Register(nameof(IsSearchable), typeof(bool), typeof(ComboBoxEditEx), new FrameworkPropertyMetadata(false, (o, args) => ((ComboBoxEditEx)o).OnIsSearchableChanged()));

		/// <summary>Current value.</summary>
		public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
		/// <summary>Is nullable.</summary>
		public bool IsNullable { get => (bool) GetValue(IsNullableProperty); set => SetValue(IsNullableProperty, value); }
		/// <summary>Current <see cref="IItemsSource"/>.</summary>
		public IItemsSource Source { get => (IItemsSource)GetValue(SourceKey.DependencyProperty); private set => SetValue(SourceKey, value); }
		/// <summary>Show obsolete.</summary>
		public bool ShowObsolete { get => (bool) GetValue(ShowObsoleteProperty); set => SetValue(ShowObsoleteProperty, value); }
		/// <summary>Sort order.</summary>
		public ListSortDirection? SortOrder { get => (ListSortDirection?) GetValue(SortOrderProperty); set => SetValue(SortOrderProperty, value); }
		/// <summary>Is searchable.</summary>
		public bool IsSearchable { get => (bool) GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }

		/// <summary>
		/// Get default item container style.
		/// </summary>
		protected virtual Style GetDefaultItemContainerStyle() => (Style)FindResource(new EditorListBoxThemeKeyExtension {ResourceKey = EditorListBoxThemeKeys.DefaultItemStyle });

		/// <summary>Initializes a new instance of the <see cref="ComboBoxEditEx"/>. </summary>
		public ComboBoxEditEx()
		{
			var mb = new Binding
			{
				Mode = BindingMode.OneWay,
				Converter = new ItemToTooltipConverter(),
				Path = new PropertyPath(".")
			};

			// ReSharper disable once VirtualMemberCallInConstructor
			ItemContainerStyle = new Style(typeof(ComboBoxEditItem), GetDefaultItemContainerStyle()) { Setters = { new Setter(ToolTipProperty, mb) } };

			DisplayMember = nameof(IItemsSourceItem.DisplayName);
			ValueMember = nameof(IItemsSourceItem.Value);
		}

		internal static IItemsSource CoerceItemsSource(object newValue, bool? showObsolete, ListSortDirection? sortOrder)
			=> newValue.ToItemsSource(null, !showObsolete, sortOrder);

		private object CoerceItemsSource(object newValue)
		{
			var isSet = ReadLocalValue(ShowObsoleteProperty) != DependencyProperty.UnsetValue;

			var src = CoerceItemsSource(newValue, isSet ? !ShowObsolete : (bool?) null, SortOrder);

			Source = src;

			return src.Values;
		}

		private object CoerceValueProperty(object newVal)
		{
			if(newVal != null)
				return newVal;

			var src = Source;
			var canBeNull = src == null || IsNullable || EditValue == null;

			return canBeNull ? null : DependencyProperty.UnsetValue;
		}

		private void OnShowObsoleteChanged()  => SetCurrentValue(ItemsSourceProperty, Source);
		private void OnSortOrderChanged()     => SetCurrentValue(ItemsSourceProperty, Source);

		private void OnIsNullableChanged()
		{
			this.RemoveClearButton();
			if(IsNullable)
				this.AddClearButton();
		}

		private void OnIsSearchableChanged()
		{
			var searchable = IsSearchable;
			SetCurrentValue(AutoCompleteProperty, searchable);
			SetCurrentValue(IncrementalFilteringProperty, searchable);
			SetCurrentValue(IncrementalSearchProperty, searchable);
			SetCurrentValue(FilterConditionProperty, DevExpress.Data.Filtering.FilterCondition.StartsWith);
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

			BindingOperations.SetBinding(this, EditValueProperty, new Binding(nameof(Value))
			{
				Source = this,
				Mode = BindingMode.TwoWay,
				Converter = new ComboBoxEditExItemConverter(this),
				UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
			});
		}

		/// <inheritdoc />
		protected override BaseEditSettings CreateEditorSettings() => new ComboBoxEditExSettings();

		/// <inheritdoc />
		protected override string GetDisplayText(object editValue, bool applyFormatting) => base.GetDisplayText(TryConvertStringEnum(editValue), applyFormatting);

		protected override EditStrategyBase CreateEditStrategy() => new ComboBoxEditExStrategy(this);

		object TryConvertStringEnum(object value)
		{
			if (!(value is string str))
				return value;

			var itemValueType = Source?.ValueType;
			var ut = itemValueType?.GetUnderlyingType() ?? itemValueType;
			return ut?.IsEnum == true ? str.To(ut) : value;
		}

		protected virtual object TryConvertEditValue(object ev) => ev;

		class ComboBoxEditExValueContainerService : TextInputValueContainerService
		{
			public ComboBoxEditExValueContainerService(TextEditBase editor) : base(editor) { }

			public override void SetEditValue(object editValue, UpdateEditorSource updateSource) => base.SetEditValue(((ComboBoxEditEx) OwnerEdit).TryConvertEditValue(editValue) ?? editValue, updateSource);
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

					return e.TryConvertEditValue(editValue) ?? editValue;
				});
			}
		}

		private sealed class ComboBoxEditExItemConverter : IValueConverter
		{
			private readonly Type _enumerableType;
			private readonly ComboBoxEditEx _parent;
			private readonly Type _valueType;

			public ComboBoxEditExItemConverter(ComboBoxEditEx parent)
			{
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				_valueType = _parent.Source?.ValueType;

				if(_valueType != null)
					_enumerableType = typeof(IEnumerable<>).Make(_valueType);
			}

			// Converts property value IEnumerable<value_type> to combobox selected items IEnumerable (Value ====> EditValue)
			object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;

			// Converts combobox selected items IEnumerable<object> to property value IEnumerable<value_type>  (EditValue ====> Value)
			object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if(_valueType == null)
					return null;

				var isMultiSelect = _parent is SubsetComboBox;

				if(value == null)
					return isMultiSelect ? _valueType.CreateArray(0) : null;

				if (!isMultiSelect)
					return value.To(_valueType);

				if(_enumerableType.IsInstanceOfType(value))
					return value;

				var items = (value as IEnumerable<object>)?.ToArray();
				if(items == null)
					return null;

				var arr = _valueType.CreateArray(items.Length);

				for (var i = 0; i < items.Length; ++i)
					arr.SetValue(items[i].To(_valueType), i);

				return arr;
			}
		}

		class ItemToTooltipConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
				=> value is IItemsSourceItem item ? item.Description : null;

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
		}
	}

	/// <summary>
	/// The drop-down list to select a set of fields.
	/// </summary>
	public class SubsetComboBox : ComboBoxEditEx
	{
		/// <summary>Display selected items count.</summary>
		public static readonly DependencyProperty DisplaySelectedItemsCountProperty = DependencyProperty.Register(nameof(DisplaySelectedItemsCount), typeof(bool), typeof(SubsetComboBox), new FrameworkPropertyMetadata(false));

		/// <summary>Display selected items count.</summary>
		public bool DisplaySelectedItemsCount { get => (bool)GetValue(DisplaySelectedItemsCountProperty); set => SetValue(DisplaySelectedItemsCountProperty, value); }

		private readonly Dictionary<object, int> _valueOrder = new Dictionary<object, int>();

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

		protected override object CoerceEditValue(DependencyObject d, object value)
		{
			var ivt = Source?.ValueType;
			if(value == null && ivt != null)
				value = typeof(List<>).Make(ivt).CreateInstance();

			return base.CoerceEditValue(d, value);
		}

		protected override object TryConvertEditValue(object ev)
		{
			var ivt = Source?.ValueType;

			if (ivt == null)
				return null;

			var list = (IList) typeof(List<>).Make(ivt).CreateInstance();

			if(!(ev is IEnumerable ie))
				return list;

			var arr = ie.Cast<object>().ToArray();

			if(arr.All(o => ivt.IsInstanceOfType(o)))
			{
				foreach (var o in arr)
					list.Add(o);

				return list;
			}

			if(arr.All(o => o is IItemsSourceItem))
			{
				foreach (var o in arr)
					list.Add(((IItemsSourceItem)o).Value);

				return list;
			}

			return list;
		}

		/// <inheritdoc />
		protected override string GetDisplayText(object editValue, bool applyFormatting)
		{
			if(editValue is IList itemsList)
			{
				if(DisplaySelectedItemsCount && itemsList.Count != 1)
					return "Selected: {0}".Translate().Put(itemsList.Count);

				editValue = itemsList.Cast<object>().OrderBy(GetValueOrder).ToList();
			}

			return base.GetDisplayText(editValue, applyFormatting);
		}

		/// <inheritdoc />
		protected override BaseEditSettings CreateEditorSettings() => new SubsetComboBoxSettings();

		private int GetValueOrder(object val) =>
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
			IsTextEditableProperty.OverrideMetadata(typeof(ComboBoxEditExSettings), new FrameworkPropertyMetadata(false));
			ImmediatePopupProperty.OverrideMetadata(typeof(ComboBoxEditExSettings), new FrameworkPropertyMetadata(true));
			ItemsSourceProperty.OverrideMetadata(typeof(ComboBoxEditExSettings), new FrameworkPropertyMetadata(null, null, (o, value) => ((ComboBoxEditExSettings)o).CoerceItemsSource(value)));

			RegisterCustomEdit();
		}

		public ComboBoxEditExSettings()
		{
			DisplayMember = nameof(IItemsSourceItem.DisplayName);
			ValueMember = nameof(IItemsSourceItem.Value);
		}

		private object CoerceItemsSource(object newValue)
		{
			return ComboBoxEditEx.CoerceItemsSource(newValue, null, null).Values;
		}

		internal static void RegisterCustomEdit() => EditorSettingsProvider.Default.RegisterUserEditor(typeof(ComboBoxEditEx), typeof(ComboBoxEditExSettings), () => new ComboBoxEditEx(), () => new ComboBoxEditExSettings());

		/// <summary>Current value.</summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(object), typeof(ComboBoxEditExSettings));
		/// <summary>Is searchable.</summary>
		public static readonly DependencyProperty IsSearchableProperty = DependencyProperty.Register(nameof(IsSearchable), typeof(bool), typeof(ComboBoxEditExSettings), new FrameworkPropertyMetadata(false));

		/// <summary>Current value.</summary>
		public object Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
		/// <summary>Is searchable.</summary>
		public bool IsSearchable { get => (bool) GetValue(IsSearchableProperty); set => SetValue(IsSearchableProperty, value); }


		/// <inheritdoc />
		protected override void AssignToEditCore(IBaseEdit e)
		{
			if (e is ComboBoxEditEx editor)
			{
				SetValueFromSettings(ValueProperty, () => editor.Value = Value);
				SetValueFromSettings(IsSearchableProperty, () => editor.IsSearchable = IsSearchable);
			}

			base.AssignToEditCore(e);
		}
	}

	/// <summary>
	/// Edit settings for <see cref="SubsetComboBox"/>.
	/// </summary>
	public class SubsetComboBoxSettings : ComboBoxEditExSettings
	{
		/// <summary>Display selected items count.</summary>
		public static readonly DependencyProperty DisplaySelectedItemsCountProperty = DependencyProperty.Register(nameof(DisplaySelectedItemsCount), typeof(bool), typeof(SubsetComboBoxSettings), new FrameworkPropertyMetadata(false));

		/// <summary>Display selected items count.</summary>
		public bool DisplaySelectedItemsCount { get => (bool)GetValue(DisplaySelectedItemsCountProperty); set => SetValue(DisplaySelectedItemsCountProperty, value); }

		static SubsetComboBoxSettings() => RegisterCustomEdit();

		/// <inheritdoc />
		public SubsetComboBoxSettings() => StyleSettings = new CheckedComboBoxStyleSettings();

		internal new static void RegisterCustomEdit() => EditorSettingsProvider.Default.RegisterUserEditor(typeof(SubsetComboBox), typeof(SubsetComboBoxSettings), () => new SubsetComboBox(), () => new SubsetComboBoxSettings());

		/// <inheritdoc />
		protected override void AssignToEditCore(IBaseEdit e)
		{
			if (e is SubsetComboBox editor)
				SetValueFromSettings(DisplaySelectedItemsCountProperty, () => editor.DisplaySelectedItemsCount = DisplaySelectedItemsCount);

			base.AssignToEditCore(e);
		}
	}
}
