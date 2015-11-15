// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LegendModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Enumeration constants to define label placement
    /// </summary>
    public enum LegendPlacement
    {
        /// <summary>
        /// Places at the upper left corner
        /// </summary>
        TopLeft,
        /// <summary>
        /// Places at the upper right corner
        /// </summary>
        TopRight,
        /// <summary>
        /// Places at the bottom left corner
        /// </summary>
        BottomLeft,
        /// <summary>
        /// Places at the bottom right corner
        /// </summary>
        BottomRight,
        /// <summary>
        /// Places above the surface
        /// </summary>
        Top,
        /// <summary>
        /// Places below the surface
        /// </summary>
        Bottom,
        /// <summary>
        /// Places to the left of the surface
        /// </summary>
        Left,
        /// <summary>
        /// Places to the right of the surface
        /// </summary>
        Right,
        /// <summary>
        /// Places inside the ParentSurface
        /// </summary>
        Inside,
    }

    /// <summary>
    /// Defines a custom chart modifier to provide info for the <see cref="LegendTemplate"/>
    /// </summary>
    public class LegendModifier : InspectSeriesModifierBase
    {
        /// <summary>
        /// Defines the LegendData DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LegendDataProperty =
            DependencyProperty.Register("LegendData", typeof(ChartDataObject), typeof(LegendModifier), new PropertyMetadata(null));

        /// <summary>
        /// Defines the GetLegendDataFor DependencyProperty
        /// </summary>
        public static readonly DependencyProperty GetLegendDataForProperty = DependencyProperty.Register("GetLegendDataFor", typeof(SourceMode), typeof(LegendModifier), new PropertyMetadata(SourceMode.AllSeries, (s, e) => ((LegendModifier)s).UpdateLegend()));

        /// <summary>
        /// Defines the LegendPlacement DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LegendPlacementProperty =
            DependencyProperty.Register("LegendPlacement", typeof(LegendPlacement), typeof(LegendModifier), new PropertyMetadata(LegendPlacement.Inside));

        /// <summary>
        /// Defines the LegendTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LegendItemTemplateProperty =
            DependencyProperty.Register("LegendItemTemplate", typeof(DataTemplate), typeof(LegendModifier), new PropertyMetadata(null));

        /// <summary>
        /// Defines the LegendOrientation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(LegendModifier), new PropertyMetadata(Orientation.Vertical));

        /// <summary>
        /// Defines the ShowSeriesMarkers DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowSeriesMarkersProperty =
            DependencyProperty.Register("ShowSeriesMarkers", typeof(bool), typeof(LegendModifier), new PropertyMetadata(true));

        /// <summary>
        /// Defines the ShowVisibilityCheckboxes DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowVisibilityCheckboxesProperty =
            DependencyProperty.Register("ShowVisibilityCheckboxes", typeof(bool), typeof(LegendModifier), new PropertyMetadata(true));

        /// <summary>
        /// Defines the ShowLegend DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowLegendProperty =
            DependencyProperty.Register("ShowLegend", typeof(bool), typeof(LegendModifier), new PropertyMetadata(false, OnShowLegendChanged));

        /// <summary>
        /// Defines the UltrachartLegend DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LegendTemplateProperty =
            DependencyProperty.Register("LegendTemplate", typeof(ControlTemplate), typeof(LegendModifier), new PropertyMetadata(OnLegendTemplateChanged));

        private FrameworkElement _legend;

        /// <summary>
        /// Initializes a new instance of the <see cref="LegendModifier"/> class.
        /// </summary>
        public LegendModifier()
        {
            DefaultStyleKey = typeof(LegendModifier);

            this.SetCurrentValue(SeriesDataProperty, new ChartDataObject());
        }

        /// <summary>
        /// Used in combination with <see cref="LegendModifier.ShowLegend"/> = true. If true, shows the Visibility Checkboxes in the automatically generated legend. 
        /// </summary>
        public bool ShowVisibilityCheckboxes
        {
            get { return (bool)GetValue(ShowVisibilityCheckboxesProperty); }
            set { SetValue(ShowVisibilityCheckboxesProperty, value); }
        }

        /// <summary>
        /// Used in combination with <see cref="LegendModifier.ShowLegend"/> = true. If true, shows the Series Markers in the automatically generated legend
        /// </summary>
        public bool ShowSeriesMarkers
        {
            get { return (bool)GetValue(ShowSeriesMarkersProperty); }
            set { SetValue(ShowSeriesMarkersProperty, value); }
        }

        /// <summary>
        /// Used in combination with <see cref="LegendModifier.ShowLegend"/> = true. Defines the placement of the auto-generated legend 
        /// </summary>
        public LegendPlacement LegendPlacement
        {
            get { return (LegendPlacement)GetValue(LegendPlacementProperty); }
            set { SetValue(LegendPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Legend Item Template, which provides UI that binds to a single <see cref="SeriesInfo"/> instance. 
        /// </summary>
        public DataTemplate LegendItemTemplate
        {
            get { return (DataTemplate)GetValue(LegendItemTemplateProperty); }
            set { SetValue(LegendItemTemplateProperty, value); }
        }

        /// <summary>
        /// The LegendData object provides a collection of <see cref="SeriesInfo"/> which can be bound to in ItemsControls or UltrachartLegend control. 
        /// </summary>
        public ChartDataObject LegendData
        {
            get { return (ChartDataObject)GetValue(LegendDataProperty); }
            set { SetValue(LegendDataProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Orientation of the Legend Items, e.g. Vertical, or Horizontal
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// If true, then the LegendModifier automatically shows and hosts a <see cref="UltrachartLegend"/> inside the chart surface, according to <see cref="LegendPlacement"/>. If false, then the user may provide an alternative legend control
        /// </summary>
        public bool ShowLegend
        {
            get { return (bool)GetValue(ShowLegendProperty); }
            set { SetValue(ShowLegendProperty, value); }
        }

        /// <summary>
        /// Used in combination with <see cref="LegendModifier.ShowLegend"/> = true. An optional control template for the auto-generated <see cref="UltrachartLegend"/> control
        /// </summary>
        public ControlTemplate LegendTemplate
        {
            get { return (ControlTemplate)GetValue(LegendTemplateProperty); }
            set { SetValue(LegendTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets an enumeration constant defining which series to interrogate for the legend data source 
        /// </summary>
        public SourceMode GetLegendDataFor
        {
            get { return (SourceMode)GetValue(GetLegendDataForProperty); }
            set { SetValue(GetLegendDataForProperty, value); }
        }

        /// <summary>
        /// Called when the element is attached to the Chart Surface
        /// </summary>
        public override void OnAttached()
        {
            if (ShowLegend)
            {
                ParentSurface.RootGrid.SafeAddChild(_legend);
            }

            base.OnAttached();
        }

        /// <summary>
        /// Called immediately before the element is detached from the Chart Surface
        /// </summary>
        public override void OnDetached()
        {
            ParentSurface.RootGrid.SafeRemoveChild(_legend);

            base.OnDetached();
        }

        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        protected override void ClearAll() { }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleMasterMouseEvent(Point mousePoint) { }

        /// <summary>
        /// When overriden in a derived class, called to handle the Slave <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleSlaveMouseEvent(Point mousePoint) { }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        public override void OnModifierMouseMove(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when the parent <see cref="UltrachartSurface"/> is rendered
        /// </summary>
        /// <param name="e">The <see cref="UltrachartRenderedMessage"/> which contains the event arg data</param>
        public override void OnParentSurfaceRendered(UltrachartRenderedMessage e)
        {
            base.OnParentSurfaceRendered(e);

            UpdateLegend();
        }

        /// <summary>
        /// Refreshes the legend with up-to-date <see cref="SeriesInfo"/> with series names, latest values
        /// </summary>
        public virtual void UpdateLegend()
        {
            if (IsEnabled && IsAttached && ParentSurface != null && ParentSurface.RenderableSeries != null)
            {
                var allSeries = ParentSurface.RenderableSeries.Where(IsSeriesValid);

                var newSerieesInfoCollection = GetSeriesInfo(allSeries);
                var oldSeriesInfoCollection = SeriesData.SeriesInfo;

                // We need this trick to prevent visibility checkboxes flickering.
                // Cutting off the old collection
                var newRenderableSeries = newSerieesInfoCollection.Select(x => x.RenderableSeries).ToArray();

                oldSeriesInfoCollection.RemoveWhere(info => !newRenderableSeries.Contains(info.RenderableSeries));

                // Update old elements and add new elements
                foreach (var seriesInfo in newSerieesInfoCollection)
                {
                    var newSeriesInfo = seriesInfo;
                    var oldSeriesInfo =
                        oldSeriesInfoCollection.FirstOrDefault(
                            x => x.RenderableSeries.Equals(newSeriesInfo.RenderableSeries));

                    if (oldSeriesInfo != null)
                    {
                        UpdateSeriesInfo(oldSeriesInfo, newSeriesInfo);
                    }
                    else
                    {
                        oldSeriesInfoCollection.Add(newSeriesInfo);
                    }
                }
            }
        }

        private bool IsSeriesValid(IRenderableSeries series)
        {
            return series != null && CheckSeriesMode(series) && series.DataSeries != null;
        }

        private bool CheckSeriesMode(IRenderableSeries series)
        {
            var result = GetLegendDataFor == SourceMode.AllSeries ||
                         series.IsVisible && GetLegendDataFor == SourceMode.AllVisibleSeries ||
                         series.IsSelected && GetLegendDataFor == SourceMode.SelectedSeries ||
                         !series.IsSelected && GetLegendDataFor == SourceMode.UnselectedSeries;

            return result;
        }

        /// <summary>
        /// Gets the SeriesInfo for all the RenderableSeries passed in
        /// </summary>
        /// <param name="allSeries"></param>
        /// <returns></returns>
        protected virtual ObservableCollection<SeriesInfo> GetSeriesInfo(IEnumerable<IRenderableSeries> allSeries)
        {
            var seriesInfos = new ObservableCollection<SeriesInfo>();

            if (allSeries != null)
            {
                foreach (var renderableSeries in allSeries)
                {
                    var hitResult = renderableSeries.HitTest(new Point(ModifierSurface.ActualWidth, 0));
                    var seriesInfo = renderableSeries.GetSeriesInfo(hitResult);

                    seriesInfos.Add(seriesInfo);
                }
            }

            return seriesInfos;
        }

        /// <summary>
        /// Does a replace of the data on the OldSeriesInfo instance with data from NewSeriesInfo
        /// </summary>
        /// <param name="oldSeriesInfo"></param>
        /// <param name="newSeriesInfo"></param>
        private static void UpdateSeriesInfo(SeriesInfo oldSeriesInfo, SeriesInfo newSeriesInfo)
        {
            oldSeriesInfo.DataSeriesIndex = newSeriesInfo.DataSeriesIndex;
            oldSeriesInfo.DataSeriesType = newSeriesInfo.DataSeriesType;
            oldSeriesInfo.IsHit = newSeriesInfo.IsHit;
            oldSeriesInfo.SeriesColor = newSeriesInfo.SeriesColor;
            oldSeriesInfo.SeriesName = newSeriesInfo.SeriesName;
            oldSeriesInfo.Value = newSeriesInfo.Value;
            oldSeriesInfo.XValue = newSeriesInfo.XValue;
            oldSeriesInfo.YValue = newSeriesInfo.YValue;
            oldSeriesInfo.XyCoordinate = newSeriesInfo.XyCoordinate;
        }

        private static void OnShowLegendChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = d as LegendModifier;
            if (modifier != null && modifier.ParentSurface != null && modifier.LegendTemplate != null)
            {
                if (modifier.ShowLegend)
                {
                    modifier._legend.DataContext = modifier;
                    modifier.ParentSurface.RootGrid.SafeAddChild(modifier._legend);
                }
                else
                {
                    modifier.ParentSurface.RootGrid.SafeRemoveChild(modifier._legend);
                }
            }
        }

        private static void OnLegendTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = d as LegendModifier;
            if (modifier != null && e.NewValue != null)
            {
                modifier._legend = modifier._legend ?? new LegendPlaceholder();
                modifier._legend.DataContext = modifier;
            }
        }
    }
}
