// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NiceLongScale.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Numerics
{
    internal class NiceLongScale
    {
        private static uint _maxTicks;
        private readonly long _min;
        private readonly long _max;
        private readonly int _minorsPerMajor;
        private long _tickSpacing;
        private Tuple<long, long> _niceRange;

        internal NiceLongScale(long min, long max, int minorsPerMajor, uint maxTicks = 10)
        {
            if (minorsPerMajor < 1)
            {
                throw new ArgumentException("MinorsPerMajor must be greater than or equal to 2");
            }
            _min = min;
            _max = max;
            _minorsPerMajor = minorsPerMajor;
            _maxTicks = maxTicks;

            Calculate();
        }

        public Tuple<long, long> TickSpacing
        {
            get { return new Tuple<long, long>(_tickSpacing / _minorsPerMajor, _tickSpacing); }
        }

        internal Tuple<long, long> NiceRange
        {
            get { return _niceRange; }
        }

        private void Calculate()
        {
            var maxTicks = (uint)Math.Max((int)_maxTicks - 1, 1);

            var range = NiceNum(_max - _min, false);
            range = range / maxTicks;
            _tickSpacing = range > 0 ? NiceNum(range, true) : 1;

            long niceMin = (_min / _tickSpacing) * _tickSpacing;
            long niceMax = (_max / _tickSpacing) * _tickSpacing;
            _niceRange = Tuple.Create(niceMin, niceMax);
        }

        /// <summary>
        /// Returns a "nice" number approximately equal to the range bounds. 
        /// Rounds the number if round = true. 
        /// Takes the ceiling if round = false
        /// </summary>
        /// <param name="range"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        private long NiceNum(long range, bool round)
        {
            // Exponent of range
            double exponent = Math.Floor(Math.Log10(range));

            // Fractional part of range
            double fraction = (range / Math.Pow(10, exponent)).RoundOff(1, MidpointRounding.AwayFromZero);

            // Nice, rounded fraction
            long niceFraction;

            if (round)
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

            return niceFraction * (long)Math.Pow(10, exponent);
        }
    }
}
