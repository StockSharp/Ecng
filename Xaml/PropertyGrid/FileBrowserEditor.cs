namespace Ecng.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> ��� <see cref="FileBrowserPicker"/>.
	/// </summary>
	public class FileBrowserEditor : TypeEditor<FileBrowserPicker>
	{
		/// <summary>
		/// ���������� <see cref="TypeEditor{T}.ValueProperty"/> ��������� <see cref="FileBrowserPicker.FileProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = FileBrowserPicker.FileProperty;
		}
	}
}