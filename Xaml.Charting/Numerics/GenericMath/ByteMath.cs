// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ByteMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class ByteMath : IMath<byte>
    {
        public byte MinValue
        {
            get { return byte.MinValue; }
        }

        public byte MaxValue
        {
            get { return byte.MaxValue; }
        }

        public byte ZeroValue
        {
            get { return 0; }
        }

        public byte Max(byte a, byte b)
        {
            return a > b ? a : b;
        }

        public byte Min(byte a, byte b)
        {
            return a < b ? a : b;
        }

        public byte MinGreaterThan(byte floor, byte a, byte b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(byte value)
        {
            return false;
        }

        public byte Subtract(byte a, byte b)
        {
            return (byte) (a - b);
        }

        public byte Abs(byte a)
        {
            return a;
        }

        public double ToDouble(byte value)
        {
            return value;
        }

        public byte Mult(byte lhs, byte rhs)
        {
            return (byte) (lhs * rhs);
        }

        public byte Mult(byte lhs, double rhs)
        {
            return (byte) (lhs*rhs);
        }

        public byte Add(byte lhs, byte rhs)
        {
            return (byte) (lhs + rhs);
        }

        public byte Inc(ref byte value)
        {
            return ++value;
        }
        public byte Dec(ref byte value)
        {
            return --value;
        }
    }
}