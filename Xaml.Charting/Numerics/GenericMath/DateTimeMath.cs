// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DateTimeMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class DateTimeMath : IMath<DateTime>
    {
        public DateTime MinValue
        {
            get { return DateTime.MinValue; }
        }

        public DateTime MaxValue
        {
            get { return DateTime.MaxValue; }
        }

        public DateTime ZeroValue
        {
            get { return DateTime.MinValue; }
        }

        public DateTime Max(DateTime a, DateTime b)
        {
            return a.Ticks > b.Ticks ? a : b;
        }

        public DateTime Min(DateTime a, DateTime b)
        {
            return a.Ticks < b.Ticks ? a : b;
        }

        public DateTime MinGreaterThan(DateTime floor, DateTime a, DateTime b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(DateTime value)
        {
            return false;
        }

        public DateTime Subtract(DateTime a, DateTime b)
        {
            return new DateTime(a.Ticks - b.Ticks);
        }

        public DateTime Abs(DateTime a)
        {
            return a;
        }

        public double ToDouble(DateTime value)
        {
            return value.Ticks;
        }

        public DateTime Mult(DateTime lhs, DateTime rhs)
        {
            return new DateTime(lhs.Ticks * rhs.Ticks);
        }

        public DateTime Mult(DateTime lhs, double rhs)
        {
            return new DateTime((long) (lhs.Ticks * rhs));
        }

        public DateTime Add(DateTime lhs, DateTime rhs)
        {
            return new DateTime(lhs.Ticks + rhs.Ticks);
        }

        public DateTime Inc(ref DateTime value)
        {
            return new DateTime(value.Ticks + 1);
        }
        public DateTime Dec(ref DateTime value)
        {
            return new DateTime(value.Ticks - 1);
        }
    }
}
