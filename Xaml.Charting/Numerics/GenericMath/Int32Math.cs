// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Int32Math.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class Int32Math : IMath<int>
    {
        public int MaxValue
        {
            get { return Int32.MaxValue; }
        }

        public int MinValue
        {
            get { return Int32.MinValue; }
        }

        public int ZeroValue
        {
            get { return 0; }
        }

        public int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        public int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        public int MinGreaterThan(int floor, int a, int b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(int value)
        {
            return false;
        }

        public int Subtract(int a, int b)
        {
            return a - b;
        }

        public int Abs(int a)
        {
            return Math.Abs(a);
        }

        public double ToDouble(int value)
        {
            return value;
        }

        public int Mult(int lhs, int rhs)
        {
            return lhs * rhs;
        }

        public int Mult(int lhs, double rhs)
        {
            return (int) (lhs * rhs);
        }

        public int Add(int lhs, int rhs)
        {
            return lhs + rhs;
        }

        public int Inc(ref int value)
        {
            return ++value;
        }
        public int Dec(ref int value)
        {
            return --value;
        }
    }
}
