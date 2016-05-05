namespace Ecng.Xaml.DevExp
{
	using System.Windows;

	using DevExpress.Xpf.Editors.Settings;
	using DevExpress.Xpf.Grid.LookUp;

	public class EntityPropertyEditor : LookUpEditSettings
	{
		static EntityPropertyEditor()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityPropertyEditor), new FrameworkPropertyMetadata(typeof(EntityPropertyEditor)));
		}

		public EntityPropertyEditor()
		{
			DisplayMember = "FullDisplayName";
			ValueMember = "Name";
		}
	}
}