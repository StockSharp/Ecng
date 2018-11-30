namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Linq;

	using DevExpress.Xpf.Editors;
	using DevExpress.Xpf.PropertyGrid;

	using Ecng.ComponentModel;

	public abstract class BaseCustomEnumEditor<TValue> : ComboBoxEdit
		where TValue : struct
	{
		protected BaseCustomEnumEditor(params TValue[] values)
		{
			ItemsSource = values.Select(v => Tuple.Create(v.GetDisplayName(), (TValue?)v)).ToArray();
			
			DisplayMember = nameof(Tuple<string, TValue?>.Item1);
			ValueMember = nameof(Tuple<string, TValue?>.Item2);

			Name = "PART_Editor";
			EditMode = EditMode.InplaceActive;
			NavigationManager.SetNavigationMode(this, NavigationMode.Auto);

			this.AddClearButton();
		}
	}
}