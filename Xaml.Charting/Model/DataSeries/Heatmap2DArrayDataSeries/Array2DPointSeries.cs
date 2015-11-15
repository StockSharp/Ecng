// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// Array2DPointSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Resamples 2D data in X dimension before passing it into renderer
    /// </summary>
    internal class Array2DPointSeries<TX, TY> : GenericPointSeriesBase<XyySeriesPoint>
        where TX : IComparable
        where TY : IComparable
    {
        private readonly IHeatmap2DArrayDataSeries _dataSeries;
        private readonly Func<int, TX> _xMapping;
        private readonly Func<int, TY> _yMapping;

        public Array2DPointSeries(IHeatmap2DArrayDataSeries dataSeries, Func<int, TX> xMapping, Func<int, TY> yMapping) : base(null)
        {
            _xMapping = xMapping;
            _yMapping = yMapping;
            _dataSeries = dataSeries;
        }

        public override IPoint this[int index] // index is in X dimension
        {
            get
            {
                return new Array2DSegment<TX, TY>(_dataSeries, _xMapping, _yMapping, index);
            }
        }
        /// <summary>
        /// count of items in X dimension
        /// </summary>
        public override int Count
        {
            get { return _dataSeries.ArrayWidth; }
        }
        /// <summary>
        /// height of heatmap (size in in Y dimension)
        /// </summary>
        public override DoubleRange GetYRange()
        {
            return new DoubleRange(_yMapping(0).ToDouble(), _yMapping(_dataSeries.ArrayHeight).ToDouble());
        }
    }
}