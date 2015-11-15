// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ShortMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Numerics.GenericMath
{
    internal sealed class ShortMath : IMath<short>
    {
        public short MinValue
        {
            get { return short.MinValue; }
        }

        public short MaxValue
        {
            get { return short.MaxValue; }
        }

        public short ZeroValue
        {
            get { return 0; }
        }

        public short Max(short a, short b)
        {
            return a > b ? a : b;
        }

        public short Min(short a, short b)
        {
            return a < b ? a : b;
        }

        public short MinGreaterThan(short floor, short a, short b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(short value)
        {
            return false;
        }

        public short Subtract(short a, short b)
        {
            return (short) (a - b);
        }

        public short Abs(short a)
        {
            return Math.Abs(a);
        }

        public double ToDouble(short value)
        {
            return value;
        }

        public short Mult(short lhs, short rhs)
        {
            return (short) (lhs * rhs);
        }

        public short Mult(short lhs, double rhs)
        {
            return (short)(lhs * rhs);
        }

        public short Add(short lhs, short rhs)
        {
            return (short) (lhs + rhs);
        }

        public short Inc(ref short value)
        {
            return ++value;
        }
        public short Dec(ref short value)
        {
            return --value;
        }
    }
}
