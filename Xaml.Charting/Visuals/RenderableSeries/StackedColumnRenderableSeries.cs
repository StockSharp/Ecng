// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedColumnRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.PointMarkers;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines a Stacked-Column renderable series, supporting rendering of column bars which have accumulated Y-values for multiple series in a group.
    /// </summary>
    /// <remarks>
    /// The StackedColumnRenderableSeries may render data from any a <see cref="IXyDataSeries{TX,TY}"/> derived data-source, 
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
    public class StackedColumnRenderableSeries : BaseColumnRenderableSeries, IStackedColumnRenderableSeries
    {
        /// <summary>  
        /// Defines the StackedGroupId DependnecyProperty
        /// </summary>
        public static readonly DependencyProperty StackedGroupIdProperty = DependencyProperty.Register("StackedGroupId", typeof(string), typeof(StackedColumnRenderableSeries), new PropertyMetadata("DefaultStackedGroupId", StackedGroupIdPropertyChanged));

        /// <summary>  
        /// Defines the IsOneHundredPercent DependnecyProperty
        /// </summary>
        public static readonly DependencyProperty IsOneHundredPercentProperty = DependencyProperty.Register("IsOneHundredPercent", typeof(bool), typeof(StackedColumnRenderableSeries), new PropertyMetadata(default(bool)));

        /// <summary>  
        /// Defines the Spacing DependnecyProperty 
        /// </summary>
        public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register("Spacing", typeof(double), typeof(StackedColumnRenderableSeries), new PropertyMetadata(0.1, OnInvalidateParentSurface));

        /// <summary>  
        /// Defines the SpacingMode DependnecyProperty
        /// </summary>
        public static readonly DependencyProperty SpacingModeProperty = DependencyProperty.Register("SpacingMode", typeof(SpacingMode), typeof(StackedColumnRenderableSeries), new PropertyMetadata(SpacingMode.Relative, OnInvalidateParentSurface));

        /// <summary> 
        /// Defines the ShowLabel DependnecyProperty 
        /// </summary>
        public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register("ShowLabel", typeof (bool), typeof (StackedColumnRenderableSeries), new PropertyMetadata(false, OnInvalidateParentSurface));

        /// <summary>  
        /// Defines the LabelColor DependnecyProperty 
        /// </summary>
        public static readonly DependencyProperty LabelColorProperty = DependencyProperty.Register("LabelColor", typeof (Color), typeof (StackedColumnRenderableSeries), new PropertyMetadata(Colors.White));

        /// <summary>  
        /// Defines the LabelFontSize DependnecyProperty
        /// </summary>
        public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register("LabelFontSize", typeof(float), typeof(StackedColumnRenderableSeries), new PropertyMetadata(12f));
        
        /// <summary>  
        /// Defines the LabelTextFormatting DependnecyProperty 
        /// </summary>
        public static readonly DependencyProperty LabelTextFormattingProperty = DependencyProperty.Register("LabelTextFormatting", typeof (string), typeof (StackedColumnRenderableSeries), new PropertyMetadata("0.00"));

        /// <summary>
        /// Initializes a new instance of the <see cref="StackedColumnRenderableSeries" /> class.
        /// </summary>
        public StackedColumnRenderableSeries()
        {
            this.DefaultStyleKey = typeof(StackedColumnRenderableSeries);
        }

        /// <summary>
        /// Gets or sets a string StackedGroupId. All series within the same group get stacked vertically.
        /// </summary>
        public string StackedGroupId
        {
            get { return (string)GetValue(StackedGroupIdProperty); }
            set { SetValue(StackedGroupIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value which indicates whether the series are 100% stacked
        /// </summary>
        public bool IsOneHundredPercent
        {
            get { return (bool)GetValue(IsOneHundredPercentProperty); }
            set
            {
                SetValue(IsOneHundredPercentProperty, value);
                var ultraChartSurface = GetParentSurface();
                if (ultraChartSurface != null)
                {
                    ultraChartSurface.InvalidateElement();
                }
            }
        }

        /// <summary>
        /// Gets or sets the value which specifies the width of the gap between horizontally stacked columns. 
        /// Can be set to either a relative or absolute value depending on the <see cref="SpacingMode"/> used.
        /// </summary>
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="SpacingMode"/> to use for the space between columns computations.
        /// E.g. the default of Absolute requires that <see cref="Spacing"/> is in pixels. The value
        /// of Relative requires that <see cref="Spacing"/> is a double value from 0.0 to 1.0.
        /// </summary>
        public SpacingMode SpacingMode
        {
            get { return (SpacingMode)GetValue(SpacingModeProperty); }
            set { SetValue(SpacingModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value indicating whether to show text labels over the columns.
        /// </summary>
        public bool ShowLabel
        {
            get { return (bool)GetValue(ShowLabelProperty); }
            set { SetValue(ShowLabelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the foreground color for text labels.
        /// </summary>
        public Color LabelColor
        {
            get { return (Color)GetValue(LabelColorProperty); }
            set { SetValue(LabelColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size for text labels.
        /// </summary>
        public float LabelFontSize
        {
            get { return (float)GetValue(LabelFontSizeProperty); }
            set { SetValue(LabelFontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the formatting string for text labels.
        /// </summary>
        public string LabelTextFormatting
        {
            get { return (string)GetValue(LabelTextFormattingProperty); }
            set { SetValue(LabelTextFormattingProperty, value); }
        }

        bool IStackedColumnRenderableSeries.IsValidForDrawing { get { return base.IsValidForDrawing; } }

        /// <summary>
        /// The <see cref="IStackedColumnsWrapper"/> instance which wraps this <see cref="StackedColumnRenderableSeries"/>.
        /// </summary>
        public IStackedColumnsWrapper Wrapper
        {
            get
            {
                var surface = (UltrachartSurface) GetParentSurface();
                return surface != null ? surface.StackedColumnsWrapper : null;
            }
        }

        /// <summary>
        /// Computes the full X data range which current <see cref="StackedColumnRenderableSeries"/> occupies.
        /// </summary>
        public override IRange GetXRange()
        {
            var isLogarithmicAxis = CurrentRenderPassData != null &&
                                    CurrentRenderPassData.XCoordinateCalculator.IsLogarithmicAxisCalculator;
            return Wrapper.GetXRange(isLogarithmicAxis);
        }

        /// <summary>
        /// Returns the data range of the assosiated <see cref="IDataSeries"/> on Y direction
        /// <param name="xRange">The X-Axis Range currently in view</param>
        /// <param name="getPositiveRange">Indicates whether to return positive YRange only</param>
        /// </summary>
        public override IRange GetYRange(IRange xRange, bool getPositiveRange)
        {
            if (xRange == null)
            {
                throw new ArgumentNullException("xRange");
            }

            var indicesRange = DataSeries.GetIndicesRange(xRange);

            var yRange = Wrapper.CalculateYRange(this, indicesRange);

            return yRange;
        }
        
        /// <summary>
        /// Draws the series using the <see cref="IRenderContext2D"/> and the <see cref="IRenderPassData"/> passed in.
        /// </summary>
        /// <param name="renderContext">The render context. This is a graphics object which has methods to draw lines, quads and polygons to the screen</param>
        /// <param name="renderPassData">The render pass data. Contains a resampled <see cref="IPointSeries"/>, the <see cref="IndexRange"/> of points on the screen
        /// and the current YAxis and XAxis <see cref="ICoordinateCalculator{T}"/> to convert data-points to screen points</param>
        protected override void InternalDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            Wrapper.DrawStackedSeries(renderContext);
        }

        /// <summary>
        /// Used Internally: gets the rotation angle of the chart, which is 0 degrees or 90 degrees depending on whether the parent <see cref="UltrachartSurface"/>
        /// has swapped X and Y Axes or not. 
        /// </summary>
        public double GetChartRotationAngle()
        {
            return GetChartRotationAngle(CurrentRenderPassData);
        }

        /// <summary>
        /// Returns the series lower bound at nearest hit point
        /// </summary>
        protected override double GetSeriesBodyLowerDataBound(HitTestInfo nearestHitPoint)
        {
            var bounds = Wrapper.GetSeriesVerticalBounds(this, nearestHitPoint.DataSeriesIndex);
            return Math.Min(bounds.Item1, bounds.Item2);
        }

        /// <summary>
        /// Returns the series upper bound at nearest hit point
        /// </summary>
        protected override double GetSeriesBodyUpperDataBound(HitTestInfo nearestHitPoint)
        {
            var bounds = Wrapper.GetSeriesVerticalBounds(this, nearestHitPoint.DataSeriesIndex);
            return Math.Max(bounds.Item1, bounds.Item2);
        }

        /// <summary>
        /// Returns the width of a single column at <see cref="HitTestInfo.DataSeriesIndex"/>.
        /// </summary>
        protected override double GetSeriesBodyWidth(HitTestInfo nearestHitPoint)
        {
            return Wrapper.GetSeriesBodyWidth(this, nearestHitPoint.DataSeriesIndex);
        }

        protected override HitTestInfo HitTestInternal(Point rawPoint, double hitTestRadius, bool interpolate)
        {
            var nearestHitResult = HitTestInfo.Empty;

            if (IsVisible)
            {
                nearestHitResult = NearestHitResult(rawPoint, GetHitTestRadiusConsideringPointMarkerSize(hitTestRadius), SearchMode.Nearest, false);

                nearestHitResult = Wrapper.ShiftHitTestInfo(rawPoint, nearestHitResult, hitTestRadius, this);
            }

            return nearestHitResult;
        }

        private static void StackedGroupIdPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var rSeries = d as StackedColumnRenderableSeries;
            if (rSeries != null && rSeries.Wrapper != null)
            {
                rSeries.Wrapper.MoveSeriesToAnotherGroup(rSeries, (string)e.OldValue, (string)e.NewValue);
            }
        }
    }
}