// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Resamplers.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using Ecng.Xaml.Charting.Common.Extensions;
// ReSharper Disable All

namespace Ecng.Xaml.Charting.Numerics.GenericMath
{
	internal static class ArrayOperations
    {
		private static readonly IDictionary<Type, object> _genericArrayHelpers = new Dictionary<Type, object>();

		static ArrayOperations()
		{
			// Register generic helpers	
			_genericArrayHelpers.Add(typeof(Decimal), new DecimalGenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(Double), new DoubleGenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(Single), new SingleGenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(Int32), new Int32GenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(UInt32), new UInt32GenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(Int64), new Int64GenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(UInt64), new UInt64GenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(Int16), new Int16GenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(UInt16), new UInt16GenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(Byte), new ByteGenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(SByte), new SByteGenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(DateTime), new DateTimeGenericArrayHelper());	
			_genericArrayHelpers.Add(typeof(TimeSpan), new TimeSpanGenericArrayHelper());	
		}

		/// <returns>T.MinValue if there are no elements in input. This is required for joining ranges of dataseries</returns>
        internal static T Maximum<T>(IEnumerable<T> enumerable)
        {
            var array = enumerable as T[];
            if (array != null)
            {
                return Maximum<T>(array, 0, array.Length);
            }

            var iList = enumerable as IList<T>;
            if (iList != null)
            {
                return Maximum<T>(iList.ToUncheckedList(), 0, iList.Count);
            }

            var math = GenericMathFactory.New<T>();
            var max = math.MinValue;
            foreach (var item in enumerable)
                max = math.Max(max, item);

            return max;
        }

		/// <returns>T.MaxValue if there are no elements in input. This is required for joining ranges of dataseries</returns>
        internal static T Minimum<T>(IEnumerable<T> enumerable)
        {
          var math = GenericMathFactory.New<T>();

          return Minimum(enumerable, math.Min);
        }

        internal static T MinGreaterThan<T>(IEnumerable<T> enumerable, T floor)
        {
            var math = GenericMathFactory.New<T>();

            return Minimum(enumerable, (a,b) => math.MinGreaterThan(floor, a, b));
        }

        internal static T Minimum<T>(IEnumerable<T> enumerable, Func<T,T,T> minFunc)
        {
            var array = enumerable as T[];
            if (array != null)
            {
                return Minimum<T>(array, 0, array.Length, minFunc);
            }

            var iList = enumerable as IList<T>;
            if (iList != null)
            {
                return Minimum<T>(iList.ToUncheckedList(), 0, iList.Count, minFunc);
            }

            var math = GenericMathFactory.New<T>();
            var min = math.MaxValue;
            foreach (var item in enumerable)
                min = minFunc(min, item);

            return min;
        }

	
        /// <summary>
        /// Fast generic computation of the Min and Max of an enumerable 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">The input enumerable.</param>
        /// <param name="min">T.MaxValue if there are no elements in input. This is required for joining ranges of dataseries</param>
        /// <param name="max">T.MinValue if there are no elements in input. This is required for joining ranges of dataseries</param>
        public static void MinMax<T>(IEnumerable<T> enumerable, out T min, out T max)
        {
            var array = enumerable as T[];
            if (array != null)
            {
                MinMax<T>(array, 0, array.Length, out min, out max);
                return;
            }

            var iList = enumerable as IList<T>;
            if (iList != null)
            {
                MinMax<T>(iList.ToUncheckedList(), 0, iList.Count, out min, out max);
                return;
            }

            var math = GenericMathFactory.New<T>();
            max = math.MinValue;
            min = math.MaxValue;
            foreach (var item in enumerable)
            {
                max = math.Max(max, item);
                min = math.Min(min, item);
            }            
        }

		public static bool IsSortedAscending<T>(IEnumerable<T> enumerable)
			where T:IComparable
		{
			var array = enumerable as T[];
            if (array != null)
            {
                return IsSortedAscending<T>(array, 0, array.Length);
            }

            var iList = enumerable as IList<T>;
            if (iList != null)
            {
                return IsSortedAscending<T>(iList.ToUncheckedList(), 0, iList.Count);
            }

			var itr = enumerable.GetEnumerator();
			if (!itr.MoveNext()) return true;
			var last = itr.Current;
			
			while(itr.MoveNext())
			{
				if (itr.Current.CompareTo(last) < 0) 
					return false;

				last = itr.Current;
			}
			return true;
		}

