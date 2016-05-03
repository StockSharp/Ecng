namespace Ecng.Xaml.PropertyGrid
{
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	public class TimeZoneEditor : TypeEditor<TimeZoneComboBox>
	{
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = TimeZoneComboBox.SelectedTimeZoneProperty;
		}
	}
}