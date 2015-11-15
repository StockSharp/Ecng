// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastCandlestickRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides Fast Candlestick series rendering, however makes the assumption that all X-Data is evenly spaced. Gaps in the data are collapsed
    /// </summary>
    /// <remarks>In order to render data as a <see cref="FastCandlestickRenderableSeries"/>, the input <see cref="IDataSeries{Tx,Ty}"/> 
    /// must have OHLC data appended via the <see cref="IDataSeries{Tx,Ty}"/> Append method</remarks>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastCandlestickRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// Defines the UpWickColor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty UpWickColorProperty = DependencyProperty.Register("UpWickColor", typeof(Color), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(ColorConstants.White, OnInvalidateParentSurface));
        /// <summary>
        /// Defines the DownWickColor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DownWickColorProperty = DependencyProperty.Register("DownWickColor", typeof(Color), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(ColorConstants.SteelBlue, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the UpBodyBrush DependencyProperty
        /// </summary>
        public static readonly DependencyProperty UpBodyBrushProperty = DependencyProperty.Register("UpBodyBrush", typeof(Brush), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(new SolidColorBrush(ColorConstants.Transparent), OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DownBodyBrush DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DownBodyBrushProperty = DependencyProperty.Register("DownBodyBrush", typeof(Brush), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(new SolidColorBrush(ColorConstants.SteelBlue), OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DataPointWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DataPointWidthProperty = DependencyProperty.Register("DataPointWidth", typeof(double), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(0.8, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the UpBodyColor DependencyProperty
        /// </summary>
        [Obsolete("We're sorry! FastCandlestickRenderableSeries.UpBodyColor is obsolete, please use UpBodyBrush instead", true)]
        public static readonly DependencyProperty UpBodyColorProperty = DependencyProperty.Register("UpBodyColor", typeof(Color), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(ColorConstants.Transparent, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the DownBodyColor DependencyProperty
        /// </summary>
        [Obsolete("We're sorry! FastCandlestickRenderableSeries.DownBodyColor is obsolete, please use DownBodyBrush instead", true)]
        public static readonly DependencyProperty DownBodyColorProperty = DependencyProperty.Register("DownBodyColor", typeof(Color), typeof(FastCandlestickRenderableSeries), new PropertyMetadata(ColorConstants.SteelBlue, OnInvalidateParentSurface));

        private IPen2D _upWickPen;
        private IPen2D _downWickPen;
        private IBrush2D _upBodyBrush;
        private IBrush2D _downBodyBrush;
        private int _candleWidth;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastCandlestickRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FastCandlestickRenderableSeries()
        {
            DefaultStyleKey = typeof(FastCandlestickRenderableSeries);
            ResamplingMode = ResamplingMode.Mid;
        }

        /// <summary>
        /// Gets or sets the DataPointWidth, a value between 0.0 and 1.0 which defines the fraction of available space each column should occupy
        /// </summary>
        public double DataPointWidth
        {
            get { return (double)GetValue(DataPointWidthProperty); }
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
            get { return (Color)GetValue(DownWickColorProperty); }
            set { SetValue(DownWickColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Color used for candle body on up-candles (close &gt; open)
        /// </summary>
        [Obsolete("We're sorry! FastCandlestickRenderableSeries.UpBodyColor is obsolete, please use UpBodyBrush instead", true)]
        public Color UpBodyColor
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        /// <summary>
        /// Gets or sets the Brush used for candle body on up-candles (close &gt; open). If null, UpBodyColor property is used
        /// </summary>
        public Brush UpBodyBrush
        {
            get { return (Brush)GetValue(UpBodyBrushProperty); }
            set { SetValue(UpBodyBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Color used for candle body on down-candles (close &lt; open)
        /// </summary>
        [Obsolete("We're sorry! FastCandlestickRenderableSeries.DownBodyColor is obsolete, please use DownBodyBrush instead", true)]
        public Color DownBodyColor
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        /// <summary>
        /// Gets or sets the Brush used for candle body on up-candles (close &gt; open). If null, UpBodyColor property is used
        /// </summary>
        public Brush DownBodyBrush
        {
            get { return (Brush)GetValue(DownBodyBrushProperty); }
            set { SetValue(DownBodyBrushProperty, value); }
        }

        /// <summary>
        /// Called when resampling mode changes
        /// </summary>
        protected override void OnResamplingModeChanged()
        {
            // Force Mid if not already Mid
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
            return _candleWidth;
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
                           (DownWickColor.A != 0 && StrokeThickness > 0) ||
                           UpBodyBrush != null || DownBodyBrush != null);
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

            var indicesRange = CurrentRenderPassData.PointRange;

            using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))
            using (_upWickPen = penManager.GetPen(UpWickColor))
            using (_downWickPen = penManager.GetPen(DownWickColor))
            {
                _upBodyBrush = CreateBrush(renderContext, UpBodyBrush);
                _downBodyBrush = CreateBrush(renderContext, DownBodyBrush);

                renderContext.DisposeResourceAfterDraw(_upBodyBrush);
                renderContext.DisposeResourceAfterDraw(_downBodyBrush);

                renderContext.SetPrimitvesCachingEnabled(true);
                if (PointResamplerBase.RequiresReduction(ResamplingMode, indicesRange,
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

        private IBrush2D CreateBrush(IRenderContext2D renderContext, Brush wpfBrush)
        {
            var solidBrush = wpfBrush as SolidColorBrush;

            var customBrush = solidBrush != null
                ? renderContext.CreateBrush(solidBrush.Color, Opacity)
                : renderContext.CreateBrush(wpfBrush, Opacity, TextureMappingMode.PerPrimitive);

            return customBrush;
        }

        private void DrawReduced(IRenderContext2D renderContext, IRenderPassData renderPassData, IPenManager penManager)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;

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
                
                var x1 = renderPassData.XCoordinateCalculator.GetCoordinate(ohlcPoint.X);

                var y1 = renderPassData.YCoordinateCalculator.GetCoordinate(ohlcPoint.YValues.High);
                var y2 = renderPassData.YCoordinateCalculator.GetCoordinate(ohlcPoint.YValues.Low);

                var open = ohlcPoint.YValues.Open;
                var close = ohlcPoint.YValues.Close;

                bool isUp = close >= open;

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

                drawingHelper.DrawLine(TransformPoint(new Point(x1, y1), isVerticalChart), TransformPoint(new Point(x1, y2), isVerticalChart), wickPen);
            }
        }

        private void DrawVanilla(IRenderContext2D renderContext, IRenderPassData renderPassData, IPenManager penManager)
        {
            var isVerticalChart = renderPassData.IsVerticalChart;
            var gradientRotationAngle = GetChartRotationAngle(renderPassData);            
            var ohlcPoints = CurrentRenderPassData.PointSeries;

            int setCount = ohlcPoints.Count;
            if (setCount == 1)
                return;

            _candleWidth = GetDatapointWidth(renderPassData.XCoordinateCalculator, ohlcPoints, DataPointWidth);
            _candleWidth = (_candleWidth > 1 && _candleWidth % 2 == 0) ? _candleWidth - 1 : _candleWidth;

            var paletteProvider = PaletteProvider;
            
            var drawingHelper = SeriesDrawingHelpersFactory.GetSeriesDrawingHelper(renderContext, CurrentRenderPassData);

            GenericPoint2D<OhlcSeriesPoint> ohlcPoint = null;
            for(int i = 0; i < setCount; i++)
            {
                ohlcPoint = ohlcPoints[i] as GenericPoint2D<OhlcSeriesPoint>;

                var open = ohlcPoint.YValues.Open;
                var high = ohlcPoint.YValues.High;
                var close = ohlcPoint.YValues.Close;
                var low = ohlcPoint.YValues.Low;
                bool isUp = close >= open;

                var xCentre = renderPassData.XCoordinateCalculator.GetCoordinate(ohlcPoint.X).ClipToIntValue();
                var xLeft = (xCentre - (_candleWidth * 0.5)).ClipToIntValue();
                var xRight = (xCentre + (_candleWidth * 0.5)).ClipToIntValue();

                var yOpen = renderPassData.YCoordinateCalculator.GetCoordinate(isUp ? close : open).ClipToIntValue();
                var yHigh = renderPassData.YCoordinateCalculator.GetCoordinate(high).ClipToIntValue();
                var yLow = renderPassData.YCoordinateCalculator.GetCoordinate(low).ClipToIntValue();
                var yClose = renderPassData.YCoordinateCalculator.GetCoordinate(isUp ? open : close).ClipToIntValue();
                
                var wickPen = isUp ? _upWickPen : _downWickPen;
                var bodyBrush = isUp ? _upBodyBrush : _downBodyBrush;

                if (paletteProvider != null)
                {
                    var overrideColor = paletteProvider.OverrideColor(this, ohlcPoint.X, open, high, low, close);
                    if (overrideColor.HasValue)
                    {
                        wickPen = penManager.GetPen(overrideColor.Value);
                        bodyBrush = renderContext.CreateBrush(overrideColor.Value, Opacity);
                    }
                }

                // Draw candle wick - 2 lines for top and bottom wicks
                drawingHelper.DrawLine(TransformPoint(new Point(xCentre, yHigh), isVerticalChart), TransformPoint(new Point(xCentre, yOpen), isVerticalChart), wickPen);
                drawingHelper.DrawLine(TransformPoint(new Point(xCentre, yClose), isVerticalChart), TransformPoint(new Point(xCentre, yLow), isVerticalChart), wickPen);

                // Ensure y1 is always smaller than y2
                if (isUp) { NumberUtil.Swap(ref yOpen, ref yClose); }
              
                // Draw candle body
                drawingHelper.DrawBox( 
                    TransformPoint(new Point(xLeft, yOpen), isVerticalChart), 
                    TransformPoint(new Point(xRight, yClose), isVerticalChart),
                    bodyBrush,wickPen,
                    gradientRotationAngle);
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