// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
//  
// NiceDateScale.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics
{
    internal class NiceDateScale : INiceScale
    {
        private static readonly double _maxTicks = 10;
        private readonly DateTime _min;
        private readonly DateTime _max;
        private double _range = double.NaN;
        private double _tickSpacing;
        private DateRange _niceRange;

        internal NiceDateScale(DateTime min, DateTime max)
        {
            _min = min;
            _max = max;
            Calculate();
        }

        internal TimeSpanDelta TickSpacing
        {
            get { return new TimeSpanDelta(TimeSpan.Zero, TimeSpan.Zero); }
        }

        private void Calculate()
        {
//            _range = NiceNum(_max - _min, false);
//            _tickSpacing = NiceNum(_range/(_maxTicks - 1), true);
//            double niceMin = Math.Floor(_min/_tickSpacing)*_tickSpacing;
//            double niceMax = Math.Ceiling(_max/_tickSpacing)*_tickSpacing;
//            _niceRange = new DateAxisDelta(niceMin, niceMax);
        }

        /// <summary>
        /// Returns a "nice" number approximately equal to the range bounds. 
        /// Rounds the number if round = true. 
        /// Takes the ceiling if round = false
        /// </summary>
        /// <param name="range"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        private double NiceNum(double range, bool round)
        {
            // Exponent of range
            double exponent = Math.Floor(Math.Log10(range));

            // Fractional part of range
            double fraction = range/Math.Pow(10, exponent);

            // Nice, rounded fraction
            double niceFraction = double.NaN;

            if(round)
            {
                if (fraction < 1.5) { niceFraction = 1; }
                else if (fraction < 3) { niceFraction = 2; }
                else if (fraction < 7) { niceFraction = 5; }
                else { niceFraction = 10; }
            }
            else
            {
                if (fraction <= 1) { niceFraction = 1; }
                else if (fraction <= 2) { niceFraction = 2; }
                else if (fraction <= 5) { niceFraction = 5; }
                else { niceFraction = 10; }
            }

            return niceFraction*Math.Pow(10, exponent);
        }
    }
}