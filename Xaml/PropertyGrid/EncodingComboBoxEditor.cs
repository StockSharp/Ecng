namespace Ecng.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="EncodingComboBox"/>.
	/// </summary>
	public class EncodingComboBoxEditor : TypeEditor<EncodingComboBox>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="EncodingComboBox.SelectedEncodingProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = EncodingComboBox.SelectedEncodingProperty;
		}
	}
}