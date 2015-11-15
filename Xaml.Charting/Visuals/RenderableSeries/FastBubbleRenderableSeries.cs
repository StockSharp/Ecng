// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastBubbleRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines a Bubble-chart renderable series, supporting rendering of bubbles using Z data, positioned using X-Y data.
    /// Bubbles are defined using the <see cref="FastBubbleRenderableSeries.BubbleColor"/> property, but rendered as a soft-edged circle 
    /// which fades to transparent in the centre. 
    /// </summary>
    /// <remarks>
    /// The FastBubbleRenderableSeries requires a <see cref="XyzDataSeries{TX,TY,TZ}"/> data-source, 
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
    public class FastBubbleRenderableSeries : BaseRenderableSeries
    {
        /// <summary>
        /// Defines the BubbleColor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BubbleColorProperty = DependencyProperty.Register("BubbleColor", typeof(Color), typeof(FastBubbleRenderableSeries), new PropertyMetadata(default(Color), OnPropertyChanged));

        /// <summary>
        /// Defines the AutoZRange DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AutoZRangeProperty = DependencyProperty.Register("AutoZRange", typeof(bool), typeof(FastBubbleRenderableSeries), new PropertyMetadata(true, OnPropertyChanged));

        /// <summary>
        /// Defines the ZScaleFactor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ZScaleFactorProperty = DependencyProperty.Register("ZScaleFactor", typeof(double), typeof(FastBubbleRenderableSeries), new PropertyMetadata(1.0, OnPropertyChanged));

        private SmartDisposable<ISprite2D> _cachedBubble;
        private Type _typeOfRendererForCachedBubble;

        private double _maxZValue = 0;

        const double MaxBubbleSizeInPixels = 300;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="FastBubbleRenderableSeries"/> class.
        /// </summary>
        public FastBubbleRenderableSeries()
        {
            DefaultStyleKey = typeof(FastBubbleRenderableSeries);
        }

        /// <summary>
        /// Gets or sets the BubbleColor, a base colour used when rendering the bubbles as a soft-edged circle, centred on the X-Y point
        /// </summary>
        public Color BubbleColor
        {
            get { return (Color)GetValue(BubbleColorProperty); }
            set { SetValue(BubbleColorProperty, value); }
        }
        /// <summary>
        /// Gets or sets whether Z-range should be automatically scaled. If True, then depending on the XYZ points in the
        /// viewport, the size of bubbles will be scaled to fit. Else, the size of bubbles will be absolute
        /// </summary>
        public bool AutoZRange
        {
            get { return (bool)GetValue(AutoZRangeProperty); }
            set { SetValue(AutoZRangeProperty, value); }
        }
        /// <summary>
        /// Gets or sets a Z-scaling factor, equal to Pixels divided by Z-Unit
        /// </summary>
        public double ZScaleFactor
        {
            get { return (double)GetValue(ZScaleFactorProperty); }
            set { SetValue(ZScaleFactorProperty, value); }
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var nearestHitPoint = base.HitTestInternal(rawPoint, hitTestRadius, false);

            nearestHitPoint = HitTestSeriesWithBody(rawPoint, nearestHitPoint, hitTestRadius);

            return nearestHitPoint;
        }

        protected override bool IsBodyHit(Point pt, Rect boundaries, HitTestInfo nearestHitPoint)
        {
            var bubbleCenterX = nearestHitPoint.HitTestPoint.X;
            var bubbleCenterY = nearestHitPoint.HitTestPoint.Y;

            var bubbleSize = GetBubbleSize(nearestHitPoint.ZValue.ToDouble());

            boundaries = new Rect(bubbleCenterX - bubbleSize / 2, bubbleCenterY - bubbleSize / 2, bubbleSize, bubbleSize);
            var radius = bubbleSize/2;

            var dx = pt.X - (boundaries.Left + boundaries.Right) / 2;
            var dy = pt.Y - (boundaries.Top + boundaries.Bottom) / 2;
            var distanceToCenter = Math.Sqrt(dx * dx + dy * dy);

            return distanceToCenter <= radius;
        }

		/// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            // We need Xyy data to achieve a bubble chart, where X,Y0 are position and Y1 is bubble size
            base.AssertDataPointType<XyzSeriesPoint>("XyzDataSeries");

            RecreateCachedBubbles(renderContext);

            Color? paletteColor = null;
		    EnumerateBubbles(renderPassData, (x, y, z, rect) =>
		    {
		        if (PaletteProvider != null)
		        {
                    paletteColor = PaletteProvider.OverrideColor(this, x, y, z);
		        }

		        if (paletteColor.HasValue)
		        {
                    using (var pen = renderContext.CreatePen((Color)paletteColor, AntiAliasing, StrokeThickness, Opacity))
                    using (var fill = renderContext.CreateBrush((Color) paletteColor, Opacity))
                    {
                        renderContext.DrawEllipse(pen, fill, new Point(rect.X + rect.Width * 0.5, rect.Y + rect.Height * 0.5), rect.Width, rect.Height);
                    }
		        }
		        else
				{
                    // Blit the pre-cached point marker+
                    renderContext.DrawSprites(_cachedBubble.Inner, new[] {rect});
		        }
			});
        }

        private void RecreateCachedBubbles(IRenderContext2D renderContext2D)
        {
            var typeOfRenderer = renderContext2D.GetType();
            if (_cachedBubble != null && _typeOfRendererForCachedBubble == typeOfRenderer)
                return;

            if (_cachedBubble != null)
            {
                _cachedBubble.Dispose();
                _cachedBubble = null;
            }

            _cachedBubble = new SmartDisposable<ISprite2D>(renderContext2D.CreateSprite(new Ellipse
            {
                Width = MaxBubbleSizeInPixels,
                Height = MaxBubbleSizeInPixels,
                Fill = new RadialGradientBrush(new GradientStopCollection
                            {
                                new GradientStop { Color = Colors.Transparent, Offset = 0},
                                new GradientStop { Color = BubbleColor, Offset = 0.95},
                                new GradientStop { Color = Colors.Transparent, Offset = 1}
                            }),
            }));

            _typeOfRendererForCachedBubble = typeOfRenderer;
        }

        private void EnumerateBubbles(IRenderPassData renderPassData, Action<double, double, double, Rect> callback)
        {
            var pointSeries = renderPassData.PointSeries as XyzPointSeries;
            int setCount = pointSeries.Count;

            _maxZValue = 0;            

            var isVerticalChart = renderPassData.IsVerticalChart;

            if (AutoZRange)
            {
                for (int i = 0; i < setCount; i++)
                {
                    var xyzPoint = pointSeries[i] as GenericPoint2D<XyzSeriesPoint>;
                    var zValue = xyzPoint.YValues.Z;
                    if (zValue > _maxZValue) _maxZValue = zValue;
                }
            }

            // Iterate over points collection and render point markers
            for (int i = 0; i < setCount; i++)
            {
                // Cast the Series-POint to XyyDataPoint
                var xyyPoint = pointSeries[i] as GenericPoint2D<XyzSeriesPoint>;

                double xValue = xyyPoint.X;
                double yValue = xyyPoint.YValues.Y;
                double zValue = xyyPoint.YValues.Z;

                if (double.IsNaN(yValue))
                    continue;

                var bubbleSize = GetBubbleSize(zValue);

                var xCoord = (int) renderPassData.XCoordinateCalculator.GetCoordinate(xValue);
                var yCoord = (int) renderPassData.YCoordinateCalculator.GetCoordinate(yValue);

                var bubbleCenter = TransformPoint(xCoord, yCoord, isVerticalChart);
                bubbleCenter = renderPassData.TransformationStrategy.ReverseTransform(bubbleCenter);

                callback(xValue, yValue, zValue, new Rect(bubbleCenter.X - bubbleSize / 2, bubbleCenter.Y - bubbleSize / 2, bubbleSize, bubbleSize));
            }
        }

        private double GetBubbleSize(double zValue)
        {
            double bubbleSize = zValue * ZScaleFactor;

            if (AutoZRange)
            {
                bubbleSize = MaxBubbleSizeInPixels * zValue / _maxZValue;
            }

            return bubbleSize;
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = (FastBubbleRenderableSeries)d;
            series._cachedBubble = null;
            series.OnInvalidateParentSurface();
        }
    }
}