// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltraStockChart.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Data;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Provides a high performance Stock Chart control surface with a <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> viewport. 
    /// 
    /// Templated to inclue a <see cref="CategoryDateTimeAxis"/> as XAxis and <see cref="NumericAxis"/> as YAxis. 
    /// 
    /// The UltraStockChart can have an <see cref="IDataSeries"/> data source for each <see cref="IRenderableSeries"/>, or use the new MVVM API (see the <see cref="UltrachartSurface.SeriesSource"/> property)
    /// </summary>
    [UltrachartLicenseProvider(typeof(UltraStockChartLicenseProvider))]
    public class UltraStockChart : UltrachartSurface
    {
        /// <summary>Defines the XAxisStyle DependencyProperty</summary>
        public static readonly DependencyProperty XAxisStyleProperty = DependencyProperty.Register("XAxisStyle", typeof (Style), typeof (UltraStockChart), new PropertyMetadata(default(Style)));
        /// <summary>Defines the YAxisStyle DependencyProperty</summary>
        public static readonly DependencyProperty YAxisStyleProperty = DependencyProperty.Register("YAxisStyle", typeof (Style), typeof (UltraStockChart), new PropertyMetadata(default(Style)));
        /// <summary>Defines the IsCursorEnabled DependencyProperty</summary>
        public static readonly DependencyProperty IsCursorEnabledProperty = DependencyProperty.Register("IsCursorEnabled", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(default(bool), OnDataProviderChanged));
        /// <summary>Defines the IsRolloverEnabled DependencyProperty</summary>
        public static readonly DependencyProperty IsRolloverEnabledProperty = DependencyProperty.Register("IsRolloverEnabled", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(default(bool), OnDataProviderChanged));
        /// <summary>Defines the IsPanEnabled DependencyProperty</summary>
        public static readonly DependencyProperty IsPanEnabledProperty = DependencyProperty.Register("IsPanEnabled", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(default(bool)));
        /// <summary>Defines the IsRubberBandZoomEnabled DependencyProperty</summary>
        public static readonly DependencyProperty IsRubberBandZoomEnabledProperty = DependencyProperty.Register("IsRubberBandZoomEnabled", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(default(bool)));
        /// <summary>Defines the BarTimeFrame DependencyProperty</summary>
        public static readonly DependencyProperty BarTimeFrameProperty = DependencyProperty.Register("BarTimeFrame", typeof(double), typeof(UltraStockChart), new PropertyMetadata(-1.0));        
        /// <summary>Defines the IsXAxisVisible DependencyProperty</summary>
        public static readonly DependencyProperty IsXAxisVisibleProperty = DependencyProperty.Register("IsXAxisVisible", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(true, OnIsXAxisVisibleDependencyPropertyChanged));
        /// <summary>Defines the VerticalChartGroupId DependencyProperty</summary>
        public static readonly DependencyProperty VerticalChartGroupIdProperty = DependencyProperty.Register("VerticalChartGroupId", typeof (string), typeof (UltraStockChart), new PropertyMetadata(default(string), OnVerticalChartGroupIdChanged));
        /// <summary>Defines the IsAxisMarkersEnabled DependencyProperty</summary>
        public static readonly DependencyProperty IsAxisMarkersEnabledProperty = DependencyProperty.Register("IsAxisMarkersEnabled", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(true, OnInvalidateUltrachartSurface));
        /// <summary>Defines the LegendSource DependencyProperty</summary>
        public static readonly DependencyProperty LegendSourceProperty = DependencyProperty.Register("LegendSource", typeof (ChartDataObject), typeof (UltraStockChart), new PropertyMetadata(null));
        /// <summary>Defines the CurrentDataProvider DependencyProperty</summary>
        public static readonly DependencyProperty DefaultDataProviderProperty = DependencyProperty.Register("DefaultDataProvider", typeof(InspectSeriesModifierBase), typeof(UltraStockChart), new PropertyMetadata(null));
        /// <summary> Defines the ShowLegend DependencyProperty </summary>
        public static readonly DependencyProperty ShowLegendProperty = DependencyProperty.Register("ShowLegend", typeof(bool), typeof(UltraStockChart), new PropertyMetadata(true));
        /// <summary> Defines the LegendStyle DependencyProperty </summary>
        public static readonly DependencyProperty LegendStyleProperty =DependencyProperty.Register("LegendStyle", typeof (Style), typeof (UltraStockChart), new PropertyMetadata(default(Style)));

        /// <summary>
        /// Initializes a new instance of the <see cref="UltraStockChart" /> class.
        /// </summary>
        public UltraStockChart()
        {
            DefaultStyleKey = typeof (UltraStockChart);
        }

        /// <summary>
        /// Gets or set modifier which provides data for legend
        /// </summary>
        public InspectSeriesModifierBase DefaultDataProvider
        {
            get { return (InspectSeriesModifierBase)GetValue(DefaultDataProviderProperty); }
            set { SetValue(DefaultDataProviderProperty, value); }
        }

        /// <summary>
        /// Gets or sets data source for legend
        /// </summary>
        public ChartDataObject LegendSource
        {
            get { return (ChartDataObject) GetValue(LegendSourceProperty); }
            set { SetValue(LegendSourceProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether need to display legend
        /// </summary>
        public bool ShowLegend
        {
            get { return (bool)GetValue(ShowLegendProperty); }
            set { SetValue(ShowLegendProperty, value); }
        }

        /// <summary>
        /// Gets or sets style for <see cref="LegendModifier"/>
        /// </summary>
        public Style LegendStyle
        {
            get { return (Style)GetValue(LegendStyleProperty); }
            set { SetValue(LegendStyleProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets whether Axis Markers are to be displayed on the right YAxis, showing the series values
        /// </summary>
        public bool IsAxisMarkersEnabled
        {
            get { return (bool)GetValue(IsAxisMarkersEnabledProperty); }
            set { SetValue(IsAxisMarkersEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets a proxy value for the UltrachartGroup.VerticalChartGroup attached property and MouseManager.MouseEventGroup property, which is used to bind together the chart sizes and mouse events
        /// </summary>  
        public string VerticalChartGroupId
        {
            get { return (string)GetValue(VerticalChartGroupIdProperty); }
            set { SetValue(VerticalChartGroupIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets a Style allowing direct overriding of the built-in XAxis (TargetType must be CategoryDateTimeAxis)
        /// </summary> 
        public Style XAxisStyle
        {
            get { return (Style)GetValue(XAxisStyleProperty); }
            set { SetValue(XAxisStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a Style allowing direct overriding of the built-in XAxis (TargetType must be NumericAxis)
        /// </summary>
        public Style YAxisStyle
        {
            get { return (Style)GetValue(YAxisStyleProperty); }
            set { SetValue(YAxisStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the X axis is visible.
        /// </summary>        
        public bool IsXAxisVisible
        {
            get { return (bool)GetValue(IsXAxisVisibleProperty); }
            set { SetValue(IsXAxisVisibleProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the CursorModifier is enabled or not
        /// </summary>
        public bool IsCursorEnabled
        {
            get { return (bool) GetValue(IsCursorEnabledProperty); }
            set { SetValue(IsCursorEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the RolloverModifier is enabled or not
        /// </summary>
        public bool IsRolloverEnabled
        {
            get { return (bool)GetValue(IsRolloverEnabledProperty); }
            set { SetValue(IsRolloverEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the Pan modifier is enabled
        /// </summary>
        public bool IsPanEnabled
        {
            get { return (bool)GetValue(IsPanEnabledProperty); }
            set { SetValue(IsPanEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the RubberBandXyZoom is enabled
        /// </summary>
        public bool IsRubberBandZoomEnabled
        {
            get { return (bool)GetValue(IsRubberBandZoomEnabledProperty); }
            set { SetValue(IsRubberBandZoomEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets the BarTimeFrame, this is the time in seconds for each bar on the <see cref="UltraStockChart"/>
        /// </summary>
        public double BarTimeFrame
        {
            get { return (double) GetValue(BarTimeFrameProperty); }
            set { SetValue(BarTimeFrameProperty, value);}
        }

        /// <summary>
        /// Zooms the chart to the extents of the data, plus any X or Y Grow By fraction set on the X and Y Axes
        /// </summary>
        public override void ZoomExtents()
        {
            if (YAxes.IsNullOrEmpty()) return;

            using (SuspendUpdates())
            {
                YAxis.GrowBy = new DoubleRange(0.1, 0.1);
                base.ZoomExtents();
            }
        }

        private static void OnIsXAxisVisibleDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraStockChart = (UltraStockChart) d;

            var isXAxisVisible = (bool)e.NewValue;
            if (ultraStockChart.XAxis != null)
            {
                ultraStockChart.XAxis.Visibility = isXAxisVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnVerticalChartGroupIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraStockChart = (UltraStockChart)d;
            if (ultraStockChart == null) return;

            var modifier = ultraStockChart.ChartModifier as ModifierGroup;
            if (modifier == null) return;

            MouseManager.SetMouseEventGroup(modifier, e.NewValue as string);
        }

        private static void OnDataProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var stockChart = d as UltraStockChart;
            if (stockChart != null)
            {
                var modifierGroup = (ModifierGroup)stockChart.ChartModifier;
                if (modifierGroup != null)
                {
                    var currentDataSource = (InspectSeriesModifierBase) modifierGroup["LegendModifier"];

                    if (stockChart.IsRolloverEnabled)
                    {
                        currentDataSource = (InspectSeriesModifierBase) modifierGroup["RolloverModifier"];
                    }
                    else if (stockChart.IsCursorEnabled)
                    {
                        currentDataSource = (InspectSeriesModifierBase) modifierGroup["CursorModifier"];
                    }

                    stockChart.DefaultDataProvider = currentDataSource;
                }
            }
        }
    }
}