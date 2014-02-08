namespace Ecng.Xaml
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> ��� <see cref="DatabaseConnectionComboBox"/>.
	/// </summary>
	public class DatabaseConnectionEditor : TypeEditor<DatabaseConnectionComboBox>
	{
		/// <summary>
		/// ���������� <see cref="TypeEditor{T}.ValueProperty"/> ��������� <see cref="DatabaseConnectionComboBox.SelectedConnectionProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = DatabaseConnectionComboBox.SelectedConnectionProperty;
		}
	}
}