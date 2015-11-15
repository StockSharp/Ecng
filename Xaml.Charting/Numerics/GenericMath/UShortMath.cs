// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UShortMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Numerics.GenericMath
{
    internal sealed class UShortMath : IMath<ushort>
    {
        public ushort MinValue
        {
            get { return ushort.MinValue; }
        }

        public ushort MaxValue
        {
            get { return ushort.MaxValue; }
        }

        public ushort ZeroValue
        {
            get { return 0; }
        }

        public ushort Max(ushort a, ushort b)
        {
            return a > b ? a : b;
        }

        public ushort Min(ushort a, ushort b)
        {
            return a < b ? a : b;
        }

        public ushort MinGreaterThan(ushort floor, ushort a, ushort b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(ushort value)
        {
            return false;   
        }

        public ushort Subtract(ushort a, ushort b)
        {
            return (ushort) (a - b);
        }

        public ushort Abs(ushort a)
        {
            return a;
        }

        public double ToDouble(ushort value)
        {
            return value;
        }

        public ushort Mult(ushort lhs, ushort rhs)
        {
            return (ushort)(lhs * rhs);
        }

        public ushort Mult(ushort lhs, double rhs)
        {
            return (ushort)(lhs * rhs);
        }

        public ushort Add(ushort lhs, ushort rhs)
        {
            return (ushort)(lhs + rhs);
        }

        public ushort Inc(ref ushort value)
        {
            return ++value;
        }
        public ushort Dec(ref ushort value)
        {
            return --value;
        }
    }
}