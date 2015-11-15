// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Uint64Math.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class Uint64Math : IMath<ulong>
    {
        public ulong MinValue
        {
            get { return UInt64.MinValue; }
        }

        public ulong MaxValue
        {
            get { return UInt64.MaxValue; }
        }

        public ulong ZeroValue
        {
            get { return 0; }
        }

        public ulong Max(ulong a, ulong b)
        {
            return a > b ? a : b;
        }

        public ulong Min(ulong a, ulong b)
        {
            return a < b ? a : b;
        }

        public ulong MinGreaterThan(ulong floor, ulong a, ulong b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(ulong value)
        {
            return false;
        }

        public ulong Subtract(ulong a, ulong b)
        {
            return a - b;
        }

        public ulong Abs(ulong a)
        {
            return a;
        }

        public double ToDouble(ulong value)
        {
            return value;
        }

        public ulong Mult(ulong lhs, ulong rhs)
        {
            return lhs * rhs;
        }

        public ulong Mult(ulong lhs, double rhs)
        {
            return (ulong) (lhs * rhs);
        }

        public ulong Add(ulong lhs, ulong rhs)
        {
            return lhs + rhs;
        }

        public ulong Inc(ref ulong value)
        {
            return ++value;
        }
        public ulong Dec(ref ulong value)
        {
            return --value;
        }
    }
}