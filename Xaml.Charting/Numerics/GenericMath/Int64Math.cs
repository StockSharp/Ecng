// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Int64Math.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.GenericMath
{
    internal sealed class Int64Math : IMath<long>
    {
        public long MaxValue
        {
            get { return long.MaxValue; }
        }

        public long MinValue
        {
            get { return long.MinValue; }
        }

        public long ZeroValue
        {
            get { return 0; }
        }

        public long Max(long a, long b)
        {
            return a > b ? a : b;
        }

        public long Min(long a, long b)
        {
            return a < b ? a : b;
        }

        public long MinGreaterThan(long floor, long a, long b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(long value)
        {
            return false;
        }

        public long Subtract(long a, long b)
        {
            return a - b;
        }

        public long Abs(long a)
        {
            return Math.Abs(a);
        }

        public double ToDouble(long value)
        {
            return value;
        }

        public long Mult(long lhs, long rhs)
        {
            return lhs * rhs;
        }

        public long Mult(long lhs, double rhs)
        {
            return ((long)(lhs* rhs));
        }

        public long Add(long lhs, long rhs)
        {
            return lhs + rhs;
        }

        public long Inc(ref long value)
        {
            return ++value;
        }
        public long Dec(ref long value)
        {
            return --value;
        }
    }
}
