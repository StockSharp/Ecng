namespace Ecng.Xaml.PropertyGrid
{
	using System.Windows.Controls.Primitives;

	using Ecng.Xaml.Database;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="DatabaseProviderComboBox"/>.
	/// </summary>
	public class DatabaseProviderEditor : TypeEditor<DatabaseProviderComboBox>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="Selector.SelectedItemProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = Selector.SelectedItemProperty;
		}
	}
}