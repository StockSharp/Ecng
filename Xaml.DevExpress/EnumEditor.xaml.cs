namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.Editors.Helpers;
	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.Grid;
	using DevExpress.Xpf.PropertyGrid;

	using Ecng.Common;
	using Ecng.ComponentModel;

	/// <summary>
	/// Localized enum editor.
	/// </summary>
	public partial class EnumEditor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EnumEditor"/>.
		/// </summary>
		public EnumEditor()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <para>
		/// Creates a new editor with the specified settings.
		/// </para>
		/// </summary>
		/// <param name="assignEditorSettings"><b>true</b> to assign specified settings to the new editor; otherwise, <b>false</b>.
		///             </param><param name="defaultViewInfo">An object implementing the <see cref="T:DevExpress.Xpf.Editors.Settings.IDefaultEditorViewInfo"/> interface.
		///             </param><param name="optimizationMode">A <see cref="T:DevExpress.Xpf.Editors.Helpers.EditorOptimizationMode"/> enumeration value.
		///             </param>
		/// <returns>
		/// An object implementing the <see cref="T:DevExpress.Xpf.Editors.IBaseEdit"/> interface.
		/// </returns>
		public override IBaseEdit CreateEditor(bool assignEditorSettings, IDefaultEditorViewInfo defaultViewInfo,
			EditorOptimizationMode optimizationMode)
		{
			Type valueType;

			switch (defaultViewInfo)
			{
				case EditorColumn column:
					valueType = column.Owner.ValueType;
					break;
				case GridColumn column:
					valueType = column.FieldType;
					break;
				default:
					return base.CreateEditor(assignEditorSettings, defaultViewInfo, optimizationMode);
			}

			var isNullable = valueType.IsNullable();
			var type = !isNullable
				? valueType
				: valueType.GetUnderlyingType();

			var editor = base.CreateEditor(assignEditorSettings, defaultViewInfo, optimizationMode);

			if (!(editor is ComboBoxEdit cbe))
				cbe = new ComboBoxEdit();

			cbe.ItemsSource = Enum.GetValues(type);
			cbe.ItemTemplate = ItemTemplate;
			cbe.ApplyItemTemplateToSelectedItem = ApplyItemTemplateToSelectedItem;
			cbe.IsTextEditable = IsTextEditable;
			cbe.AutoComplete = AutoComplete;
			cbe.ImmediatePopup = ImmediatePopup;

			if (isNullable)
				cbe.AddClearButton();

			return cbe;
		}
	}

	class EnumDisplayNameConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || value is ComboBoxEdit ? string.Empty : value.GetDisplayName();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
