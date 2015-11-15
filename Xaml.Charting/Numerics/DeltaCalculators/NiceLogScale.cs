// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NiceLogScale.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    internal class NiceLogScale : NiceDoubleScale
    {
        private readonly double _logBase;

        internal NiceLogScale(double min, double max, double logBase, int minorsPerMajor, uint maxTicks = 10)
            : base(min, max, minorsPerMajor, maxTicks)
        {
            _logBase = logBase;
        }

        public override void CalculateDelta()
        {
            double upperExponent = Math.Log(_maxDelta, _logBase);
            upperExponent = upperExponent > 0 ? Math.Ceiling(upperExponent) : Math.Floor(upperExponent);

            double lowerExponent = Math.Floor(Math.Log(Math.Abs(_minDelta), _logBase));
            double diffExp = Math.Max(Math.Abs(
                Math.Sign(_minDelta) == -1 ? upperExponent + lowerExponent : upperExponent - lowerExponent), 1);

            var range = NiceNum(upperExponent - lowerExponent, false);

            var tickSpacing = NiceNum(range / _maxTicks, true);

            _maxDelta = tickSpacing;
            _minDelta = (Math.Pow(_logBase, tickSpacing) - 1)/_minorsPerMajor;

            double niceMax = Math.Pow(_logBase, upperExponent);
            double niceMin = niceMax / Math.Pow(_logBase, diffExp);

            _niceRange = new DoubleRange(niceMin, niceMax);
        }
    }
}
