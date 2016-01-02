// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NumberUtil.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;

namespace Ecng.Xaml.Charting.Utility
{
    public static class NumberUtil
    {
        private const double EPSILON = 1E-15d;
        private static readonly decimal DecimalEpsilon = new decimal(1, 0, 0, false, 28); //1e-28m;

        internal static byte NumDigitsInPositiveNumber(this int x) {
            if (x < 1000000) {
                if (x < 10) return 1;
                if (x < 100) return 2;
                if (x < 1000) return 3;
                if (x < 10000) return 4;
                if (x < 100000) return 5;
                return 6;
            }

            if (x < 10000000) return 7;
            if (x < 100000000) return 8;
            if (x < 1000000000) return 9; 
            if (x < 10000000000) return 10; 

            throw new InvalidOperationException("impossible");
        }

        internal static byte NumDigitsInPositiveNumber(this long x) {
            if (x < 1000000) {
                if (x < 10) return 1;
                if (x < 100) return 2;
                if (x < 1000) return 3;
                if (x < 10000) return 4;
                if (x < 100000) return 5;
                return 6;
            }

            if (x < 10000000) return 7;
            if (x < 100000000) return 8;
            if (x < 1000000000) return 9; 
            if (x < 10000000000) return 10; 
            if (x < 100000000000) return 11; 
            if (x < 1000000000000) return 12; 

            return (byte)(Math.Truncate(Math.Log10(x)) + 1); // Very uncommon
        }

        internal static bool DoubleEquals(this double value, double other) {
            return Math.Abs(value - other) < EPSILON;
        }

        public static double Round(this double value, double nearest) {
            return Math.Round(value/nearest)*nearest;                
        }

        public static double NormalizePrice(this double price, double priceStep) {
            return Math.Round(price/priceStep)*priceStep;                
        }

        public static float Round(this float value, float nearest)
        {
            return (float)Math.Round((double)value/nearest)*nearest;                
        }

        public static double RoundUp(double value, double nearest)
        {
            return Math.Ceiling(value/nearest)*nearest;                
        }

        public static double RoundDown(double value, double nearest)
        {
            return Math.Floor(value / nearest) * nearest;
        }

        internal static bool IsDivisibleBy(double value, double divisor)
        {
            value = Math.Round(value, 15);

            if (Math.Abs(divisor - 0) < EPSILON)
                return false;

            var divided = Math.Abs(value/divisor);
            double epsilon = EPSILON * divided;
            return Math.Abs(divided - Math.Round(divided)) <= epsilon;
        }

        internal static bool IsDivisibleBy(decimal value, decimal divisor)
        {
            if (Math.Abs(divisor - 0M) < DecimalEpsilon)
                return false;

            var divided = Math.Abs(value / divisor);
            decimal epsilon = DecimalEpsilon * divided;
            return Math.Abs(divided - Math.Round(divided)) <= epsilon;
        }

        internal static decimal RoundUp(decimal value, decimal nearest)
        {
            return decimal.Ceiling(value/nearest)*nearest;
        }

        public static void Swap(ref int value1, ref int value2)
        {
            int temp = value2;
            value2 = value1;
            value1 = temp;
        }

        public static void Swap(ref long value1, ref long value2)
        {
            long temp = value2;
            value2 = value1;
            value1 = temp;
        }

        public static void Swap(ref double value1, ref double value2)
        {
            double temp = value2;
            value2 = value1;
            value1 = temp;
        }

        public static void Swap(ref float value1, ref float value2)
        {
            float temp = value2;
            value2 = value1;
            value1 = temp;
        }

        /// <summary>
        /// Swaps X1,X2 and Y1,Y2 so that the first coordinate pair is always to the left of the second coordinate pair
        /// </summary>
        /// <param name="xCoord1"></param>
        /// <param name="xCoord2"></param>
        /// <param name="yCoord1"></param>
        /// <param name="yCoord2"></param>
        internal static void SortedSwap(ref double xCoord1, ref double xCoord2, ref double yCoord1, ref double yCoord2)
        {
            if (xCoord1 > xCoord2)
            {
                double temp = xCoord1;
                xCoord1 = xCoord2;
                xCoord2 = temp;

                temp = yCoord1;
                yCoord1 = yCoord2;
                yCoord2 = temp;
            }
        }

        public static int Constrain(int value, int lowerBound, int upperBound)
        {
            return value < lowerBound ? lowerBound : value > upperBound ? upperBound : value;
        }

        public static long Constrain(long value, long lowerBound, long upperBound)
        {
            return value < lowerBound ? lowerBound : value > upperBound ? upperBound : value;
        }

        public static double Constrain(double value, double lowerBound, double upperBound)
        {
            return value < lowerBound ? lowerBound : value > upperBound ? upperBound : value;
        }

        public static bool IsPowerOf(double value, double power, double logBase)
        {
            return Math.Abs(RoundUpPower(value, power, logBase) - (double)value) <= double.Epsilon;
        }

        internal static double RoundUpPower(double value, double power, double logBase)
        {
            bool flip = Math.Sign(value) == -1;

            double logWithPowerBase = Math.Log(Math.Abs(value), logBase) / Math.Log(Math.Abs(power), logBase);

            // Round to mitigate a log calculation mistake
            logWithPowerBase = Math.Round(logWithPowerBase, 5);
            double exponent = Math.Ceiling(logWithPowerBase);

            if (Math.Abs(exponent - logWithPowerBase) < double.Epsilon) return value;

            exponent = flip ? exponent - 1 : exponent;

            double result = Math.Pow(power, exponent);
            return flip ? -result : result;
        }

        internal static double RoundDownPower(double value, double power, double logBase)
        {
            bool flip = Math.Sign(value) == -1;

            double logWithPowerBase = Math.Log(Math.Abs(value), logBase) / Math.Log(Math.Abs(power), logBase);

            // Round to mitigate a log calculation mistake
            logWithPowerBase = Math.Round(logWithPowerBase, 5);
            double exponent = Math.Floor(logWithPowerBase);

            if (Math.Abs(exponent - logWithPowerBase) < double.Epsilon) return value;

            exponent = flip ? exponent - 1 : exponent;

            double result = Math.Pow(power, exponent);
            return flip ? -result : result;
        }

        internal static bool IsIntegerType(Type type)
        {
            var result = false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    result = true;
                    break;
            }

            return result;
        }

        public static bool IsNaN(double value)
        {
            return value != value;
        }
    }
}