// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// FastHeatmapRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using System.Windows.Media;
using System.ComponentModel;
using System.Linq;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Displays 2D data as a heatmap
    /// </summary>
    [UltrachartLicenseProvider(typeof(RenderableSeriesUltrachartLicenseProvider))]
    public class FastHeatMapRenderableSeries : BaseRenderableSeries, INotifyPropertyChanged
    {
        private const double DefaultMaximum = 1d;
        private const double DefaultMinimum = 0d;

        private const float DefaultCellFontSize = 12f;

        /// <summary>
        /// Defines the DrawTextInCell DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawTextInCellProperty = DependencyProperty.Register("DrawTextInCell", typeof(bool), typeof(FastHeatMapRenderableSeries),
            new PropertyMetadata(false));

        /// <summary>
        /// Defines the CellTextForeground DependencyProperty
        /// </summary>
        public static readonly DependencyProperty CellTextForegroundProperty = DependencyProperty.Register("CellTextForeground", typeof(Color), typeof(FastHeatMapRenderableSeries),
            new PropertyMetadata(Colors.White));

        /// <summary>
        /// Defines the CellFontSize DependencyProperty
        /// </summary>
        public static readonly DependencyProperty CellFontSizeProperty = DependencyProperty.Register("CellFontSize", typeof(float), typeof(FastHeatMapRenderableSeries),
            new PropertyMetadata(DefaultCellFontSize));

        /// <summary>
        /// Defines the ColorMap DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ColorMapProperty = DependencyProperty.Register("ColorMap", typeof(LinearGradientBrush), typeof(FastHeatMapRenderableSeries),
            new PropertyMetadata(DefaultColorMap));

        /// <summary>
        /// Defines the Minimum DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register("Minimum", typeof(double), typeof(FastHeatMapRenderableSeries),
            new PropertyMetadata(DefaultMinimum, OnMinimumMaximumPropertyChanged));
        /// <summary>
        /// Defines the Maximum DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register("Maximum", typeof(double), typeof(FastHeatMapRenderableSeries),
            new PropertyMetadata(DefaultMaximum, OnMinimumMaximumPropertyChanged));

        /// <summary>
        /// Is used to notify HeatmapColourMap
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="FastHeatMapRenderableSeries"/> class.
        /// </summary>
        /// <remarks></remarks>
        public FastHeatMapRenderableSeries()
        {
            DefaultStyleKey = typeof (FastHeatMapRenderableSeries);
        }

        static LinearGradientBrush DefaultColorMap
        {
            get
            {
                return new LinearGradientBrush(new GradientStopCollection
                {
                    new GradientStop { Color = Colors.Blue, Offset = 0},
                    new GradientStop { Color = Colors.Red, Offset = 1},
                }, 0);
            }
        }

        /// <summary>
        /// Gets or sets the ColorMap which is used to calculate color from data value
        /// </summary>
        public LinearGradientBrush ColorMap
        {
            get { return (LinearGradientBrush) GetValue(ColorMapProperty); }
            set { SetValue(ColorMapProperty, value); }
        }

        /// <summary>
        /// Gets or sets value which corresponds to min color
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets value which corresponds to max color
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets a flag to draw value in a cell
        /// </summary>
        public bool DrawTextInCell
        {
            get { return (bool) GetValue(DrawTextInCellProperty); }
            set { SetValue(DrawTextInCellProperty, value); }
        }

        /// <summary>
        /// Gets or sets foreground color to draw value in a cell
        /// </summary>
        public Color CellTextForeground
        {
            get { return (Color) GetValue(CellTextForegroundProperty); }
            set { SetValue(CellTextForegroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets foreground color to draw value in a cell
        /// </summary>
        public float CellFontSize
        {
            get { return (float) GetValue(CellFontSizeProperty); }
            set { SetValue(CellFontSizeProperty, value); }
        }

        private DoubleToColorMappingSettings MappingSettings
        {
            get
            {
                return new DoubleToColorMappingSettings
                {
                    GradientStops = ColorMap.GradientStops.ToArray(),
                    Minimum = Minimum,
                    ScaleFactor = 1 / Math.Abs(Maximum - Minimum)
                };
            }
        }

               
        /// <summary>
        /// Middle value to be displayed by HeatmapColourMap
        /// </summary>
        public double MiddleValue { get { return (Minimum + Maximum) * 0.5; } }
        

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var result = HitTestInfo.Empty;

            var heatmapDataSeries = DataSeries as IHeatmap2DArrayDataSeries;
            if (heatmapDataSeries != null)
            {
                var xValue = CurrentRenderPassData.XCoordinateCalculator.GetDataValue(rawPoint.X);
                var yValue = CurrentRenderPassData.YCoordinateCalculator.GetDataValue(rawPoint.Y);

                result = heatmapDataSeries.ToHitTestInfo(xValue, yValue, interpolate);
                result.HitTestPoint = rawPoint;
            }

            return result;
        }

        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            if (renderPassData.IsVerticalChart)
            {
                throw new NotImplementedException(String.Format("We are sorry! The vertical chart feature is not supported by the {0} currently.", GetType().Name));
            }

            var mappingSettings = MappingSettings;
            bool drawTextInCell = DrawTextInCell;
            var cellTextForeground = CellTextForeground;
            var cellFontSize = CellFontSize;
            var h = renderContext.ViewportSize.Height;
            int w = (int)renderContext.ViewportSize.Width;

			var points = CurrentRenderPassData.PointSeries;
			var xAxisIsFlipped = CurrentRenderPassData.XCoordinateCalculator.HasFlippedCoordinates;
			var yAxisIsFlipped = CurrentRenderPassData.YCoordinateCalculator.HasFlippedCoordinates;
            int pointsCount = points.Count;
            double opacity = Opacity;

            for (int horizontalCellIndex = 0; horizontalCellIndex < pointsCount; horizontalCellIndex++)
            {
                var arraySegment = (I2DArraySegment) points[horizontalCellIndex];
                var cellLeft = renderPassData.XCoordinateCalculator.GetCoordinate(arraySegment.XValueAtLeft);
                var cellRight = renderPassData.XCoordinateCalculator.GetCoordinate(arraySegment.XValueAtRight);

                if (cellLeft < 0 && cellRight < 0) continue;
                if (cellLeft > w && cellRight > w) continue;
                if (cellLeft < 0) cellLeft = 0;
                if (cellRight > w) cellRight = w;

                int yStartBottom = (int) renderPassData.YCoordinateCalculator.GetCoordinate(arraySegment.YValueAtBottom);
                int yEndTop = (int) renderPassData.YCoordinateCalculator.GetCoordinate(arraySegment.YValueAtTop);
                var verticalPixels = arraySegment.GetVerticalPixelsArgb(mappingSettings);

                if (xAxisIsFlipped)
                {
                    NumberUtil.Swap(ref cellLeft, ref cellRight);
                }

                for (var x = (int) cellLeft; x < cellRight; x++)
                {
                    if (x >= 0 && x < w)
                    {
                        renderContext.DrawPixelsVertically(x,
                            yStartBottom, yEndTop,
                            verticalPixels, opacity, yAxisIsFlipped);
                    }
                }

                if (drawTextInCell)
                {
                    var verticalPixelValues = arraySegment.GetVerticalPixelValues();

                    int cellHeight = (yStartBottom - yEndTop)/verticalPixelValues.Count;
                    if (yAxisIsFlipped) cellHeight *= -1;

                    for (int verticalCellIndex = 0; verticalCellIndex < verticalPixelValues.Count; verticalCellIndex++)
                    {
                        if (cellHeight > 0 && cellRight > cellLeft)
                        {
                            var cellIndex = verticalPixelValues.Count - 1 - verticalCellIndex;
                            var cellTop = yEndTop +
                                          (yStartBottom - yEndTop)*(yAxisIsFlipped ? verticalCellIndex + 1 : cellIndex)/
                                          verticalPixelValues.Count;

                            if (cellTop > h) continue;
                            if (cellTop + cellHeight < 0) continue;

                            var cellValue = verticalPixelValues[yAxisIsFlipped ? cellIndex : verticalCellIndex];

                            renderContext.DrawText(FormatCellToString(cellValue), new Rect(cellLeft, cellTop, cellRight - cellLeft, cellHeight), AlignmentX.Center, AlignmentY.Center,
                                cellTextForeground, cellFontSize);
                        }
                    }
                }
            }
        }

        protected virtual string FormatCellToString(double cellValue)
        {
            return cellValue.ToString("N2");
        }

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static void OnMinimumMaximumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fastHeatMapRenderableSeries = d as FastHeatMapRenderableSeries;

            if (fastHeatMapRenderableSeries != null)
            {
                fastHeatMapRenderableSeries.OnPropertyChanged("MiddleValue");
            }
        }
    }
}
