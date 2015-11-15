// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// StochasticKFilter.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Numerics.Filters
{
    public class StochasticKFilter : IFilter
    {
        private readonly int _length;
        private double _current = double.NaN;
        private double _highest = double.MinValue;
        private double _lowest = double.MaxValue;
        private readonly double[] _highs;
        private readonly double[] _lows;        

        public StochasticKFilter(int length)
        {
            _length = length;
            //_circularBuffer = new double[length];
        }

        public double Current
        {
            get { return _current; }
        }

        public IFilter PushValue(double closeValue)
        {
//            _highest = Math.Max(highValue, _highest);
//            _lowest = Math.Min(lowValue, _lowest);

            throw new NotImplementedException();
            //return 100 * (closeValue - lowValue) / (highValue - lowValue);
        }

        public IFilter UpdateValue(double value)
        {
            throw new System.NotImplementedException();
        }
    }
}