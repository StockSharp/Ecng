// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// OhlcPointSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// An internal concept - the <see cref="IPointSeries"/> provides a sequence of <see cref="ISeriesPoint{T}"/> derived 
    /// types, which represent resampled data immediately before rendering. 
    /// 
    /// The <see cref="OhlcPointSeries"/> specifically is used when resampling and rendering OhlcDataSeries
    /// </summary>
    /// <seealso cref="FastCandlestickRenderableSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="OhlcSeriesPoint"/>
    public class OhlcPointSeries : GenericPointSeriesBase<OhlcSeriesPoint>
    {
        private readonly IPointSeries _openPoints;
        private readonly IPointSeries _highPoints;
        private readonly IPointSeries _lowPoints;
        private readonly IPointSeries _closePoints;

        private bool _useXIndices;
        private int _baseIndex;
        private readonly UncheckedList<double> _xValues;
        private readonly UncheckedList<double> _openValues;
        private readonly UncheckedList<double> _highValues;
        private readonly UncheckedList<double> _lowValues;
        private readonly UncheckedList<double> _closeValues;


        public OhlcPointSeries(IPointSeries openPoints, IPointSeries highPoints, IPointSeries lowPoints, IPointSeries closePoints) : base(closePoints)
        {
            _openPoints = openPoints;
            _highPoints = highPoints;
            _lowPoints = lowPoints;
            _closePoints = closePoints;

            var open2dPoints = openPoints as IPoint2DListSeries;
            _openValues = open2dPoints != null ? open2dPoints.YValues : null;

            var high2dPoints = highPoints as IPoint2DListSeries;
            _highValues = high2dPoints != null ? high2dPoints.YValues : null;

            var low2dPoints = lowPoints as IPoint2DListSeries;
            _lowValues = low2dPoints != null ? low2dPoints.YValues : null;

            var close2dPoints = closePoints as IPoint2DListSeries;
            if (close2dPoints != null)
            {
                _xValues = close2dPoints.XValues;
                _useXIndices = _xValues == null;
                _baseIndex = close2dPoints.XBaseIndex;
                _closeValues = close2dPoints.YValues;
            }
        }

        public override int Count
        {
            get { return _closePoints.Count; }
        }

        public override IPoint this[int index]
        {
            get
            {
                var pt = new GenericPoint2D<OhlcSeriesPoint>(
                    _useXIndices ? index + _baseIndex : (_xValues != null ? _xValues[index] : _closePoints[index].X),
                    new OhlcSeriesPoint(
                         _openValues != null ? _openValues[index] : _openPoints[index].Y,
                        _highValues != null ? _highValues[index] : _highPoints[index].Y,
                        _lowValues != null ? _lowValues[index] : _lowPoints[index].Y,
                        _closeValues != null ? _closeValues[index] : _closePoints[index].Y));

                return pt;
            }
        }

        public override DoubleRange GetYRange()
        {
            int count = Count;

            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = 0; i < count; i++)
            {
                double currentHigh = _highValues != null ? _highValues[i] : _highPoints[i].Y;
                double currentLow = _lowValues != null ? _lowValues[i] : _lowPoints[i].Y;
                if (double.IsNaN(currentHigh) || double.IsNaN(currentLow)) continue;
                min = min < currentLow ? min : currentLow;
                max = max > currentHigh ? max : currentHigh;
                count = _highPoints.Count;
            }

            return new DoubleRange(min, max);
        }
    }
}
