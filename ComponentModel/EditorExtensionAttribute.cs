namespace Ecng.ComponentModel
{
	using System;

	/// <summary>
	/// Specifies editor extension options for enums and properties.
	/// </summary>
	[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Property)]
	public class EditorExtensionAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets a value indicating whether auto-complete is enabled.
		/// </summary>
		public bool AutoComplete { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the items should be sorted.
		/// </summary>
		public bool Sorted { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether obsolete items should be included.
		/// </summary>
		public bool IncludeObsolete { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the count of selected items should be displayed.
		/// </summary>
		public bool ShowSelectedItemsCount { get; set; }
	}
}