// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// EnumerableExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Color = System.Windows.Media.Color;

namespace Ecng.Xaml.Charting.Common.Extensions
{
	public static class EnumerableExtensions
	{        
		internal static UIElement SingleOrDefault(this UIElementCollection collection, Predicate<UIElement> predicate)
		{
			foreach(var element in collection)
			{
				if (predicate((UIElement)element))
					return (UIElement)element;
			}

			return null;
		}

		internal static DoubleRange GetRange(this IList<double> list)
		{
			var innerArray = list is FifoSeriesColumn<double> ? ((FifoSeriesColumn<double>)list).ToUnorderedUncheckedList() : list.ToUncheckedList();
			double min, max;
			ArrayOperations.MinMax(innerArray, out min, out max);
			return new DoubleRange(min, max);
		}

		internal static double[] ToDoubleArray<T>(this IList<T> list)
		{
			var result = list.ToUncheckedList() as double[];
			return result;

			throw new NotImplementedException();
		}

		internal static UncheckedList<T> ToUncheckedList<T>(this IList<T> list, IndexRange indexRange, bool allowCopy)
		{
			int count = indexRange.Max - indexRange.Min + 1;

			var uncheckedList = list as UncheckedList<T>;
			if (uncheckedList != null)
			{
				return new UncheckedList<T>(uncheckedList.Array, indexRange.Min + uncheckedList.BaseIndex, count);
			}

			var seriesColumn = list as BaseSeriesColumn<T>;
			if (seriesColumn != null) return seriesColumn.ToUncheckedList(indexRange.Min, count);

			var array = list as T[];
			if (array != null) return new UncheckedList<T>(array, indexRange.Min, count);

#if !SILVERLIGHT
			if (list is List<T>)
				return new UncheckedList<T>((T[])typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(list), indexRange.Min, count);
#endif


			if (allowCopy)
			{
				return new UncheckedList<T>(list.ToArray(), indexRange.Min, count);
			}

			return null;
		}

		internal static UncheckedList<T> ToUncheckedList<T>(this IList<T> list, bool allowCopy)
		{
			var uncheckedList = list as UncheckedList<T>;
			if (uncheckedList != null)
			{
				return uncheckedList;
			}

			var seriesColumn = list as BaseSeriesColumn<T>;
			if (seriesColumn != null)
				return seriesColumn.ToUncheckedList(0, seriesColumn.Count);

			var array = list as T[];
			if (array != null) return new UncheckedList<T>(array);

#if !SILVERLIGHT
			if (list is List<T>)
				return new UncheckedList<T>((T[])typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(list));
#endif

			// 

			if (allowCopy)
			{
				return new UncheckedList<T>(list.ToArray());
			}

			return null;
		}


