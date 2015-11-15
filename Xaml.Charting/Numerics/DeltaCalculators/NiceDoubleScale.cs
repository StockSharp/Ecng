// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NiceDoubleScale.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics
{
    internal class NiceDoubleScale : INiceScale
    {
        protected static uint _maxTicks;
        protected readonly int _minorsPerMajor;

        protected double _minDelta;
        protected double _maxDelta;

        protected DoubleRange _niceRange;

        private double _range = double.NaN;
        private double _tickSpacing;
        
        internal NiceDoubleScale(double min, double max, int minorsPerMajor, uint maxTicks = 10)
        {
            if (minorsPerMajor < 1)
            {
                throw new ArgumentException("MinorsPerMajor must be greater than or equal to 2");
            }

            _minDelta = min;
            _maxDelta = max;

            _minorsPerMajor = minorsPerMajor;
            _maxTicks = maxTicks;
        }

        public DoubleAxisDelta TickSpacing
        {
            get { return new DoubleAxisDelta(_minDelta, _maxDelta); }
        }

        internal DoubleRange NiceRange
        {
            get { return _niceRange; }
        }

        public virtual void CalculateDelta()
        {
            var maxTicks = (uint)Math.Max((int)_maxTicks - 1, 1);
            _range = NiceNum(_maxDelta - _minDelta, false);            
            _tickSpacing = NiceNum(_range/maxTicks, true);

            var niceMin = Math.Floor(_minDelta / _tickSpacing) * _tickSpacing;
            var niceMax = Math.Ceiling(_maxDelta / _tickSpacing) * _tickSpacing;

            _maxDelta = _tickSpacing;
            _minDelta = _tickSpacing/_minorsPerMajor;

            _niceRange = new DoubleRange(niceMin, niceMax);
        }

        /// <summary>
        /// Returns a "nice" number approximately equal to the range bounds. 
        /// Rounds the number if round = true. 
        /// Takes the ceiling if round = false
        /// </summary>
        /// <param name="range"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        protected virtual double NiceNum(double range, bool round)
        {
            // Exponent of range
            double exponent = range > 0 ? Math.Floor(Math.Log10(range)) : 0;

            // Fractional part of range
            double fraction = (range/Math.Pow(10, exponent)).RoundOff(1, MidpointRounding.AwayFromZero);

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