// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DoubleMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics.GenericMath
{
    internal sealed class DoubleMath : IMath<double>
    {
        public double MaxValue
        {
            get
            {
                return double.MaxValue;
            }
        }

        public double MinValue
        {
            get
            {
                return double.MinValue;
            }
        }

        public double ZeroValue
        {
            get { return 0; }
        }

        public double Max(double a, double b)
        {
            if (a.IsNaN()) return b;
            if (b.IsNaN()) return a;

            return a > b ? a : b;
        }

        public double Min(double a, double b)
        {
            if (a.IsNaN()) return b;
            if (b.IsNaN()) return a;

            return a < b ? a : b;
        }

        public double MinGreaterThan(double floor, double a, double b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(double value)
        {
            return value.IsNaN();
        }

        public double Subtract(double a, double b)
        {
            return a - b;
        }

        public double Abs(double a)
        {
            return Math.Abs(a);
        }

        public double ToDouble(double value)
        {
            return value;
        }

        public double Mult(double lhs, double rhs)
        {
            return lhs*rhs;
        }

        public double Add(double lhs, double rhs)
        {
            return lhs + rhs;
        }

        public double Inc(ref double value)
        {
            return ++value;
        }
        public double Dec(ref double value)
        {
            return --value;
        }
    }
}
