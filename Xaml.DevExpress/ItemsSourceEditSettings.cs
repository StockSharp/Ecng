namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	using DevExpress.Xpf.Editors.Settings;

	using Ecng.ComponentModel;

	/// <summary>
	/// Contains settings specific to a combobox editor with uses <see cref="IItemsSource"/>.
	/// </summary>
	public class ItemsSourceEditSettings : ComboBoxEditSettings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ItemsSourceEditSettings"/>.
		/// </summary>
		public ItemsSourceEditSettings()
		{
			DisplayMember = "Item1";
			ValueMember = "Item2";
			ItemsSource = ComboBoxItems;
		}

		/// <summary>
		/// Items.
		/// </summary>
		public IList<Tuple<string, object>> ComboBoxItems { get; } = new ObservableCollection<Tuple<string, object>>();
	}
}