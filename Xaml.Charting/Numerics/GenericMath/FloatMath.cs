// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FloatMath.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal sealed class FloatMath : IMath<float>
    {
        public float MaxValue
        {
            get { return float.MaxValue; }
        }

        public float MinValue
        {
            get { return float.MinValue; }
        }
        public float ZeroValue
        {
            get { return 0; }
        }

        public float Max(float a, float b)
        {
            if (IsNaN(a)) return b;
            if (IsNaN(b)) return a;

            return a > b ? a : b;
        }

        private bool IsDefined(float a)
        {
            return !float.IsInfinity(a) && !float.IsNaN(a);
        }

        public float Min(float a, float b)
        {
            if (IsNaN(a)) return b;
            if (IsNaN(b)) return a;

            return a < b ? a : b;
        }

        public float MinGreaterThan(float floor, float a, float b)
        {
            var min = Min(a, b);
            var max = Max(a, b);
            return min.CompareTo(floor) > 0 ? min : max;
        }

        public bool IsNaN(float value)
        {
            // Fast NaN check. 
            // NOTE: Value != Value check is intentional
            // http://stackoverflow.com/questions/3286492/can-i-improve-the-double-isnan-x-function-call-on-embedded-c
            // 

            // ReSharper disable EqualExpressionComparison
            // ReSharper disable CompareOfFloatsByEqualityOperator
#pragma warning disable 1718
            return value != value;
#pragma warning restore 1718
            // ReSharper restore CompareOfFloatsByEqualityOperator
            // ReSharper restore EqualExpressionComparison
        }

        public float Subtract(float a, float b)
        {
            return a - b;
        }

        public float Abs(float a)
        {
            return Math.Abs(a);
        }

        public double ToDouble(float value)
        {
            return value;
        }

        public float Mult(float lhs, float rhs)
        {
            return lhs * rhs;
        }

        public float Mult(float lhs, double rhs)
        {
            return (float) (lhs * rhs);
        }

        public float Add(float lhs, float rhs)
        {
            return lhs + rhs;
        }

        public float Inc(ref float value)
        {
            return ++value;
        }
        public float Dec(ref float value)
        {
            return --value;
        }
    }
}
