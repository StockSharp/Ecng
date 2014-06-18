namespace Ecng.Xaml.Database
{
	using System.Windows.Controls.Primitives;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> ��� <see cref="DatabaseProviderComboBox"/>.
	/// </summary>
	public class DatabaseProviderEditor : TypeEditor<DatabaseProviderComboBox>
	{
		/// <summary>
		/// ���������� <see cref="TypeEditor{T}.ValueProperty"/> ��������� <see cref="Selector.SelectedItemProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = Selector.SelectedItemProperty;
		}
	}
}