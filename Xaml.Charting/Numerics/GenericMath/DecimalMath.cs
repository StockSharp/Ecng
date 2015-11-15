// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DecimalMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class DecimalMath : IMath<decimal>
    {
        public decimal MinValue
        {
            get { return decimal.MinValue; }
        }

        public decimal MaxValue
        {
            get { return decimal.MaxValue; }
        }

        public decimal ZeroValue
        {
            get { return 0; }
        }

        public decimal Max(decimal a, decimal b)
        {
            return a > b ? a : b;
        }

        public decimal Min(decimal a, decimal b)
        {
            return a < b ? a : b;
        }

        public decimal MinGreaterThan(decimal floor, decimal a, decimal b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(decimal value)
        {
            return false;
        }

        public decimal Subtract(decimal a, decimal b)
        {
            return a - b;
        }

        public decimal Abs(decimal a)
        {
            return Math.Abs(a);
        }

        public double ToDouble(decimal value)
        {
            return (double) value;
        }

        public decimal Mult(decimal lhs, decimal rhs)
        {
            return lhs*rhs;
        }

        public decimal Mult(decimal lhs, double rhs)
        {
            return lhs * (decimal) rhs;
        }

        public decimal Add(decimal lhs, decimal rhs)
        {
            return lhs + rhs;
        }

        public decimal Inc(ref decimal value)
        {
            return ++value;
        }
        public decimal Dec(ref decimal value)
        {
            return --value;
        }
    }
}