		/// <returns>warning: returned array may contain padding zeros in end</returns>
		internal static T[] ToUncheckedList<T>(this IList<T> list)
		{
			var seriesColumn = list as SeriesColumn<T>;
			if (seriesColumn != null) return seriesColumn.UncheckedArray();

			var array = list as T[];
			if (array != null) return array;

			var sciList = list as UltraList<T>;
			if (sciList != null) return sciList.ItemsArray;

#if !SILVERLIGHT
			if (list is List<T>)
				return (T[])typeof(List<T>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(list);
#endif

			// Optimized to reduce new/copy operations on FIFO buffers
			var fifoBuffer = list as FifoSeriesColumn<T>;
			if (fifoBuffer != null)
				return fifoBuffer.ToArray();

			// Cannot perform conversion, just return the list itself
			return list.ToArray();
		}

		internal static bool IsNullOrEmptyList(this IList enumerable)
		{
			return enumerable == null || enumerable.Count == 0;
		}

		internal static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
		{
			return enumerable == null || !enumerable.Any();
		}

		internal static bool IsEmpty<T>(this IEnumerable<T> enumerable)
		{
			return !enumerable.Any();
		}

		internal static void ForEachDo<T>(this IEnumerable enumerable, Action<T> operation)
		{
			if (enumerable == null)
				return;

			ForEachDo<T>((IEnumerable<T>)enumerable.OfType<IRenderableSeries>(), operation);
		}

		internal static void ForEachDo<T>(this IEnumerable<T> enumerable, Action<T> operation)
		{
			if (enumerable == null)
				return;

			Guard.NotNull(operation, "operation");

			foreach(var value in enumerable)
			{
				operation(value);
			}
		}

		internal static void RemoveWhere<T>(this IList<T> collection, Predicate<T> predicate)
		{
			for(int i = 0; i < collection.Count; i++)
			{
				if (predicate(collection[i]))
				{
					collection.RemoveAt(i--);
				}
			}
		}

		internal static void AddIfNotContains<T>(this IList<T> collection, T item)
		{
			if (!collection.Contains(item))
			{
				collection.Add(item);
			}
		}

		/// <summary>
        /// Finds the index of the item in the List according to the desired <see cref="SearchMode"/>. 
        /// If <paramref name="isSorted"/> is true, uses fast binary search
		/// </summary>
		/// <typeparam name="T">The type of the list</typeparam>
		/// <param name="list">The list to search</param>
		/// <param name="isSorted">If true, will use fast binary search</param>
		/// <param name="value">The value to find the index for</param>
        /// <param name="searchMode">The <see cref="SearchMode"/> options</param>
		/// <returns>The found index, or -1 if not found</returns>
        public static int FindIndex<T>(this IList<T> list, bool isSorted, IComparable value, SearchMode searchMode)
            where T:IComparable
		{
			return ((IList)list).FindIndex(isSorted, value, searchMode);
		}

        /// <summary>
        /// Finds the index of the item in the List according to the desired <see cref="SearchMode"/>. 
        /// If <paramref name="isSorted"/> is true, uses fast binary search
        /// </summary>
        /// <param name="list">The list to search</param>
        /// <param name="isSorted">If true, will use fast binary search</param>
        /// <param name="value">The value to find the index for</param>
        /// <param name="searchMode">The <see cref="SearchMode"/> options</param>
        /// <returns>The found index, or -1 if not found</returns>
		public static int FindIndex(this IList list, bool isSorted, IComparable value, SearchMode searchMode)
		{
			if (ReferenceEquals(null, list))
				throw new ArgumentNullException("list");
			if (ReferenceEquals(null, value))
				throw new ArgumentNullException("value");

			var comparer = Comparer<IComparable>.Default;

			var index = -1;
			// Return sorted search (Binary search)
			if (isSorted)
			{
				index = FindIndexInSortedData(list, value, comparer, searchMode);
			}
			else
			{
				// The only allowed mode for unsorted data is SearchMode.Exact
				if (searchMode == SearchMode.Exact)
				{
					index = list.IndexOf(value);
				}
				else
					throw new NotImplementedException(
						String.Format(
							"Unsorted data occurs in the collection. The only allowed SearchMode is {0} when FindIndex() is called on an unsorted collection, but {1} was passed in.",
							SearchMode.Exact, searchMode));
			}

			return index;
		}      

		private static int FindIndexInSortedData(this IList list, IComparable value, IComparer<IComparable> comparer, SearchMode searchMode)
		{
			// Basic sanity checks
			if (list.Count == 0)
				return -1;

			if (comparer.Compare(value, (IComparable)list[0]) < 0)
				return searchMode == SearchMode.Exact ? -1 : 0;
			if (comparer.Compare(value, (IComparable)list[0]) == 0)
				return 0;
			if (comparer.Compare(value, (IComparable)list[list.Count - 1]) == 0)
				return list.Count - 1;
			if (comparer.Compare(value, (IComparable)list[list.Count - 1]) > 0)
				return searchMode == SearchMode.Exact ? -1 : list.Count - 1;

			int lower = 0;
			int upper = list.Count - 1;
			int middle = -1;
			while (lower <= upper)
			{
				middle = (lower + upper) / 2;
				int comparisonResult = comparer.Compare(value, (IComparable)list[middle]);
				if (comparisonResult == 0)
					return middle;
				else if (comparisonResult < 0)
					upper = middle - 1;
				else
					lower = middle + 1;
			}

			if (searchMode == SearchMode.Exact)
				return -1;
			if (searchMode == SearchMode.Nearest)
				return GetNearestMiddleIndex(list, lower, upper, value);

			middle = (lower + upper) / 2;
			return searchMode == SearchMode.RoundDown ? middle : middle + 1;
		}

		internal static int FindIndexInSortedData<TX>(this TX[] array, int arrayLength, TX value, SearchMode searchMode, IMath<TX> math)
			where TX: IComparable
		{
			// Basic sanity checks
			if (arrayLength == 0)
				return -1;

			if (value.CompareTo(array[0]) < 0)
				return searchMode == SearchMode.Exact ? -1 : 0;
			if (value.CompareTo(array[0]) == 0)
				return 0;
			if (value.CompareTo(array[arrayLength - 1]) == 0)
				return arrayLength - 1;
			if (value.CompareTo(array[arrayLength - 1]) > 0)
				return searchMode == SearchMode.Exact ? -1 : arrayLength - 1;

			int lower = 0;
			int upper = arrayLength - 1;
			int middle = -1;
			while (lower <= upper)
			{
				middle = (lower + upper) / 2;
				int comparisonResult = value.CompareTo(array[middle]);
				if (comparisonResult == 0)
					return middle;
				else if (comparisonResult < 0)
					upper = middle - 1;
				else
					lower = middle + 1;
			}

			if (searchMode == SearchMode.Exact)
				return -1;
			if (searchMode == SearchMode.Nearest)
				return GetNearestMiddleIndex(array, arrayLength, lower, upper, value, math);

			middle = (lower + upper) / 2;
			return searchMode == SearchMode.RoundDown ? middle : middle + 1;
		}

		private static int GetNearestMiddleIndex(IList list, int lower, int upper, IComparable value) // todo: it neds comments
		{
			if (lower > upper)
			{
				int temp = upper;
				upper = lower;
				lower = temp;
			}

			upper = NumberUtil.Constrain(upper, 0, list.Count-1);
			lower = NumberUtil.Constrain(lower, 0, list.Count-1);
			double lowerValue = ((IComparable) list[lower]).ToDouble();
			double upperValue = ((IComparable) list[upper]).ToDouble();
			double desired = value.ToDouble();

			return desired - lowerValue >= upperValue - desired ? upper : lower;
		}
		private static int GetNearestMiddleIndex<TX>(TX[] array, int arrayLength, int lower, int upper, TX value, IMath<TX> math) // todo: it neds comments
			where TX : IComparable
		{
			if (lower > upper)
			{
				int temp = upper;
				upper = lower;
				lower = temp;
			}

			upper = NumberUtil.Constrain(upper, 0, arrayLength - 1);
			lower = NumberUtil.Constrain(lower, 0, arrayLength - 1);
			var lowerValue = array[lower];
			var upperValue = array[upper];
			var desired = value;

			return math.Subtract(desired, lowerValue).CompareTo(math.Subtract(upperValue, desired)) >= 0 ? upper : lower;
		}

		internal static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
		{
			foreach(var item in values)
				collection.Add(item);
		}


		// CODE REVIEW {abt}: Bad idea - having static variables inside a shared static class can lead to state corruption
		private static bool _isLokingSameColors;
		private static Point _prevPoint;
		private static Color? _prevPointColor;


		internal static IEnumerable<IEnumerable<Tuple<Point, Point>>> SplitMultilineByGaps(this IEnumerable<Tuple<Point, Point>> points)
		{
			var iterator = points.GetEnumerator();
			if (iterator.MoveNext())
			{
				while (EnumerateUntilNonGap(iterator))
				{
					yield return EnumerateUntilGap(iterator);
					if (!iterator.MoveNext()) yield break;
				}
			}
		}

		/// <param name="iterator">must be set to first NaN point in collection</param>
		static bool EnumerateUntilNonGap(IEnumerator<Tuple<Point, Point>> iterator)
		{   
			if (double.IsNaN(iterator.Current.Item1.X) || double.IsNaN(iterator.Current.Item1.Y))
			{
				while (iterator.MoveNext())
				{
					if (!double.IsNaN(iterator.Current.Item1.X) && !double.IsNaN(iterator.Current.Item1.Y))
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}

		/// <param name="iterator">must be set to first non-NaN point in collection</param>
		static IEnumerable<Tuple<Point, Point>> EnumerateUntilGap(IEnumerator<Tuple<Point, Point>> iterator)
		{
			if (!double.IsNaN(iterator.Current.Item1.X) && !double.IsNaN(iterator.Current.Item1.Y))
			{
				yield return iterator.Current;
				while (iterator.MoveNext())
				{
					if (!double.IsNaN(iterator.Current.Item1.X) && !double.IsNaN(iterator.Current.Item1.Y))
					{
						yield return iterator.Current;
					}
					else
					{
						yield break;
					}
				}
			}
		}

		/// <summary>
		/// Splits single <see cref="BandSeriesInfo"/> onto two instances in order to siplify working with <see cref="SeriesInfo"/> collections
		/// </summary>
		/// <param name="infos"></param>
		/// <returns></returns>
		internal static IEnumerable<SeriesInfo> SplitToSinglePointInfo(this IEnumerable<SeriesInfo> infos)
		{
			var enumerator = infos.GetEnumerator();

			while (enumerator.MoveNext())
			{
				var bandSeriesInfo = enumerator.Current as BandSeriesInfo;

				if (bandSeriesInfo != null)
				{
					var hitTestInfo = new HitTestInfo
										  {
											  // Change Y on Y1
												YValue = bandSeriesInfo.Y1Value,
												Y1Value = bandSeriesInfo.YValue,
												HitTestPoint = bandSeriesInfo.Xy1Coordinate,
											  // Copy other values
												XValue = bandSeriesInfo.XValue,
												DataSeriesIndex = bandSeriesInfo.DataSeriesIndex,
												DataSeriesType = bandSeriesInfo.DataSeriesType,
												IsHit = bandSeriesInfo.IsHit,
										  };

					var firstBandInfo = new BandSeriesInfo(bandSeriesInfo.RenderableSeries, hitTestInfo)
											{
												IsFirstSeries = true,
											};

					yield return firstBandInfo;
				}
				yield return enumerator.Current;
			}
		}

		/// <summary>
		/// Returns the maximum value or null if sequence is empty.
		/// </summary>
		/// <param name="that">The sequence to retrieve the maximum value from.
		/// </param>
		/// <returns>The maximum value or null.</returns>
		public static T? MaxOrNullable<T>(this IEnumerable<T> that)
			where T : struct, IComparable
		{
			if (!that.Any())
			{
				return null;
			}
			return that.Max();
		}

		/// <summary>
		/// Returns the minimum value or null if sequence is empty.
		/// </summary>
		/// <param name="that">The sequence to retrieve the minimum value from.
		/// </param>
		/// <returns>The minimum value or null.</returns>
		public static T? MinOrNullable<T>(this IEnumerable<T> that)
			where T : struct, IComparable
		{
			if (!that.Any())
			{
				return null;
			}
			return that.Min();
		}
	}

	/// <summary>
	/// Enumeration constants to define binary searching of lists
	/// </summary>
	public enum SearchMode
	{
		/// <summary>
		/// Specifies exact search. If the index is not found, -1 is returned. 
		/// </summary>
		Exact, 

		/// <summary>
		/// Specifies the nearest index. This will round up or down if the search is in-between x-values
		/// </summary>
		Nearest, 

		/// <summary>
		/// Rounds down to the nearest index.
		/// </summary>
		RoundDown, 

		/// <summary>
		/// Rounds up to the nearest index
		/// </summary>
		RoundUp
	}
}