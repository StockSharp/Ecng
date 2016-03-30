#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Xaml.Algo
File: AmazonRegionComboBox.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace Ecng.Backup.Amazon.Xaml
{
	using System.Windows.Controls;

	using global::Amazon;

	/// <summary>
	/// The drop-down list to select the AWS region.
	/// </summary>
	public class AmazonRegionComboBox : ComboBox
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonRegionComboBox"/>.
		/// </summary>
		public AmazonRegionComboBox()
		{
			DisplayMemberPath = nameof(RegionEndpoint.DisplayName);
			ItemsSource = AmazonExtensions.Endpoints;
		}
	}
}