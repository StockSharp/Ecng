#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Xaml.Algo
File: AmazonRegionEditor.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

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

		//FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		//{
		//	var comboBox = new AmazonRegionComboBox { IsEditable = false, Width = double.NaN };

		//	var binding = new Binding("Value")
		//	{
		//		Source = propertyItem,
		//		//Converter = new EndPointValueConverter(comboBox, SmartComAddresses.Matrix),
		//		Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
		//	};

		//	BindingOperations.SetBinding(comboBox, ComboBox.TextProperty, binding);
		//	return comboBox;
		//}
	}
}