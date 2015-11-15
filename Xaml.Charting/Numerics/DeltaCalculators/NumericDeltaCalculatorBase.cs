// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
// 
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NumericDeltaCalculatorBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics
{
    internal abstract class NumericDeltaCalculatorBase: IDeltaCalculator
    {
        private const uint DefaultTicksCount = 10;

        /// <summary>
        /// Given an absolute Axis Min and Max, returns a TickRange instance containing sensible MinorDelta and MajorDelta values
        /// </summary>
        public IAxisDelta<double> GetDeltaFromRange(double min, double max, int minorsPerMajor, uint maxTicks = DefaultTicksCount)
        {
            Guard.ArgumentIsRealNumber(min);
            Guard.ArgumentIsRealNumber(max);

            var scaleCalculator = GetScale(min, max, minorsPerMajor, maxTicks);
            scaleCalculator.CalculateDelta();

            return scaleCalculator.TickSpacing;
        }

        protected virtual INiceScale GetScale(double min, double max, int minorsPerMajor, uint maxTicks)
        {
            return new NiceDoubleScale(min, max, minorsPerMajor, maxTicks);
        }

        
        IAxisDelta IDeltaCalculator.GetDeltaFromRange(IComparable min, IComparable max, int minorsPerMajor, uint maxTicks)
        {
            return GetDeltaFromRange(min.ToDouble(), max.ToDouble(), minorsPerMajor, maxTicks);
        }

    }
}
