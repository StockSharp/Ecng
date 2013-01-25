namespace Ecng.ComponentModel
{
	using System;

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TItem"></typeparam>
	/// <typeparam name="TBound"></typeparam>
	public class CompressedDataRange<TItem, TBound> : Range<TBound>
		where TBound : IComparable<TBound>
	{
		/// <summary>
		/// Gets or sets the item.
		/// </summary>
		/// <value>The item.</value>
		public TItem Item { get; set; }
	}
}