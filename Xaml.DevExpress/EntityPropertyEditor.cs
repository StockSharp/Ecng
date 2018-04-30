namespace Ecng.Xaml.DevExp
{
	using System.Windows;

	using DevExpress.Xpf.Grid.LookUp;

	using Ecng.ComponentModel;

	public class EntityPropertyEditor : LookUpEditSettings
	{
		static EntityPropertyEditor()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityPropertyEditor), new FrameworkPropertyMetadata(typeof(EntityPropertyEditor)));
		}

		public EntityPropertyEditor()
		{
			DisplayMember = nameof(EntityProperty.FullDisplayName);
			ValueMember = nameof(EntityProperty.Name);
		}
	}
}