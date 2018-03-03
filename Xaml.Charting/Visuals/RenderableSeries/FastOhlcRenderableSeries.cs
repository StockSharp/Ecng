// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastOhlcRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides Fast Candlestick series rendering, however makes the assumption that all X-Data is evenly spaced. Gaps in the data are collapsed
    /// </summary>
    /// <remarks>In order to render data as a <see cref="FastCandlestickRenderableSeries"/>, the input <see cref="IDataSeries{Tx,Ty}"/> 
    /// must have OHLC data appended via the <see cref="IDataSeries{Tx,Ty}"/> Append method</remarks>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastOhlcRenderableSeries : BaseRenderableSeries
    {
        private int _ohlcWidth;

        /// <summary>
        /// Defines the UpWickColor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty UpWickColorProperty = DependencyProperty.Register("UpWickColor", typeof (Color), typeof (FastOhlcRenderableSeries), new PropertyMetadata(ColorConstants.White, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DownWickColor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DownWickColorProperty = DependencyProperty.Register("DownWickColor", typeof(Color), typeof(FastOhlcRenderableSeries), new PropertyMetadata(ColorConstants.SteelBlue, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DataPointWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataPointWidthProperty = DependencyProperty.Register("DataPointWidth", typeof(double), typeof(FastOhlcRenderableSeries), new PropertyMetadata(0.8, OnInvalidateParentSurface));

        protected IPen2D _upWickPen;
        protected IPen2D _downWickPen;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastCandlestickRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FastOhlcRenderableSeries()
        {
            DefaultStyleKey = typeof (FastOhlcRenderableSeries);
            ResamplingMode = ResamplingMode.Mid;
        }

        /// <summary>
        /// Gets or sets the DataPointWidth, a value between 0.0 and 1.0 which defines the fraction of available space each column should occupy
        /// </summary>
        public double DataPointWidth
        {
            get { return (double) GetValue(DataPointWidthProperty); }
            set { SetValue(DataPointWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Color used for wicks and outlines on up-candles (close &gt; open)
        /// </summary>
        public Color UpWickColor
        {
            get { return (Color) GetValue(UpWickColorProperty); }
            set { SetValue(UpWickColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Color used for wicks and outlines on down-candles (close &lt; open)
        /// </summary>
        public Color DownWickColor
        {
            get { return (Color) GetValue(DownWickColorProperty); }
            set { SetValue(DownWickColorProperty, value); }
        }

        /// <summary>
        /// Called when the resampling mode changes
        /// </summary>
        protected override void OnResamplingModeChanged()
        {
            // Force Mid as resampling mode if not already Mid
            ResamplingMode = ResamplingMode.Mid;
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var nearestHitPoint = base.HitTestInternal(rawPoint, hitTestRadius, false);

            nearestHitPoint = HitTestSeriesWithBody(rawPoint, nearestHitPoint, hitTestRadius);

            var distance = CurrentRenderPassData.IsVerticalChart
                ? Math.Abs(nearestHitPoint.HitTestPoint.Y - rawPoint.Y)
                : Math.Abs(nearestHitPoint.HitTestPoint.X - rawPoint.X);

            if (!nearestHitPoint.IsWithinDataBounds)
            {
                var isVerticalHit = distance < GetSeriesBodyWidth(nearestHitPoint) / DataPointWidth / 2;
                nearestHitPoint.IsWithinDataBounds = nearestHitPoint.IsVerticalHit = isVerticalHit;
            }

            return nearestHitPoint;
        }

        protected override double GetSeriesBodyWidth(HitTestInfo nearestHitPoint)
        {
            return _ohlcWidth;
        }

        protected override double GetSeriesBodyLowerDataBound(HitTestInfo nearestHitPoint)
        {
            return nearestHitPoint.LowValue.ToDouble();
        }

        protected override double GetSeriesBodyUpperDataBound(HitTestInfo nearestHitPoint)
        {
            return nearestHitPoint.HighValue.ToDouble();
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() &&
                          ((UpWickColor.A != 0 && StrokeThickness > 0) ||
                           (DownWickColor.A != 0 && StrokeThickness > 0));
            return isValid;
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            AssertDataPointType<OhlcSeriesPoint>("OhlcDataSeries");

            using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))
            {
                _upWickPen = penManager.GetPen(UpWickColor);
                _downWickPen = penManager.GetPen(DownWickColor);

                renderContext.SetPrimitvesCachingEnabled(true);
                if (PointResamplerBase.RequiresReduction(ResamplingMode, renderPassData.PointRange,
                    (int) renderContext.ViewportSize.Width))
                {
                    DrawReduced(renderContext, renderPassData, penManager);
                }
                else
                {
                    DrawVanilla(renderContext, renderPassData, penManager);
                }
                renderContext.SetPrimitvesCachingEnabled(false);
            }
        }

        private void DrawReduced(IRenderContext2D renderContext, IRenderPassData renderPassData, IPenManager penManager)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;

            var xCoordinateCalculator = renderPassData.XCoordinateCalculator;
            var yCoordinateCalculator = renderPassData.YCoordinateCalculator;

            var ohlcPoints = CurrentRenderPassData.PointSeries;

            // Setup Constants...
            int setCount = ohlcPoints.Count;

            var paletteProvider = PaletteProvider;

            var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);

            GenericPoint2D<OhlcSeriesPoint> ohlcPoint = null;
            // Collate data into points (x1, y1, x2, y2 ...)
            for (int i = 0; i < setCount; i++)
            {
                ohlcPoint = ohlcPoints[i] as GenericPoint2D<OhlcSeriesPoint>;

                var x1 =  xCoordinateCalculator.GetCoordinate(ohlcPoint.X);

                var y1 =  yCoordinateCalculator.GetCoordinate(ohlcPoint.YValues.High);
                var y2 =  yCoordinateCalculator.GetCoordinate(ohlcPoint.YValues.Low);

                var open = ohlcPoint.YValues.Open;
                var close = ohlcPoint.YValues.Close;
                bool isUp = close >= open;

                var wickPen = isUp ? _upWickPen : _downWickPen;

                Color? overrideColor = null;
                if (paletteProvider != null)
                {
                    overrideColor = paletteProvider.OverrideColor(
                        this,
                        ohlcPoint.X,
                        ohlcPoint.YValues.Open,
                        ohlcPoint.YValues.High,
                        ohlcPoint.YValues.Low,
                        ohlcPoint.YValues.Close);

                    if (overrideColor.HasValue)
                        wickPen = penManager.GetPen(overrideColor.Value);
                }

                drawingHelper.DrawLine(
                    TransformPoint(new Point(x1, y1), isVerticalChart),
                    TransformPoint(new Point(x1, y2), isVerticalChart), wickPen);

                if (overrideColor.HasValue) wickPen.Dispose();
            }
        }

        protected virtual void DrawVanilla(IRenderContext2D renderContext, IRenderPassData renderPassData, IPenManager penManager)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;
            var ohlcPoints = CurrentRenderPassData.PointSeries;

            int setCount = ohlcPoints.Count;
            if (setCount == 1)
                return;

            _ohlcWidth = GetDatapointWidth(renderPassData.XCoordinateCalculator, ohlcPoints, DataPointWidth);
            _ohlcWidth = (_ohlcWidth > 1 && _ohlcWidth % 2 == 0) ? _ohlcWidth - 1 : _ohlcWidth;

            var paletteProvider = PaletteProvider;
            
            var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);
            
            GenericPoint2D<OhlcSeriesPoint> ohlcPoint = null;
            for (int i = 0; i < setCount; i++)
            {
                ohlcPoint = ohlcPoints[i] as GenericPoint2D<OhlcSeriesPoint>;

                var open = ohlcPoint.YValues.Open;
                var high = ohlcPoint.YValues.High;
                var close = ohlcPoint.YValues.Close;
                var low = ohlcPoint.YValues.Low;
                bool isUp = close >= open;

                var xCentre = renderPassData.XCoordinateCalculator.GetCoordinate(ohlcPoint.X).ClipToIntValue();
                var xLeft = (xCentre - (_ohlcWidth * 0.5)).ClipToIntValue();
                var xRight = (xCentre + (_ohlcWidth * 0.5)).ClipToIntValue();

                var yOpen = renderPassData.YCoordinateCalculator.GetCoordinate(open).ClipToIntValue();
                var yHigh = renderPassData.YCoordinateCalculator.GetCoordinate(high).ClipToIntValue();
                var yLow = renderPassData.YCoordinateCalculator.GetCoordinate(low).ClipToIntValue();
                var yClose = renderPassData.YCoordinateCalculator.GetCoordinate(close).ClipToIntValue();

                var wickPen = isUp ? _upWickPen : _downWickPen;

                if (paletteProvider != null)
                {
                    var overrideColor = paletteProvider.OverrideColor(
                        this,
                        ohlcPoint.X,
                        ohlcPoint.YValues.Open,
                        ohlcPoint.YValues.High,
                        ohlcPoint.YValues.Low,
                        ohlcPoint.YValues.Close);

                    if (overrideColor.HasValue)
                    {
                        wickPen = penManager.GetPen(overrideColor.Value);
                    }
                }

                drawingHelper.DrawLine(TransformPoint(new Point(xLeft, yOpen), isVerticalChart),
                    TransformPoint(new Point(xCentre, yOpen), isVerticalChart), wickPen);
                drawingHelper.DrawLine(TransformPoint(new Point(xCentre, yClose), isVerticalChart),
                    TransformPoint(new Point(xRight, yClose), isVerticalChart), wickPen);
                drawingHelper.DrawLine(TransformPoint(new Point(xCentre, yHigh), isVerticalChart),
                    TransformPoint(new Point(xCentre, yLow), isVerticalChart), wickPen);
            }
        }

        /// <summary>
        /// Called when the <see cref="BaseRenderableSeries.DataSeries" /> property changes - i.e. a new <see cref="IDataSeries" /> has been set
        /// </summary>
        /// <param name="oldDataSeries">The old <see cref="IDataSeries" /></param>
        /// <param name="newDataSeries">The new <see cref="IDataSeries" /></param>
        /// <exception cref="System.InvalidOperationException"></exception>
        protected override void OnDataSeriesDependencyPropertyChanged(IDataSeries oldDataSeries, IDataSeries newDataSeries)
        {
            if (newDataSeries != null && !(newDataSeries is IOhlcDataSeries))
            {                
                throw new InvalidOperationException(string.Format("{0} expects a DataSeries of type {1}. Please ensure the correct data has been bound to the Renderable Series", GetType().Name, typeof(IOhlcDataSeries)));
            }

            base.OnDataSeriesDependencyPropertyChanged(oldDataSeries, newDataSeries);
        }
    }
}