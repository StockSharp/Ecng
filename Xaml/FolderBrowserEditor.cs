namespace Ecng.Xaml
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// <see cref="ITypeEditor"/> для <see cref="FolderBrowserPicker"/>.
	/// </summary>
	public class FolderBrowserEditor : TypeEditor<FolderBrowserPicker>
	{
		/// <summary>
		/// Установить <see cref="TypeEditor{T}.ValueProperty"/> значением <see cref="FolderBrowserPicker.FolderProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = FolderBrowserPicker.FolderProperty;
		}
	}
}