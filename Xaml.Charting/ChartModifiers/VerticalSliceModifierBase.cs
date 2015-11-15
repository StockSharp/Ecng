// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// VerticalSliceModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Input;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="VerticalSliceModifierBase"/> is part of the ChartModifier API, which provides vertical slices on <see cref="IRenderableSeries"/> and a mouse-over templated tooltip to bind to a custom DataContext, provided by the output of the Hit-Test operation on a <see cref="IRenderableSeries"/>
    /// </summary>
    public abstract class VerticalSliceModifierBase : TooltipModifierBase
    {
        /// <summary>
        /// Defines the RolloverLabel Attached Property, which can be attached to point marker
        /// </summary>
        internal static readonly DependencyProperty RolloverLabelProperty = DependencyProperty.RegisterAttached("RolloverLabel", typeof(TemplatableControl), typeof(RolloverModifier), new PropertyMetadata(null));

        internal static Control GetRolloverLabel(DependencyObject o)
        {
            return (Control)o.GetValue(RolloverLabelProperty);
        }

        internal static void SetRolloverLabel(DependencyObject o, Control value)
        {
            o.SetValue(RolloverLabelProperty, value);
        }

        private List<FrameworkElement> _rolloverMarkers = new List<FrameworkElement>();

        private List<FrameworkElement> _tooltipLabels = new List<FrameworkElement>();
        private bool _needToUpdateTooltips;

        /// <summary>
        /// Gets or sets delay for showing of tooltips in miliseconds
        /// </summary>
        public double HoverDelay
        {
            get { return (double)GetValue(HoverDelayProperty); }
            set { SetValue(HoverDelayProperty, value); }
        }

        /// <summary>
        /// Creates instance of <see cref="VerticalSliceModifierBase"/>
        /// </summary>
        protected VerticalSliceModifierBase()
        {
            _delayActionHelper = new DelayActionHelper() { Interval = HoverDelay };
        }

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached()
        {
            base.OnDetached();

            ClearAll();
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase"/> instance
        /// </summary>
        /// <remarks></remarks>
        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();
            base.OnDetached();

            RemoveMarkers();
        }

        private void RemoveMarkers(bool removeLabels = false)
        {
            if (ModifierSurface != null)
            {
                foreach (var marker in _rolloverMarkers)
                {
                    DetachRolloverMarker(marker);

                    if (removeLabels)
                    {
                        RemoveLabelFor(marker);
                    }
                }
            }

            _rolloverMarkers.Clear();
        }

        /// <summary>
        /// Detaches a RolloverMarker from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        /// <param name="rolloverMarker">The rollover marker.</param>
        protected virtual void DetachRolloverMarker(FrameworkElement rolloverMarker)
        {
            ModifierSurface.Children.Remove(rolloverMarker);

            if (!HasToShowTooltip())
            {
                rolloverMarker.MouseMove -= OnRolloverMarkerMouseMove;
                rolloverMarker.MouseLeave -= OnRolloverMarkerMouseLeave;
                rolloverMarker.MouseLeftButtonDown -= OnRolloverMarkerMouseLeave;
            }
        }

        private void RemoveLabelFor(FrameworkElement rolloverMarker)
        {
            var rolloverLabelCache = (FrameworkElement)rolloverMarker.GetValue(RolloverLabelProperty);

            if (rolloverLabelCache != null)
            {
                _tooltipLabels.Remove(rolloverLabelCache);

                if (ModifierSurface.Children.Contains(rolloverLabelCache))
                {
                    ModifierSurface.Children.Remove(rolloverLabelCache);
                }
            }
        }

        /// <summary>
        /// When overridden in derived classes, indicates whether <see cref="HitTestInfo"/> result of hit-test should be returned from the <see cref="GetSeriesInfoAt"/> method.
        /// </summary>
        protected override bool IsHitPointValid(HitTestInfo hitTestInfo)
        {
            var isPointValid = !hitTestInfo.IsEmpty() && hitTestInfo.IsWithinDataBounds &&
                               hitTestInfo.IsVerticalHit &&
                               hitTestInfo.HitTestPoint.X.IsDefined() && 
                               hitTestInfo.HitTestPoint.Y.IsDefined();

            if (hitTestInfo.DataSeriesType == DataSeriesType.Xyy)
            {
                isPointValid &= hitTestInfo.Y1HitTestPoint.Y.IsDefined();
            }

            return isPointValid;
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Slave <see cref="ChartModifierBase"/> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleSlaveMouseEvent(Point mousePoint)
        {
            HandleMasterMouseEvent(mousePoint);
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase"/> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleMasterMouseEvent(Point mousePoint)
        {
            if (!IsAttached || !IsEnabled || ParentSurface == null)
                return;

            if (ShowTooltipOn == ShowTooltipOptions.MouseHover)
                ClearTooltipLabels();

            //Remove all markers and vertical line from the ModifierSurface
            RemoveMarkers();
            
            var allSeries = ParentSurface.RenderableSeries;

            var seriesInfoCollection = new ObservableCollection<SeriesInfo>();

            if (allSeries != null)
            {
                var seriesInfos = GetSeriesInfoAt(mousePoint);
                FillWithIncludedSeries(seriesInfos, seriesInfoCollection);

                var xAxis = XAxis;
                if (xAxis != null)
                {
                    foreach (var seriesInfo in seriesInfoCollection.OrderBy(info => xAxis.IsHorizontalAxis ? info.XyCoordinate.Y : info.XyCoordinate.X))
                    {
                        var rolloverMarker = GetRolloverMarkerFrom(seriesInfo);

                        var isApplied = TryAddRolloverMarker(rolloverMarker, seriesInfo);

                        if (isApplied)
                        {
                            AttachTooltipLabelToMarker(rolloverMarker, seriesInfo);

                            TryUpdateOverlays(seriesInfo.XyCoordinate);
                        }
                    }
                }

                UpdateOverlays(mousePoint);
            }

            SeriesData.UpdateSeriesInfo(seriesInfoCollection);
        }

        protected virtual void FillWithIncludedSeries(IEnumerable<SeriesInfo> infos, ObservableCollection<SeriesInfo> seriesInfos)
        {
            infos.ForEachDo(seriesInfos.Add);
        }

        /// <summary>
        /// Enumerates the RenderableSeries on the parent <see cref="ChartModifierBase.ParentSurface" /> and gets <see cref="SeriesInfo" /> objects in given point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected override IEnumerable<SeriesInfo> GetSeriesInfoAt(Point point)
        {
            return
                GetSeriesInfoAt(renderSeries => renderSeries.VerticalSliceHitTest(point, UseInterpolation))
                    .SplitToSinglePointInfo();
        }

        /// <summary>
        /// Get rollover marker from <see cref="SeriesInfo"/> to place on chart 
        /// </summary>
        /// <param name="seriesInfo"></param>
        /// <returns></returns>
        protected abstract FrameworkElement GetRolloverMarkerFrom(SeriesInfo seriesInfo);

        private bool TryAddRolloverMarker(FrameworkElement rolloverMarker, SeriesInfo seriesInfo)
        {
            var isApplied = (rolloverMarker != null);
            if (isApplied)
            {
                isApplied = TryAddRolloverMarker(rolloverMarker, seriesInfo.XyCoordinate);

                if (isApplied)
                {
                    AttachRolloverMarker(rolloverMarker);
                }
            }

            return isApplied;
        }

        private bool TryAddRolloverMarker(FrameworkElement rolloverMarker, Point showRolloverAt)
        {
            //var translated = ModifierSurface.TranslatePoint(showRolloverAt, RootGrid);
            var isInBounds = ModifierSurface.IsPointWithinBounds(showRolloverAt);

            if (isInBounds)
            {
                PlaceRolloverMarker(rolloverMarker, showRolloverAt);
            }

            return isInBounds;
        }

        private void PlaceRolloverMarker(FrameworkElement rolloverMarker, Point hitPoint)
        {
            var prevXValue = Canvas.GetLeft(rolloverMarker);
            var prevYValue = Canvas.GetTop(rolloverMarker);

            rolloverMarker.MeasureArrange();

            var x = hitPoint.X - rolloverMarker.DesiredSize.Width / 2.0;
            var y = hitPoint.Y - rolloverMarker.DesiredSize.Height / 2.0;

            if (!prevXValue.Equals(x) || !prevYValue.Equals(y))
            {
                _needToUpdateTooltips = true;
                Canvas.SetLeft(rolloverMarker, x);
                Canvas.SetTop(rolloverMarker, y);
            }
        }

        private void AttachRolloverMarker(FrameworkElement rolloverMarker)
        {
            if (!HasToShowTooltip())
            {
                rolloverMarker.MouseMove += OnRolloverMarkerMouseMove;
                rolloverMarker.MouseLeave += OnRolloverMarkerMouseLeave;
                rolloverMarker.MouseLeftButtonDown += OnRolloverMarkerMouseLeave;
            }

            ModifierSurface.Children.Add(rolloverMarker);

            _rolloverMarkers.Add(rolloverMarker);
        }

        private void AttachTooltipLabelToMarker(FrameworkElement rolloverMarker, SeriesInfo seriesInfo)
        {
            var rolloverLabel = rolloverMarker.GetValue(RolloverLabelProperty) as FrameworkElement;
            if (rolloverLabel == null)
            {
                rolloverLabel = CreateFromTemplate(TooltipLabelTemplate, TooltipLabelTemplateSelector, seriesInfo);

                rolloverMarker.SetValue(RolloverLabelProperty, rolloverLabel);
            }

            rolloverLabel.DataContext = seriesInfo;
        }

        /// <summary>
        /// If the current modifier IsEnabled, and the Point is valid for the modifier, updates axis and chart overlays
        /// </summary>
        /// <param name="atPoint">The current mouse point</param>
        protected virtual void TryUpdateOverlays(Point atPoint)
        {
            // Update if the point is in bounds
            if (IsEnabledAt(atPoint))
            {
                TryUpdateAxesLabels(atPoint);
            }
        }

        /// <summary>
        /// If <see cref="TooltipModifierBase.ShowAxisLabels"/>, and the Point is valid for the modifier, updates axis labels
        /// </summary>
        /// <param name="showAxesLabelsAt">The current mouse point</param>
        protected void TryUpdateAxesLabels(Point showAxesLabelsAt)
        {
            if (ShowAxisLabels)
            {
                if (!IsLabelsCacheActual())
                {
                    RecreateLabels();
                }
            }
        }

        private void UpdateOverlays(Point mousePoint)
        {
            var hasMarkers = !_rolloverMarkers.IsNullOrEmpty();
            var clearOverlays = !hasMarkers || !HasToShowTooltip();
            // Update tooltips only if in corresponding mode and markers exist
            if (!clearOverlays)
            {
                if(ShowTooltipOn == ShowTooltipOptions.MouseHover)
                    _delayActionHelper.Start(() => UpdateTooltipLabels(_rolloverMarkers));
                else
                    UpdateTooltipLabels(_rolloverMarkers);
            }
            else if (ShowTooltipOn != ShowTooltipOptions.MouseOver || !hasMarkers)
            {
                ClearTooltipLabels();
            }

            // Update axis labels and vertical line
            TryUpdateOverlays(mousePoint);
        }

        private void ClearTooltipLabels()
        {
            if (ModifierSurface != null)
            {
                foreach (var label in _tooltipLabels)
                {
                    if (ModifierSurface.Children.Contains(label))
                    {
                        ModifierSurface.Children.Remove(label);
                    }
                }

                _tooltipLabels.Clear();

                foreach (var marker in _rolloverMarkers)
                {
                    RemoveLabelFor(marker);
                }
            }
        }

        private void OnRolloverMarkerMouseLeave(object sender, MouseEventArgs e)
        {
            ClearTooltipLabels();

            _needToUpdateTooltips = true;
        }

        private void OnRolloverMarkerMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(ModifierSurface as UIElement);

            var overlappedMarkers = GetRolloverMarkersAt(point);

            if (_needToUpdateTooltips)
            {
                UpdateTooltipLabels(overlappedMarkers);

                _needToUpdateTooltips = false;
            }
        }

        private List<FrameworkElement> GetRolloverMarkersAt(Point point)
        {
            var rollovers = new List<FrameworkElement>();

            foreach (var marker in _rolloverMarkers)
            {
                var rolloverRect = GetRolloverMarkerRect(marker);

                if (rolloverRect.Contains(point))
                {
                    rollovers.Add(marker);
                }
            }

            return rollovers;
        }

        private Rect GetRolloverMarkerRect(FrameworkElement rolloverMarker)
        {
            var y = (double)rolloverMarker.GetValue(Canvas.TopProperty);
            var x = (double)rolloverMarker.GetValue(Canvas.LeftProperty);

            var rolloverRect = new Rect(x, y, rolloverMarker.ActualWidth, rolloverMarker.ActualHeight);

            return rolloverRect;
        }

        private void UpdateTooltipLabels(List<FrameworkElement> overlappedMarkers)
        {
            ClearTooltipLabels();

            if (!overlappedMarkers.IsEmpty())
            {
                foreach (var marker in overlappedMarkers)
                {
                    MergeTooltipLabelFor(marker);
                }

                foreach (var tooltip in _tooltipLabels)
                {
                    if (!ModifierSurface.Children.Contains(tooltip))
                    {
                        (tooltip.Parent as Panel).SafeRemoveChild(tooltip);

                        ModifierSurface.Children.Add(tooltip);
                    }
                }
            }
        }

        private void MergeTooltipLabelFor(FrameworkElement rolloverMarker)
        {
            var tooltipLabel = (FrameworkElement)rolloverMarker.GetValue(RolloverLabelProperty);

            if (tooltipLabel != null)
            {
#if SILVERLIGHT
                MeasureVisualChild(tooltipLabel);
#endif
                var x = Canvas.GetLeft(rolloverMarker);
                var y = Canvas.GetTop(rolloverMarker);

                var markerRect = GetRolloverMarkerRect(rolloverMarker);
                var boundaryRect = new Rect(0, 0, ModifierSurface.ActualWidth, ModifierSurface.ActualHeight);

                var tooltip = GetMergedTooltip(new Point(x, y), tooltipLabel, markerRect, boundaryRect);

                _tooltipLabels.Add(tooltip);
            }
        }

        private FrameworkElement GetMergedTooltip(Point point, FrameworkElement tooltipLabel, Rect markerRect, Rect boundaryRect)
        {
            var mergedTooltip = tooltipLabel;

            var mergedTooltipRect = GetTooltipLabelRect(mergedTooltip, point, markerRect, boundaryRect);

            var overlayLabel = _tooltipLabels.FirstOrDefault(label =>
            {
                var xCoord = Canvas.GetLeft(label);
                var yCoord = Canvas.GetTop(label);
                var labelRect = new Rect(xCoord, yCoord, label.ActualWidth, label.ActualHeight);

                return labelRect.IntersectsWith(mergedTooltipRect);
            });

            if (overlayLabel != null)
            {
                // Launches the recursive call to merge all the labels which overlap
                //var mergeContainer = tooltipLabel as StackPanel ?? new StackPanel();
                mergedTooltip = MergeTwoTooltips(overlayLabel, tooltipLabel);

                mergedTooltip = GetMergedTooltip(point, mergedTooltip, markerRect, boundaryRect);
            }
            else
            {
                Canvas.SetLeft(mergedTooltip, mergedTooltipRect.X);
                Canvas.SetTop(mergedTooltip, mergedTooltipRect.Y);
            }

            return mergedTooltip;
        }

        private Rect GetTooltipLabelRect(FrameworkElement tooltip, Point point, Rect markerRect, Rect boundaryRect)
        {
#if SILVERLIGHT
            MeasureVisualChild(tooltip);
#else
            tooltip.MeasureArrange();
#endif

            // apply cursor offset only if in cursor hover mode
            double cursorOffset, verticalOffset;
            if (ShowTooltipOn == ShowTooltipOptions.MouseOver)
            {
                cursorOffset = 3d;
                verticalOffset = 0;
            }
            else
            {
                cursorOffset = 0d;
                verticalOffset = markerRect.Height / 2.0 + tooltip.ActualHeight / 2.0;
            }

            var x = markerRect.Right + cursorOffset;

            var panel = tooltip as Panel;
            var y = panel != null && panel.Children.Count > 1
                    ? point.Y - tooltip.ActualHeight / 2.0
                    : markerRect.Bottom - verticalOffset + cursorOffset;

            var labelRect = new Rect(x, y, tooltip.ActualWidth, tooltip.ActualHeight);

            if (boundaryRect.Right < labelRect.Right)
            {
                x = markerRect.Left - tooltip.ActualWidth - cursorOffset;
            }

            if (boundaryRect.Bottom < labelRect.Bottom)
            {
                var diff = labelRect.Bottom - boundaryRect.Bottom;
                y = y - diff - cursorOffset;
            }

            if (boundaryRect.Top > labelRect.Top)
            {
                var diff = boundaryRect.Top - labelRect.Top;
                y = y + diff + cursorOffset;
            }

            labelRect.X = x;
            labelRect.Y = y;

            return labelRect;
        }

        private StackPanel MergeTwoTooltips(FrameworkElement tooltip1, FrameworkElement tooltip2)
        {
            var container1 = tooltip1 as StackPanel;
            var container2 = tooltip2 as StackPanel;

            StackPanel mergedTooltip;
            if (container1 != null)
            {
                container1.SafeAddChild(tooltip2);

                mergedTooltip = container1;
            }
            else if(container2 != null)
            {
                container2.SafeAddChild(tooltip1, 0);

                mergedTooltip = container2;
            }
            else
            {
                mergedTooltip = new StackPanel();

                mergedTooltip.SafeAddChild(tooltip1);
                mergedTooltip.SafeAddChild(tooltip2);
            }

            _tooltipLabels.Remove(tooltip1);
            _tooltipLabels.Remove(tooltip2);

            return mergedTooltip;
        }

        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        protected override void ClearAll()
        {
            RemoveMarkers();
            ClearTooltipLabels();
            ClearAxesOverlay();

            _delayActionHelper.Stop();
        }

        /// <summary>
        /// When overriden in a derived class, applies new template to tooltip label, <seealso cref="TooltipModifierBase.TooltipLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.TooltipLabelTemplate" />
        /// </summary>
        protected override void OnTooltipLabelTemplateChanged()
        {
            if (ParentSurface != null && ParentSurface.RenderableSeries != null)
            {
                ParentSurface.RenderableSeries
                    .Where(series => series.RolloverMarker != null &&
                                     series.RolloverMarker.DataContext != null)
                    .ForEachDo(
                        series => series.RolloverMarker.SetValue(RolloverLabelProperty,
                                                                 CreateFromTemplate(TooltipLabelTemplate,
                                                                                    TooltipLabelTemplateSelector,
                                                                                    series.RolloverMarker.DataContext)));
            }
        }

        /// <summary>
        /// When overriden in a derived class, applies new template to axis labels, <seealso cref="TooltipModifierBase.AxisLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.AxisLabelTemplate" />
        /// </summary>
        protected override void OnAxisLabelTemplateChanged()
        {
            RecreateLabels();
        }

        /// <summary>
        /// Called when the parent surface SelectedSeries collection changes
        /// </summary>
        /// <param name="oldSeries"></param>
        /// <param name="newSeries"></param>
        protected override void OnSelectedSeriesChanged(IEnumerable<IRenderableSeries> oldSeries, IEnumerable<IRenderableSeries> newSeries)
        {
            base.OnSelectedSeriesChanged(oldSeries, newSeries);

            RemoveMarkers(true);
        }
    }
}