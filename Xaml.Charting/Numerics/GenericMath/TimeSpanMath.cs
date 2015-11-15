// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class TimeSpanMath : IMath<TimeSpan>
    {
        public TimeSpan MinValue
        {
            get { return TimeSpan.MinValue; }
        }

        public TimeSpan MaxValue
        {
            get { return TimeSpan.MaxValue; }
        }

        public TimeSpan ZeroValue
        {
            get { return TimeSpan.Zero; }
        }

        public TimeSpan Max(TimeSpan a, TimeSpan b)
        {
            return a.Ticks > b.Ticks ? a : b;
        }

        public TimeSpan Min(TimeSpan a, TimeSpan b)
        {
            return a.Ticks < b.Ticks ? a : b;
        }

        public TimeSpan MinGreaterThan(TimeSpan floor, TimeSpan a, TimeSpan b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(TimeSpan value)
        {
            return false;
        }

        public TimeSpan Subtract(TimeSpan a, TimeSpan b)
        {
            return a - b;
        }

        public TimeSpan Abs(TimeSpan a)
        {
            return a;
        }

        public double ToDouble(TimeSpan value)
        {
            return value.Ticks;
        }

        public TimeSpan Mult(TimeSpan lhs, TimeSpan rhs)
        {
            return new TimeSpan(lhs.Ticks * rhs.Ticks);
        }

        public TimeSpan Mult(TimeSpan lhs, double rhs)
        {
            return new TimeSpan((long)(lhs.Ticks * rhs));
        }

        public TimeSpan Add(TimeSpan lhs, TimeSpan rhs)
        {
            return new TimeSpan(lhs.Ticks + rhs.Ticks);
        }

        public TimeSpan Inc(ref TimeSpan value)
        {
            return new TimeSpan(value.Ticks + 1);
        }
        public TimeSpan Dec(ref TimeSpan value)
        {
            return new TimeSpan(value.Ticks - 1);
        }
    }
}