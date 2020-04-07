namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	#endregion

	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TItem"></typeparam>
	/// <typeparam name="TBound"></typeparam>
	public class CompressedData<TItem, TBound>
		where TItem : struct, IEquatable<TItem>
		where TBound : struct, IEquatable<TBound>, IComparable<TBound>
	{
		#region CompressedData.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressedData{TItem, TBound}"/> class.
		/// </summary>
		public CompressedData()
		{
			SecondaryItems = new Dictionary<TBound, List<CompressedDataRange<TItem, TBound>>>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressedData{TItem, TBound}"/> class.
		/// </summary>
		/// <param name="items">The items.</param>
		public CompressedData(TItem[][] items)
			: this()
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			if (items.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(items));

			int xCount = items.Length;
			int yCount = items[0].Length;

			var itemTypeCount = new PairSet<TItem, int>();

			int maxCount = 0;

			for (int y = 0; y < yCount; y++)
			{
				for (int x = 0; x < xCount; x++)
				{
					TItem item = items[x][y];

					int count;

					if (!itemTypeCount.ContainsKey(item))
						count = 1;
					else
					{
						count = itemTypeCount.GetValue(item);
						count++;
					}

					itemTypeCount.SetValue(item, count);

					if (maxCount < count)
						maxCount = count;
				}
			}

			if (itemTypeCount.IsEmpty())
				throw new ArgumentException("tiles");

			if (itemTypeCount.Count > 1)
			{
				PrimaryItem = itemTypeCount.GetKey(maxCount);

				for (int y = 0; y < yCount; y++)
				{
					var ranges = new List<CompressedDataRange<TItem, TBound>>();

					CompressedDataRange<TItem, TBound> range = null;

					for (int x = 0; x < xCount; x++)
					{
						TItem item = items[x][y];

						if (!PrimaryItem.Equals(item))
						{
							if (range == null)
							{
								range = new CompressedDataRange<TItem, TBound> { Item = item, Min = x.To<TBound>() };
							}
							else
							{
								if (!range.Item.Equals(item))
								{
									range.Max = (x - 1).To<TBound>();
									ranges.Add(range);

									range = new CompressedDataRange<TItem, TBound> { Item = item, Min = x.To<TBound>() };
								}
								else
									range.Max = x.To<TBound>();
							}
						}
						else
						{
							if (range != null)
							{
								range.Max = (x - 1).To<TBound>();
								ranges.Add(range);
								range = null;
							}
						}
					}

					if (range != null)
					{
						if (!range.HasMaxValue)
							range.Max = (xCount - 1).To<TBound>();

						ranges.Add(range);
					}

					if (!ranges.IsEmpty())
						SecondaryItems.Add(y.To<TBound>(), ranges);
				}
			}
			else
				PrimaryItem = itemTypeCount.Keys.ElementAt(0);
		}

		#endregion

		/// <summary>
		/// Gets or sets the primary item.
		/// </summary>
		/// <value>The primary item.</value>
		public TItem PrimaryItem { get; set; }

		/// <summary>
		/// Gets the secondary items.
		/// </summary>
		/// <value>The secondary items.</value>
		public IDictionary<TBound, List<CompressedDataRange<TItem, TBound>>> SecondaryItems { get; }

		#region Item

		/// <summary>
		/// Gets the item with the specified x.
		/// </summary>
		/// <value>The item.</value>
		public TItem this[TBound x, TBound y]
		{
			get
			{
				//if (x < 0/* || x >= Constants.XCount*/)
				//	throw new ArgumentOutOfRangeException("x");

				//if (y < 0/* || y >= Constants.YCount*/)
				//	throw new ArgumentOutOfRangeException("y");

				if (!SecondaryItems.IsEmpty())
				{
					if (SecondaryItems.TryGetValue(y, out var ranges))
					{
						foreach (var range in ranges)
						{
							if (range.Contains(x))
								return range.Item;
						}
					}
				}

				return PrimaryItem;
			}
		}

		#endregion

		private Stream _stream;

		///<summary>
		///</summary>
		///<exception cref="ArgumentNullException"></exception>
		public Stream Stream
		{
			get
			{
				if (_stream == null)
				{
					_stream = new MemoryStream();

					// write primary item
					_stream.WriteEx(PrimaryItem);

					var count = SecondaryItems.Count;

					// write 'true' that mean data has other items
					_stream.WriteEx(count > 0);

					if (count > 0)
					{
						// write secondary items count
						_stream.WriteEx(count);

						foreach (var item in SecondaryItems)
						{
							// write y position
							_stream.WriteEx(item.Key);

							// write secondary item group count
							_stream.WriteEx(item.Value.Count);

							foreach (var range in item.Value)
							{
								// write item
								_stream.WriteEx(range.Item);

								// write begin x position
								_stream.WriteEx(range.Min);

								// write end x position
								_stream.WriteEx(range.Max);
							}
						}
					}
				}

				return _stream;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

                _stream = value;

				PrimaryItem = value.Read<TItem>();

				if (value.Read<bool>())
				{
					var length = value.Read<int>();

					for (int i = 0; i < length; i++)
					{
						var y = value.Read<TBound>();

						var rangeLength = value.Read<int>();
						var rangeList = new List<CompressedDataRange<TItem, TBound>>(rangeLength);

						for (int k = 0; k < rangeLength; k++)
						{
							var item = value.Read<TItem>();
							var minX = value.Read<TBound>();
							var maxX = value.Read<TBound>();

							rangeList.Add(new CompressedDataRange<TItem, TBound> { Item = item, Min = minX, Max = maxX });
						}

						SecondaryItems.Add(y, rangeList);
					}
				}
			}
		}
	}
}