// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ComparableUtil.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;

namespace Ecng.Xaml.Charting.Utility
{
    internal class ComparableUtil
    {
        private static readonly IDictionary<Type, IComparable> _zeroValues = new Dictionary<Type, IComparable>()
                                                                                {
                                                                                    {typeof(Int64),     (Int64)0},
                                                                                    {typeof(Int32),     (Int32)0},
                                                                                    {typeof(Int16),     (Int16)0},
                                                                                    {typeof(SByte),     (SByte)0},                                                                                    
                                                                                    {typeof(UInt64),    (UInt64)0},
                                                                                    {typeof(UInt32),    (UInt32)0},
                                                                                    {typeof(UInt16),    (UInt16)0},
                                                                                    {typeof(Byte),      (Byte)0},
                                                                                    {typeof(Decimal),   (Decimal)0},
                                                                                    {typeof(Double),    (Double)0},
                                                                                    {typeof(Single),    (Single)0},
                                                                                    {typeof(DateTime),  new DateTime(0)},
                                                                                };

        private static readonly IDictionary<Type, IComparable> _maxValues = new Dictionary<Type, IComparable>()
                                                                                {
                                                                                    {typeof(Int64),     Int64.MaxValue},
                                                                                    {typeof(Int32),     Int32.MaxValue},
                                                                                    {typeof(Int16),     Int16.MaxValue},
                                                                                    {typeof(SByte),     SByte.MaxValue},                                                                                    
                                                                                    {typeof(UInt64),    UInt64.MaxValue},
                                                                                    {typeof(UInt32),    UInt32.MaxValue},
                                                                                    {typeof(UInt16),    UInt16.MaxValue},
                                                                                    {typeof(Byte),      Byte.MaxValue},
                                                                                    {typeof(Decimal),   Decimal.MaxValue},
                                                                                    {typeof(Double),    Double.MaxValue},
                                                                                    {typeof(Single),    Single.MaxValue},
                                                                                    {typeof(DateTime),  DateTime.MaxValue},
                                                                                };

        private static readonly IDictionary<Type, IComparable> _minValues = new Dictionary<Type, IComparable>()
                                                                                {
                                                                                    {typeof(Int64),     Int64.MinValue},
                                                                                    {typeof(Int32),     Int32.MinValue},
                                                                                    {typeof(Int16),     Int16.MinValue},
                                                                                    {typeof(SByte),     SByte.MinValue},                                                                                    
                                                                                    {typeof(UInt64),    UInt64.MinValue},
                                                                                    {typeof(UInt32),    UInt32.MinValue},
                                                                                    {typeof(UInt16),    UInt16.MinValue},
                                                                                    {typeof(Byte),      Byte.MinValue},
                                                                                    {typeof(Decimal),   Decimal.MinValue},
                                                                                    {typeof(Double),    Double.MinValue},
                                                                                    {typeof(Single),    Single.MinValue},
                                                                                    {typeof(DateTime),  DateTime.MinValue},
                                                                                };


        private static readonly IDictionary<Type, Func<IComparable, bool>> _isDefinedDelegates = new Dictionary<Type, Func<IComparable, bool>>()
                                                                                {
                                                                                    {typeof(Int64),     (c) => IsDefined((Int64)c) },
                                                                                    {typeof(Int32),     (c) => IsDefined((Int32)c)},
                                                                                    {typeof(Int16),     (c) => true},
                                                                                    {typeof(SByte),     (c) => true},                                                                                    
                                                                                    {typeof(UInt64),    (c) => true},
                                                                                    {typeof(UInt32),    (c) => true},
                                                                                    {typeof(UInt16),    (c) => true},
                                                                                    {typeof(Byte),      (c) => true},
                                                                                    {typeof(Decimal),   (c) => IsDefined((Decimal)c)},
                                                                                    {typeof(Double),    (c) => IsDefined((Double)c)},
                                                                                    {typeof(Single),    (c) => IsDefined((Single)c)},
                                                                                    {typeof(DateTime),  (c) => IsDefined((DateTime)c)},
                                                                                    {typeof(TimeSpan),  (c) => IsDefined((TimeSpan)c)},
                                                                                };

