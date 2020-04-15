namespace Ecng.Backup.Amazon.Xaml
{
	using System.Collections.Generic;
	
	using DevExpress.Xpf.Editors.Settings;

	using Ecng.ComponentModel;

	using global::Amazon;

	/// <summary>
	/// The drop-down list to select the AWS region.
	/// </summary>
	public class AmazonRegionEditor : ComboBoxEditSettings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonRegionEditor"/>.
		/// </summary>
		public AmazonRegionEditor()
		{
			var cb = new AmazonRegionComboBox();
			var items = new List<ItemsSourceItem>();

			foreach (RegionEndpoint item in cb.ItemsSource)
			{
				items.Add(new ItemsSourceItem(item.DisplayName, item));
			}

			ItemsSource = items.ToArray();
		}
	}
}