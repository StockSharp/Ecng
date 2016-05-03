namespace Ecng.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> ��� <see cref="EncodingComboBox"/>.
	/// </summary>
	public class EncodingComboBoxEditor : TypeEditor<EncodingComboBox>
	{
		/// <summary>
		/// ���������� <see cref="TypeEditor{T}.ValueProperty"/> ��������� <see cref="EncodingComboBox.SelectedEncodingProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = EncodingComboBox.SelectedEncodingProperty;
		}
	}
}