        public static bool IsEvenlySpaced<T>(IEnumerable<T> enumerable, double epsilon, out double spacing)
			where T:IComparable
		{
			var array = enumerable as T[];
            if (array != null)
            {
                return IsEvenlySpaced<T>(array, 0, array.Length, epsilon, out spacing);
            }

            var iList = enumerable as IList<T>;
            if (iList != null)
            {
                return IsEvenlySpaced<T>(iList.ToUncheckedList(), 0, iList.Count, epsilon, out spacing);
            }

            var math = GenericMathFactory.New<T>();
			var itr = enumerable.GetEnumerator();
			if (!itr.MoveNext()) 
			{
				spacing = 1.0;
				return true;
			}
			double last = math.ToDouble(itr.Current);
            if (!itr.MoveNext()) 
			{
				spacing = 1.0;
				return true;
			}
            double current = math.ToDouble(itr.Current);
            double lastDiff = current - last;
			last = current;

			while(itr.MoveNext())
			{
				current = math.ToDouble(itr.Current);
                double diff = current - last;
                if (Math.Abs(lastDiff - diff) > epsilon) 
				{
					spacing = Math.Abs(lastDiff);
					return false;
				}
                lastDiff = diff;
                last = current;
			}
			spacing = Math.Abs(lastDiff);
			return true;
		}

		/// <returns>T.MaxValue if there are no elements in input. This is required for joining ranges of dataseries</returns>
        internal static T Minimum<T>(T[] array, int startIndex, int count)
		{
            var math = GenericMathFactory.New<T>();

            return Minimum(array, startIndex, count, math.Min);
		}		

		// TODO {ABT} this one can go into generic helpers
		/// <returns>T.MaxValue if there are no elements in input. This is required for joining ranges of dataseries</returns>
        internal static T Minimum<T>(T[] array, int startIndex, int count, Func<T,T,T> minFunc)
        {
            var math = GenericMathFactory.New<T>();

            T min = math.MaxValue;

            int i = startIndex;
            int iMax = count;

            if (array.Length > 16)
            {
                // Unroll loop in blocks of 16 to calculate max
                int iMax2 = iMax - iMax % 16;
                for (; i != iMax2; )
                {
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                    min = minFunc(min, array[i]); ++i;
                }
            }

            // Complete loop
            for (; i != iMax; )
            {
                min = minFunc(min, array[i]); ++i;
            }

            return min;
        }

		/// <returns>T.MinValue if there are no elements in input. This is required for joining ranges of dataseries</returns>
        internal static T Maximum<T>(T[] array, int startIndex, int count)
		{
			return ((IGenericArrayHelper<T>)_genericArrayHelpers[typeof(T)]).Maximum(array, startIndex, count);
		}

		/// <summary>
        /// Fast generic computation of the Min and Max of an array 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array">The input array.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The count.</param>
        /// <param name="min">T.MaxValue if there are no elements in input. This is required for joining ranges of dataseries</param>
        /// <param name="max">T.MinValue if there are no elements in input. This is required for joining ranges of dataseries</param>
        internal static void MinMax<T>(T[] array, int startIndex, int count, out T min, out T max)
        {
			((IGenericArrayHelper<T>)_genericArrayHelpers[typeof(T)]).MinMax(array, startIndex, count, out min, out max);
		}

		/// <summary>
        /// Fast generic computation of whether an array is sorted
        /// </summary>
        internal static bool IsSortedAscending<T>(T[] array, int startIndex, int count)
        {
            return ((IGenericArrayHelper<T>)_genericArrayHelpers[typeof(T)]).IsSortedAscending(array, startIndex, count);
        }

		/// <summary>
        /// Fast generic computation of whether a list is sorted
        /// </summary>
        internal static bool IsSortedAscending<T>(IList<T> items, int startIndex, int count)
        {
            return ((IGenericArrayHelper<T>)_genericArrayHelpers[typeof(T)]).IsSortedAscending(items, startIndex, count);
        }

		/// <summary>
        /// Fast generic computation of whether an array is evenly spaced
        /// </summary>
        internal static bool IsEvenlySpaced<T>(T[] array, int startIndex, int count, double epsilon, out double spacing)
        {
            return ((IGenericArrayHelper<T>)_genericArrayHelpers[typeof(T)]).IsEvenlySpaced(array, startIndex, count, epsilon, out spacing);
        }

