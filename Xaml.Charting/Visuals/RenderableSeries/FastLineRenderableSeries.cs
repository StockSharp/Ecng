// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastLineRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Enumeration Constants to define how double.NaN is treated in the <seealso cref="FastLineRenderableSeries"/>
    /// </summary>
    public enum LineDrawMode
    {
        /// <summary>
        /// double.NaN gaps are treated as closed lines
        /// </summary>
        ClosedLines,

        /// <summary>
        /// double.NaN gaps are rendered as gaps
        /// </summary>
        Gaps,
    }

    /// <summary>
    /// Enumeration constants to define how <see cref="OhlcDataSeries"/> is drawn.
    /// </summary>
    public enum OhlcLineDrawMode { Open, High, Low, Close }

    /// <summary>
    /// Defines a Line renderable series, supporting solid, stroked (thickness 1+) lines, dashed lines <seealso cref="FastLineRenderableSeries.StrokeDashArray"/> and
    /// optional Point-markers <seealso cref="BaseRenderableSeries.PointMarker"/>
    /// </summary>
    /// <remarks>
    /// A RenderableSeries has a <see cref="IDataSeries"/> data-source, 
    /// may have a <see cref="BasePointMarker"/> point-marker, and draws onto a specific <see cref="RenderSurfaceBase"/> using the <see cref="IRenderContext2D"/>. 
    /// A given <see cref="UltrachartSurface"/> may have 0..N <see cref="BaseRenderableSeries"/>, each of which may map to, or share a <see cref="IDataSeries"/>
    /// </remarks>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="BasePointMarker"/>
    /// <seealso cref="IRenderContext2D"/>
    /// <seealso cref="FastLineRenderableSeries"/>
    /// <seealso cref="FastMountainRenderableSeries"/>
    /// <seealso cref="FastColumnRenderableSeries"/>
    /// <seealso cref="FastOhlcRenderableSeries"/>
    /// <seealso cref="XyScatterRenderableSeries"/>
    /// <seealso cref="FastCandlestickRenderableSeries"/>
    /// <seealso cref="FastBandRenderableSeries"/>
    /// <seealso cref="FastErrorBarsRenderableSeries"/>
    /// <seealso cref="FastBoxPlotRenderableSeries"/>
    /// <seealso cref="FastBubbleRenderableSeries"/>
    /// <seealso cref="FastHeatMapRenderableSeries"/>
    /// <seealso cref="StackedColumnRenderableSeries"/>
    /// <seealso cref="StackedMountainRenderableSeries"/>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastLineRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// Defines the IsDigitalLine DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsDigitalLineProperty = DependencyProperty.Register("IsDigitalLine", typeof(bool), typeof(FastLineRenderableSeries),
            new PropertyMetadata(false, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the StrokeDashArray DependencyProperty
        /// </summary>
        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register("StrokeDashArray", typeof(double[]), typeof(FastLineRenderableSeries),
            new PropertyMetadata(null, OnInvalidateParentSurface));

        /// <summary>
        /// Defines the OhlcDrawMode DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OhlcDrawModeProperty = DependencyProperty.Register("OhlcDrawMode", typeof(OhlcLineDrawMode), typeof(FastLineRenderableSeries), new PropertyMetadata(OhlcLineDrawMode.Close, OnInvalidateParentSurface));
        
        /// <summary>
        /// Initializes a new instance of the <seealso cref="FastLineRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FastLineRenderableSeries()
        {
            DefaultStyleKey = typeof (FastLineRenderableSeries);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this line series is a digital (step) line
        /// </summary>
        public bool IsDigitalLine
        {
            get { return (bool) GetValue(IsDigitalLineProperty); }
            set { SetValue(IsDigitalLineProperty, value); }
        }

        /// <summary>
        /// Gets or sets a StrokeDashArray property, used to define a dashed line. See the MSDN Documentation for 
        /// <see cref="Shape.StrokeDashArray"/> as this property attempts to mimic the same behaviour
        /// </summary>
        [TypeConverter(typeof(StringToDoubleArrayTypeConverter))]
        public double[] StrokeDashArray
        {
            get { return (double[])GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }

        /// <summary>
        /// OHLC line draw mode.
        /// </summary>
        public OhlcLineDrawMode OhlcDrawMode
        {
            get { return (OhlcLineDrawMode) GetValue(OhlcDrawModeProperty); }
            set { SetValue(OhlcDrawModeProperty, value); }
        }

        public override object PointSeriesArg => OhlcDrawMode;

        /// <summary>
        /// Called when the <see cref="BaseRenderableSeries.SeriesColor"/> dependency property changes. Allows derived types to do caching 
        /// </summary>
        protected override void OnSeriesColorChanged()
        {
            // TODO: CACHE PEN
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="BaseRenderableSeries"/> is valid for drawing.
        /// </summary>
        protected override bool GetIsValidForDrawing()
        {
            var isValid = base.GetIsValidForDrawing() &&
                          ((SeriesColor.A != 0 && StrokeThickness > 0) || PointMarker != null);
            return isValid;
        }

        protected override HitTestInfo ToHitTestInfoImpl(int nearestDataPointIndex)
        {
            var info = base.ToHitTestInfoImpl(nearestDataPointIndex);

            if(info.DataSeriesType != DataSeriesType.Ohlc && info.DataSeriesType != DataSeriesType.Hlc)
                return info;

            info.DataSeriesType = DataSeriesType.Xy;
            switch (OhlcDrawMode)
            {
                case OhlcLineDrawMode.Open:
                    info.YValue = info.OpenValue;
                    break;
                case OhlcLineDrawMode.High:
                    info.YValue = info.HighValue;
                    break;
                case OhlcLineDrawMode.Low:
                    info.YValue = info.LowValue;
                    break;
                case OhlcLineDrawMode.Close:
                    info.YValue = info.CloseValue;
                    break;
            }

            return info;
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {            
            var pointSeries = CurrentRenderPassData.PointSeries;

            // Render the line series 
            bool isPalettedLine = PaletteProvider != null;
            var lineColor = SeriesColor;          

            var linesPathFactory = SeriesDrawingHelpersFactory.GetLinesPathFactory(renderContext, CurrentRenderPassData);

            if (isPalettedLine)
            {
                // If the line is paletted, use the penned DrawLines technique
                using (var penManager = new PenManager(renderContext, AntiAliasing, StrokeThickness, Opacity))
                {
                    Func<double, double, IPen2D> createPenFunc = (x, y) =>
                    {
                        var color = PaletteProvider.GetColor(this, x, y) ?? lineColor;
                        return penManager.GetPen(color);
                    };

                    FastLinesHelper.IterateLines(linesPathFactory, createPenFunc, pointSeries,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator,
                        IsDigitalLine,
                        DrawNaNAs == LineDrawMode.ClosedLines);
                }
            }
            else
            {
                // Else, simply draw a non-paletted line                
                using (var pen = renderContext.CreatePen(SeriesColor, AntiAliasing, StrokeThickness, Opacity, StrokeDashArray))
                {
                    FastLinesHelper.IterateLines(linesPathFactory, pen, pointSeries,
                        CurrentRenderPassData.XCoordinateCalculator,
                        CurrentRenderPassData.YCoordinateCalculator,
                        IsDigitalLine, 
                        DrawNaNAs == LineDrawMode.ClosedLines);
                }
            }

            // Render the PointMarker (optional) 
            var pointMarker = GetPointMarker();
            if (pointMarker != null)
            {
                var pointMarkerPathFactory = SeriesDrawingHelpersFactory.GetPointMarkerPathFactory(renderContext, CurrentRenderPassData, pointMarker);

                FastPointsHelper.IteratePoints(pointMarkerPathFactory,
                    pointSeries,
                    CurrentRenderPassData.XCoordinateCalculator,
                    CurrentRenderPassData.YCoordinateCalculator);
            }
        }
    }    
}