        internal static TComparable MaxValue<TComparable>() where TComparable : IComparable
        {
            var comparableType = typeof(TComparable);
            if (_maxValues.ContainsKey(comparableType))
            {
                return (TComparable)_maxValues[comparableType];
            }

            throw new InvalidOperationException(string.Format("Cannot get the MaxValue of Type {0}", comparableType));
        }

        internal static TComparable Zero<TComparable>()
        {
            var comparableType = typeof(TComparable);
            if (_maxValues.ContainsKey(comparableType))
            {
                return (TComparable)_zeroValues[comparableType];
            }

            throw new InvalidOperationException(string.Format("Cannot get the Zero Value of Type {0}", comparableType));
        }

        public static IComparable MinValue(Type comparableType)
        {
            if (_minValues.ContainsKey(comparableType))
            {
                return _minValues[comparableType];
            }

            throw new InvalidOperationException(string.Format("Cannot get the MinValue of Type {0}", comparableType));
        }

        internal static TComparable MinValue<TComparable>() where TComparable : IComparable
        {
            var comparableType = typeof(TComparable);
            if (_minValues.ContainsKey(comparableType))
            {
                return (TComparable)_minValues[comparableType];
            }

            throw new InvalidOperationException(string.Format("Cannot get the MinValue of Type {0}", comparableType));
        }

        internal static bool IsValidComparableType<TComparable>() where TComparable : IComparable
        {
            return IsValidComparableType(typeof(TComparable));
        }

        internal static bool IsValidComparableType(Type comparableType)
        {
            return _minValues.ContainsKey(comparableType);
        }

        internal static TComparable Max<TComparable>(TComparable first, TComparable second) where TComparable : IComparable
        {
            return first.CompareTo(second) > 0 ? first : second;
        }

        internal static TComparable Min<TComparable>(TComparable first, TComparable second) where TComparable : IComparable
        {
            return second.CompareTo(first) > 0 ? first : second;
        }

        internal static TComparable Max<TComparable>(TComparable a, TComparable b, TComparable c, TComparable d) where TComparable : IComparable
        {
            return Max(a, Max(b, Max(c, d)));
        }

        internal static TComparable Min<TComparable>(TComparable a, TComparable b, TComparable c, TComparable d) where TComparable : IComparable
        {
            return Min(a, Min(b, Min(c, d)));
        }

        internal static bool IsDefined(IComparable comparable)
        {
            // Fast path for most common types
            if (comparable is double)
                return IsDefined((double)comparable);

            if (comparable is DateTime)
                return IsDefined((DateTime)comparable);

            var comparableType = comparable.GetType();

            if (_isDefinedDelegates.ContainsKey(comparableType))
            {
                return _isDefinedDelegates[comparableType](comparable);
            }

            throw new InvalidOperationException(string.Format("The Type {0} is not a valid Comparable Type", comparable));
        }

        internal static double ToDouble( DateTime value )
        {
            return value.Ticks;
        }

        internal static double ToDouble(IComparable comparable)
        {
            if (comparable is DateTime)
                return ((DateTime)comparable).Ticks;

            if (comparable is TimeSpan)
                return ((TimeSpan)comparable).Ticks;

            return Convert.ToDouble(comparable, CultureInfo.InvariantCulture);
        }

        private static bool IsDefined(Int16 arg) { return true; }
        private static bool IsDefined(SByte arg) { return true; }
        private static bool IsDefined(UInt64 arg) { return true; }
        private static bool IsDefined(UInt32 arg) { return true; }
        private static bool IsDefined(UInt16 arg) { return true; }
        private static bool IsDefined(Byte arg) { return true; }

        // Need this implementation for IndexRange & DateTimeRange convertsions(DateTimeRange.Ticks)
        private static bool IsDefined(Int64 arg) { return arg != Int64.MinValue && arg != Int64.MaxValue; }
        private static bool IsDefined(Int32 arg) { return arg != Int32.MinValue && arg != Int32.MaxValue; }