		/// <summary>
        /// Fast generic computation of whether an array is evenly spaced
        /// </summary>
        internal static bool IsEvenlySpaced<T>(IList<T> items, int startIndex, int count, double epsilon, out double spacing)
        {
            return ((IGenericArrayHelper<T>)_genericArrayHelpers[typeof(T)]).IsEvenlySpaced(items, startIndex, count, epsilon, out spacing);
        }

		/// <summary>Interface to fast autogenerated generic Min Max helpers</summary>
		private interface IGenericArrayHelper<T>
		{
			T Maximum(T[] array, int startIndex, int count);
			void MinMax(T[] array, int startIndex, int count, out T min, out T max);
			bool IsSortedAscending(T[] array, int startIndex, int count);
			bool IsSortedAscending(IList<T> items, int startIndex, int count);
            bool IsEvenlySpaced(T[] array, int startIndex, int count, double epsilon, out double spacing);
			bool IsEvenlySpaced(IList<T> items, int startIndex, int count, double epsilon, out double spacing);
		}
			
		#region AutoGenerated DecimalGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class DecimalGenericArrayHelper : IGenericArrayHelper<Decimal>
		{
			public Decimal Maximum(Decimal[] array, int startIndex, int count)
			{
				unchecked
				{
					Decimal max = GenericMathFactory.New<Decimal>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Decimal current;

					#if !SILVERLIGHT
					fixed (Decimal * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Decimal[] array, int startIndex, int count, out Decimal min, out Decimal max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Decimal>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Decimal current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Decimal * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Decimal[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Decimal * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Decimal current;
						Decimal last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Decimal> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Decimal[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Decimal * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Decimal> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Decimal xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated DoubleGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class DoubleGenericArrayHelper : IGenericArrayHelper<Double>
		{
			public Double Maximum(Double[] array, int startIndex, int count)
			{
				unchecked
				{
					Double max = GenericMathFactory.New<Double>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Double current;

					#if !SILVERLIGHT
					fixed (Double * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Double[] array, int startIndex, int count, out Double min, out Double max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Double>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Double current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Double * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Double[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Double * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Double current;
						Double last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Double> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Double[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Double * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Double> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Double xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated SingleGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class SingleGenericArrayHelper : IGenericArrayHelper<Single>
		{
			public Single Maximum(Single[] array, int startIndex, int count)
			{
				unchecked
				{
					Single max = GenericMathFactory.New<Single>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Single current;

					#if !SILVERLIGHT
					fixed (Single * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Single[] array, int startIndex, int count, out Single min, out Single max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Single>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Single current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Single * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Single[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Single * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Single current;
						Single last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Single> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Single[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Single * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Single> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Single xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated Int32GenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class Int32GenericArrayHelper : IGenericArrayHelper<Int32>
		{
			public Int32 Maximum(Int32[] array, int startIndex, int count)
			{
				unchecked
				{
					Int32 max = GenericMathFactory.New<Int32>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Int32 current;

					#if !SILVERLIGHT
					fixed (Int32 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Int32[] array, int startIndex, int count, out Int32 min, out Int32 max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Int32>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Int32 current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Int32 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Int32[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Int32 * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Int32 current;
						Int32 last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Int32> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Int32[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Int32 * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Int32> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Int32 xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated UInt32GenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class UInt32GenericArrayHelper : IGenericArrayHelper<UInt32>
		{
			public UInt32 Maximum(UInt32[] array, int startIndex, int count)
			{
				unchecked
				{
					UInt32 max = GenericMathFactory.New<UInt32>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					UInt32 current;

					#if !SILVERLIGHT
					fixed (UInt32 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(UInt32[] array, int startIndex, int count, out UInt32 min, out UInt32 max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<UInt32>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					UInt32 current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (UInt32 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(UInt32[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (UInt32 * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						UInt32 current;
						UInt32 last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<UInt32> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(UInt32[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (UInt32 * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<UInt32> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(UInt32 xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated Int64GenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class Int64GenericArrayHelper : IGenericArrayHelper<Int64>
		{
			public Int64 Maximum(Int64[] array, int startIndex, int count)
			{
				unchecked
				{
					Int64 max = GenericMathFactory.New<Int64>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Int64 current;

					#if !SILVERLIGHT
					fixed (Int64 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Int64[] array, int startIndex, int count, out Int64 min, out Int64 max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Int64>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Int64 current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Int64 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Int64[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Int64 * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Int64 current;
						Int64 last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Int64> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Int64[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Int64 * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Int64> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Int64 xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated UInt64GenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class UInt64GenericArrayHelper : IGenericArrayHelper<UInt64>
		{
			public UInt64 Maximum(UInt64[] array, int startIndex, int count)
			{
				unchecked
				{
					UInt64 max = GenericMathFactory.New<UInt64>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					UInt64 current;

					#if !SILVERLIGHT
					fixed (UInt64 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(UInt64[] array, int startIndex, int count, out UInt64 min, out UInt64 max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<UInt64>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					UInt64 current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (UInt64 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(UInt64[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (UInt64 * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						UInt64 current;
						UInt64 last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<UInt64> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(UInt64[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (UInt64 * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<UInt64> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(UInt64 xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated Int16GenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class Int16GenericArrayHelper : IGenericArrayHelper<Int16>
		{
			public Int16 Maximum(Int16[] array, int startIndex, int count)
			{
				unchecked
				{
					Int16 max = GenericMathFactory.New<Int16>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Int16 current;

					#if !SILVERLIGHT
					fixed (Int16 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Int16[] array, int startIndex, int count, out Int16 min, out Int16 max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Int16>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Int16 current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Int16 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Int16[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Int16 * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Int16 current;
						Int16 last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Int16> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Int16[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Int16 * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Int16> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Int16 xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated UInt16GenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class UInt16GenericArrayHelper : IGenericArrayHelper<UInt16>
		{
			public UInt16 Maximum(UInt16[] array, int startIndex, int count)
			{
				unchecked
				{
					UInt16 max = GenericMathFactory.New<UInt16>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					UInt16 current;

					#if !SILVERLIGHT
					fixed (UInt16 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(UInt16[] array, int startIndex, int count, out UInt16 min, out UInt16 max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<UInt16>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					UInt16 current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (UInt16 * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(UInt16[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (UInt16 * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						UInt16 current;
						UInt16 last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<UInt16> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(UInt16[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (UInt16 * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<UInt16> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(UInt16 xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated ByteGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class ByteGenericArrayHelper : IGenericArrayHelper<Byte>
		{
			public Byte Maximum(Byte[] array, int startIndex, int count)
			{
				unchecked
				{
					Byte max = GenericMathFactory.New<Byte>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					Byte current;

					#if !SILVERLIGHT
					fixed (Byte * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(Byte[] array, int startIndex, int count, out Byte min, out Byte max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<Byte>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					Byte current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (Byte * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(Byte[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (Byte * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						Byte current;
						Byte last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<Byte> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(Byte[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (Byte * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<Byte> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(Byte xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated SByteGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class SByteGenericArrayHelper : IGenericArrayHelper<SByte>
		{
			public SByte Maximum(SByte[] array, int startIndex, int count)
			{
				unchecked
				{
					SByte max = GenericMathFactory.New<SByte>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					SByte current;

					#if !SILVERLIGHT
					fixed (SByte * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(SByte[] array, int startIndex, int count, out SByte min, out SByte max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<SByte>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					SByte current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (SByte * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(SByte[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (SByte * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						SByte current;
						SByte last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<SByte> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(SByte[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (SByte * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<SByte> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(SByte xValue) { return (double)xValue; }
		}	
		#endregion	
			
		#region AutoGenerated DateTimeGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class DateTimeGenericArrayHelper : IGenericArrayHelper<DateTime>
		{
			public DateTime Maximum(DateTime[] array, int startIndex, int count)
			{
				unchecked
				{
					DateTime max = GenericMathFactory.New<DateTime>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					DateTime current;

					#if !SILVERLIGHT
					fixed (DateTime * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(DateTime[] array, int startIndex, int count, out DateTime min, out DateTime max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<DateTime>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					DateTime current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (DateTime * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(DateTime[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (DateTime * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						DateTime current;
						DateTime last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<DateTime> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(DateTime[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (DateTime * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<DateTime> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(DateTime xValue) { return xValue.Ticks; }
		}	
		#endregion	
			
		#region AutoGenerated TimeSpanGenericArrayHelper
#if !SILVERLIGHT
		unsafe
#endif
		class TimeSpanGenericArrayHelper : IGenericArrayHelper<TimeSpan>
		{
			public TimeSpan Maximum(TimeSpan[] array, int startIndex, int count)
			{
				unchecked
				{
					TimeSpan max = GenericMathFactory.New<TimeSpan>().MinValue;

					// Sanity checks
					if (count == 0) return max; 
					if (count == 1)
					{
						max = array[0];
						return max;
					}

					int i = startIndex;
					int iMax = count;
					TimeSpan current;

					#if !SILVERLIGHT
					fixed (TimeSpan * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate max
						int iMax2 = iMax - iMax%16;
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
							current = ptr[i]; max = current > max ? current : max; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; ++i;
					}

					return max;

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public void MinMax(TimeSpan[] array, int startIndex, int count, out TimeSpan min, out TimeSpan max)
			{
				unchecked
				{
					var math = GenericMathFactory.New<TimeSpan>();
					max = math.MinValue;
					min = math.MaxValue;

					// Sanity checks
					if (count == 0) return;
					if (count == 1)
					{
						min = array[0];
						max = array[0];
						return;
					}

					TimeSpan current;

					int i = startIndex;
					int iMax = count;

					#if !SILVERLIGHT
					fixed (TimeSpan * ptr = &array[0])
					{
					#else
					var ptr = array;
					#endif

					if (array.Length > 16)
					{
						// Unroll loop in blocks of 16 to calculate min, max
						int iMax2 = iMax - iMax % 16;
                
						for (; i != iMax2; )
						{
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
							current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
						}
					}

					// Complete loop
					for (; i != iMax; )
					{
						current = ptr[i]; max = current > max ? current : max; min = current < min ? current : min; ++i;
					}

					#if !SILVERLIGHT
					}
					#endif
				}
			}

			public bool IsSortedAscending(TimeSpan[] array, int startIndex, int count)
			{
				unchecked
				{
                    int iMax = startIndex + count;
					int i = startIndex;

#if !SILVERLIGHT
					fixed (TimeSpan * ptr = &array[0]) {
#else
					var ptr = array;
#endif

						TimeSpan current;
						TimeSpan last = ptr[i++];

						for (; i < iMax; ++i)
						{
							current = ptr[i];
							if (current < last) return false;
							last = current;
						}

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsSortedAscending(IList<TimeSpan> items, int startIndex, int count)
			{
				if (count <= 1)
				{
					return true;
				}

				return IsSortedAscending(items.ToUncheckedList(), startIndex, count);
			}

			public bool IsEvenlySpaced(TimeSpan[] array, int startIndex, int count, double epsilon, out double spacing)
			{
                if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(array[startIndex]) - TxToDouble(array[startIndex+1]));
					return true;
				}

                int iMax = startIndex + count;
				unchecked
				{
#if !SILVERLIGHT
					fixed (TimeSpan * ptr = &array[0]) {
#else
					var ptr = array;
#endif
						
                        double last = TxToDouble(ptr[startIndex++]);
                        double current = TxToDouble(ptr[startIndex++]);
                        double lastDiff = current - last;
                        last = current;

						for (int i = startIndex; i != iMax; ++i)
						{
                            current = TxToDouble(ptr[i]);
							double diff = current - last;
                            if (Math.Abs(lastDiff - diff) > epsilon) 
							{
								spacing = Math.Abs(lastDiff);
								return false;
							}
                            lastDiff = diff;
                            last = current;
						}

						spacing = Math.Abs(lastDiff);

					#if !SILVERLIGHT
					}
					#endif
				}

				return true;
			}

			public bool IsEvenlySpaced(IList<TimeSpan> items, int startIndex, int count, double epsilon, out double spacing)
			{
			    if (count <= 1)
				{
					spacing = 1.0;
					return true;
				}

				if (count == 2)
				{
					spacing = Math.Abs(TxToDouble(items[startIndex]) - TxToDouble(items[startIndex+1]));
					return true;
				}

				return IsEvenlySpaced(items.ToUncheckedList(), startIndex, count, epsilon, out spacing);
			}

            private static double TxToDouble(TimeSpan xValue) { return xValue.Ticks; }
		}	
		#endregion	
}
    }
// ReSharper Restore All
