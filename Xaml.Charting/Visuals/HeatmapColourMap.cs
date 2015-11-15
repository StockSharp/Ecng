// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HeatmapColourMap.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// A Legend Control for the <see cref="FastHeatMapRenderableSeries"/>, which gives a visual representation of double-to-color mapping in the heatmap
    /// </summary>
    public class HeatmapColourMap : Control, INotifyPropertyChanged
    {
        /// <summary>
        /// Defines the FastHeatMapRenderableSeries DependencyProperty
        /// </summary>
        public static readonly DependencyProperty FastHeatMapRenderableSeriesProperty = DependencyProperty.Register("FastHeatMapRenderableSeries",
            typeof(FastHeatMapRenderableSeries),
            typeof(HeatmapColourMap), new PropertyMetadata(null, OnMappingSettingsChanged));
        
        /// <summary>
        /// Defines the Orientation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation",
            typeof(Orientation),
            typeof(HeatmapColourMap), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        /// <summary>
        /// Gets or sets the associated <see cref="FastHeatMapRenderableSeries"/> to derive color information from
        /// </summary>
        public FastHeatMapRenderableSeries FastHeatMapRenderableSeries
        {
            get { return (FastHeatMapRenderableSeries)GetValue(FastHeatMapRenderableSeriesProperty); }
            set { SetValue(FastHeatMapRenderableSeriesProperty, value); }
        }

        /// <summary>
        /// Gets or sets <see cref="Orientation"/>
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
       

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatmapColourMap" /> class.
        /// </summary>
        public HeatmapColourMap()
        {
            DefaultStyleKey = typeof(HeatmapColourMap);
        }

        private static void OnMappingSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }
        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Is raised when a property is changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
