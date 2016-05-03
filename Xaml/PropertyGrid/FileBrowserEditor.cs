namespace Ecng.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="FileBrowserPicker"/>.
	/// </summary>
	public class FileBrowserEditor : TypeEditor<FileBrowserPicker>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="FileBrowserPicker.FileProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = FileBrowserPicker.FileProperty;
		}
	}
}