        private static bool IsDefined(Decimal arg) { return arg != Decimal.MinValue && arg != Decimal.MaxValue; }
        private static bool IsDefined(Double arg) { return arg != Double.MinValue && arg != Double.MaxValue && !Double.IsInfinity(arg) && !Double.IsNaN(arg); }
        private static bool IsDefined(Single arg) { return arg != Single.MinValue && arg != Single.MaxValue && !Single.IsNaN(arg); }
        private static bool IsDefined(DateTime arg) { return arg != DateTime.MaxValue; }
        private static bool IsDefined(TimeSpan arg) { return arg != TimeSpan.MaxValue; }


        private static bool CanConvertToUInt64(double value) { return IsDefined(value) && value >= UInt64.MinValue && value <= UInt64.MaxValue; }
        private static bool CanConvertToUInt32(double value) { return IsDefined(value) && value >= UInt32.MinValue && value <= UInt32.MaxValue; }
        private static bool CanConvertToUInt16(double value) { return IsDefined(value) && value >= UInt16.MinValue && value <= UInt16.MaxValue; }
        private static bool CanConvertToSByte(double value) { return IsDefined(value) && value >= SByte.MinValue && value <= SByte.MaxValue; }
        private static bool CanConvertToByte(double value) { return IsDefined(value) && value >= Byte.MinValue && value <= Byte.MaxValue; }
        private static bool CanConvertToInt64(double value) { return IsDefined(value) && value > Int64.MinValue && value < Int64.MaxValue; }
        private static bool CanConvertToInt32(double value) { return IsDefined(value) && value >= Int32.MinValue && value <= Int32.MaxValue; }
        private static bool CanConvertToInt16(double value) { return IsDefined(value) && value >= Int16.MinValue && value <= Int16.MaxValue; }
        private static bool CanConvertToDecimal(double value) { return IsDefined(value) && value >= (double)Decimal.MinValue && value <= (double)Decimal.MaxValue; }
        private static bool CanConvertToSingle(double value) { return IsDefined(value) && value >= Single.MinValue && value <= Single.MaxValue; }
        private static bool CanConvertToDateTime(double value) { return CanConvertToInt64(value) && value >= 0; }

        public static DateTime DateTimeFromDouble(double rawDataValue)
        {
            if (IsDefined(rawDataValue))
            {
                return new DateTime((long)rawDataValue);
            }

            return DateTime.MaxValue;
        }

        public static IComparable FromDouble(double rawDataValue, Type type)
        {
            IComparable result = null;

            if (CanChangeType(rawDataValue, type))
            {
                if (type == typeof (DateTime))
                {
                    result = new DateTime((long) rawDataValue);
                }
                else if (type == typeof (TimeSpan))
                {
                    result = new TimeSpan((long) rawDataValue);
                }
                else
                {
                    result = (IComparable) Convert.ChangeType(rawDataValue, type, CultureInfo.InvariantCulture);
                }
            }

            return result;
        }

        internal static bool CanChangeType(double value, Type conversionType)
        {
            var result = false;

            switch (Type.GetTypeCode(conversionType))
            {
                case TypeCode.Double:
                    result = true;
                    break;
                case TypeCode.Byte:
                    result = CanConvertToByte(value);
                    break;
                case TypeCode.Decimal:
                    result = CanConvertToDecimal(value);
                    break;
                case TypeCode.Int16:
                    result = CanConvertToInt16(value);
                    break;
                case TypeCode.Int32:
                    result = CanConvertToInt32(value);
                    break;
                case TypeCode.SByte:
                    result = CanConvertToSByte(value);
                    break;
                case TypeCode.Single:
                    result = CanConvertToSingle(value);
                    break;
                case TypeCode.UInt16:
                    result = CanConvertToUInt16(value);
                    break;
                case TypeCode.UInt32:
                    result = CanConvertToUInt32(value);
                    break;
                case TypeCode.UInt64:
                    result = CanConvertToUInt64(value);
                    break;
                case TypeCode.DateTime:
                    result = CanConvertToDateTime(value);
                    break;
                case TypeCode.Int64:
                    result = CanConvertToInt64(value);
                    break;
                default:
                    if (conversionType == typeof (TimeSpan))
                    {
                        result = CanConvertToInt64(value);
                    }
                    break;
            }

            return result;
        }

        public static IComparable Subtract(IComparable yValue, IComparable yValue2)
        {
            throw new NotImplementedException();
        }
    }
}