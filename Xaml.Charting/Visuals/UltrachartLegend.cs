// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartLegend.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Provides a Legend control to display series color and name
    /// </summary>
    public sealed class UltrachartLegend : ItemsControl
    {
        /// <summary>
        /// Defines the LegendData Dependency property
        /// </summary>
        public static readonly DependencyProperty LegendDataProperty = DependencyProperty.Register("LegendData", typeof(ChartDataObject), typeof(UltrachartLegend), new PropertyMetadata(null, OnLegendDataChanged));

        /// <summary>
        /// Defines the ShowVisibilityCheckboxes DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowVisibilityCheckboxesProperty =
            DependencyProperty.Register("ShowVisibilityCheckboxes", typeof (bool), typeof (UltrachartLegend), new PropertyMetadata(default(bool), OnShowVisibilityCheckboxesChanged));

        /// <summary>
        /// Defines the ShowPointMarkers DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowSeriesMarkersProperty =
            DependencyProperty.Register("ShowSeriesMarkers", typeof(bool), typeof(UltrachartLegend), new PropertyMetadata(true));

        /// <summary>
        /// Defines the Orientation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(UltrachartLegend), new PropertyMetadata(Orientation.Vertical));

        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartLegend"/> class.
        /// </summary>
        public UltrachartLegend()
        {
            DefaultStyleKey = typeof(UltrachartLegend);
        }

        /// <summary>
        /// Gets or sets the <see cref="ChartDataObject"/> which provides SeriesInfo property
        /// </summary>
        public ChartDataObject LegendData
        {
            get { return (ChartDataObject)GetValue(LegendDataProperty); }
            set { SetValue(LegendDataProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether a column of checkboxes bound to the Visibility of series is shown
        /// </summary>
        public bool ShowVisibilityCheckboxes
        {
            get { return (bool)GetValue(ShowVisibilityCheckboxesProperty); }
            set { SetValue(ShowVisibilityCheckboxesProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value indicating whether to show series markers defined by <see cref="BaseRenderableSeries.LegendMarkerTemplate"/>
        /// </summary>
        public bool ShowSeriesMarkers
        {
            get { return (bool)GetValue(ShowSeriesMarkersProperty); }
            set { SetValue(ShowSeriesMarkersProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value, which determines the orientation of legend items layout
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private static void OnShowVisibilityCheckboxesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var legend = (UltrachartLegend) d;
            if (legend.LegendData != null) legend.LegendData.ShowVisibilityCheckboxes = (bool)e.NewValue;
        }

        private static void OnLegendDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var legend = (UltrachartLegend)d;
            var cdo = e.NewValue as ChartDataObject;
            if (cdo != null) cdo.ShowVisibilityCheckboxes = legend.ShowVisibilityCheckboxes;
        }
    }
}
