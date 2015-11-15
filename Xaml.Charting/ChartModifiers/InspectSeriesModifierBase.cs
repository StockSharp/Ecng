// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// InspectSeriesModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines constants for different series sources in <see cref="UltrachartSurface"/>
    /// </summary>
    public enum SourceMode
    {
        /// <summary>
        /// The <see cref="InspectSeriesModifierBase"/> uses All Series as inputs
        /// </summary>
        AllSeries,

        /// <summary>
        /// The <see cref="InspectSeriesModifierBase"/> uses All Visible Series as inputs
        /// </summary>
        AllVisibleSeries,

        /// <summary>
        /// The <see cref="InspectSeriesModifierBase"/> uses Selected series as inputs
        /// </summary>
        SelectedSeries,

        /// <summary>
        /// The <see cref="InspectSeriesModifierBase"/> uses Unselected series as inputs
        /// </summary>
        UnselectedSeries,
    }

    /// <summary>
    /// An abstract base class which factors out handling of Axis and Chart Label templates which are shared in the <see cref="CursorModifier"/> and <see cref="RolloverModifier"/>.
    /// </summary>
    /// <seealso cref="RolloverModifier"/>
    /// <seealso cref="CursorModifier"/>
    public abstract class InspectSeriesModifierBase: ChartModifierBase
    {
        /// <summary>
        /// Defines the UseInterpolation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty UseInterpolationProperty = DependencyProperty.Register("UseInterpolation", typeof(bool), typeof(InspectSeriesModifierBase), new PropertyMetadata(false));

        /// <summary>
        /// Defines the RolloverMode DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SourceModeProperty = DependencyProperty.Register("SourceMode", typeof(SourceMode), typeof(InspectSeriesModifierBase), new PropertyMetadata(SourceMode.AllVisibleSeries));    

        /// <summary>
        /// Defines the SeriesData Dependency property which you may bind to in your applications to show cursor updates on mouse-move
        /// </summary>
        public static readonly DependencyProperty SeriesDataProperty = DependencyProperty.Register("SeriesData", typeof(ChartDataObject), typeof(InspectSeriesModifierBase), new PropertyMetadata(null));

        /// <summary>
        /// The Current MousePoint on the parent <see cref="UltrachartSurface.ModifierSurface"/>
        /// </summary>
        protected Point CurrentPoint = new Point(Double.NaN, Double.NaN);

        private bool _isMaster;

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectSeriesModifierBase"/> class.
        /// </summary>
        protected InspectSeriesModifierBase()
        {
			this.SetCurrentValue(SourceModeProperty, SourceMode.AllVisibleSeries);
            this.SetCurrentValue(ExecuteOnProperty, ExecuteOn.MouseMove);
        }

        
        /// <summary>
        /// Gets or sets the <see cref="ChartDataObject"/> which may be bound to, to provide feedback to the user of cursor updates
        /// </summary>
        /// <value>The series data.</value>
        /// <remarks></remarks>
        public ChartDataObject SeriesData
        {
            get { return (ChartDataObject)GetValue(SeriesDataProperty); }
            set { SetValue(SeriesDataProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the interaction should use interpolation
        /// </summary>
        public bool UseInterpolation
        {
            get { return (bool)GetValue(UseInterpolationProperty); }
            set { SetValue(UseInterpolationProperty, value); }
        }

        /// <summary>
        /// Gets or sets type of series on which interaction is performed
        /// </summary>
        public SourceMode SourceMode
        {
            get { return (SourceMode)GetValue(SourceModeProperty); }
            set { SetValue(SourceModeProperty, value); }
        }
        
        /// <summary>
        /// Called when the parent <see cref="UltrachartSurface"/> is rendered
        /// </summary>
        /// <param name="e">The <see cref="UltrachartRenderedMessage"/> which contains the event arg data</param>
        public override void OnParentSurfaceRendered(UltrachartRenderedMessage e)
        {
            base.OnParentSurfaceRendered(e);

            if (IsEnabled)
                HandleMouseEvent(CurrentPoint);
        }

        /// <summary>
        /// Called when the mouse leaves the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected override void OnParentSurfaceMouseLeave()
        {
            CurrentPoint.X = CurrentPoint.Y = double.NaN;

            ClearAll();
        }

        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface"/>
        /// </summary>
        protected abstract void ClearAll();

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            base.OnModifierMouseMove(e);

            HandleMouseEvent(e);
            e.Handled = false;
        }

        /// <summary>
        /// General logic for processing mouse events
        /// </summary>
        /// <param name="e"></param>
        protected void HandleMouseEvent(ModifierMouseArgs e)
        {
            var canExecute = IsInteractionEnabled(e);
            var isHandled = false;

            if (canExecute)
            {
                var relativeToModifierSurface = GetPointRelativeTo(e.MousePoint, ModifierSurface);

                CurrentPoint = relativeToModifierSurface;
                _isMaster = e.IsMaster;

                isHandled = HandleMouseEvent(CurrentPoint);
            }

            e.Handled = isHandled;
        }

        private bool IsInteractionEnabled(ModifierMouseArgs e)
        {
            return ModifierSurface != null && IsEnabled && (MatchesExecuteOn(e.MouseButtons, ExecuteOn));
        }

        private bool HandleMouseEvent(Point relativeToModifierSurface)
        {
            var isEnabled = IsEnabledAt(relativeToModifierSurface);
            if (isEnabled)
            {
                if (_isMaster)
                {
                    HandleMasterMouseEvent(relativeToModifierSurface);
                }
                else
                {
                    HandleSlaveMouseEvent(relativeToModifierSurface);
                }
            }
            else
            {
                ClearAll();
            }

            return isEnabled;
        }

        /// <summary>
        /// When overridden in derived classes, indicates whether mouse point is valid for current modifier
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected virtual bool IsEnabledAt(Point point)
        {
            var enabled = point.X.IsDefined() && point.Y.IsDefined() &&
                // this check prevents a bug, 
                // when shared cursor isn't shown if surfaces have different heights
                point.X >= 0 && point.X <= ModifierSurface.ActualWidth;

            // prevents cursor from being shown if mouse pointer is over the X axis
            if (_isMaster)
            {
                enabled &= point.Y >= 0 && point.Y <= ModifierSurface.ActualHeight;
            }

            return enabled;
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase"/> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected abstract void HandleMasterMouseEvent(Point mousePoint);

        /// <summary>
        /// When overriden in a derived class, called to handle the Slave <see cref="ChartModifierBase"/> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected abstract void HandleSlaveMouseEvent(Point mousePoint);

        /// <summary>
        /// Performs hit-test on <paramref name="axis"/>, used internally by modifiers
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="atPoint"></param>
        /// <returns></returns>
        protected AxisInfo HitTestAxis(IAxis axis, Point atPoint)
        {
            var axisInfo = axis.HitTest(atPoint);

            if (axisInfo != null)
            {
                axisInfo.IsMasterChartAxis = _isMaster;
            }

            return axisInfo;
        }

        /// <summary>
        /// Enumerates RenderableSeries on the parent <see cref="ChartModifierBase.ParentSurface"/> and gets <see cref="SeriesInfo"/> objects in given point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected virtual IEnumerable<SeriesInfo> GetSeriesInfoAt(Point point)
        {
            return GetSeriesInfoAt(renderSeries => renderSeries.HitTest(point, UseInterpolation));
        }

        /// <summary>
        /// Enumerates RenderableSeries on the parent <see cref="ChartModifierBase.ParentSurface"/> and gets <see cref="SeriesInfo"/> objects in given point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="hitTestRadius"></param>
        /// <returns></returns>
        protected virtual IEnumerable<SeriesInfo> GetSeriesInfoAt(Point point, double hitTestRadius)
        {
            return GetSeriesInfoAt(renderSeries => renderSeries.HitTest(point, hitTestRadius, UseInterpolation));
        }

        /// <summary>
        /// Called internally, gets the SeriesInfo on all RenderableSeries using the provided hit-test function
        /// </summary>
        /// <param name="hitTestMethod">The hit-test function</param>
        /// <returns>The seriesinfo list</returns>
        protected IEnumerable<SeriesInfo> GetSeriesInfoAt(Func<IRenderableSeries, HitTestInfo> hitTestMethod)
        {
            if (ParentSurface != null && !ParentSurface.RenderableSeries.IsNullOrEmpty())
            {
                foreach (var renderableSeries in ParentSurface.RenderableSeries)
                {
                    if (IsSeriesValid(renderableSeries))
                    {
                        // Given an XY point in screen space
                        // How do we transform this to snap to the series? 
                        // X Value must snap to X Value of nearest point
                        // Y Value must snap to Y Value of series at that X value
                        HitTestInfo hitTestInfo = hitTestMethod(renderableSeries);

                        if (IsHitPointValid(hitTestInfo))
                        {
                            var seriesInfo = renderableSeries.GetSeriesInfo(hitTestInfo);
                            yield return seriesInfo;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// When overridden in derived classes, indicates whether <see cref="HitTestInfo"/> result of hit-test should be returned from the <see cref="GetSeriesInfoAt(Point)"/> method.
        /// </summary>
        /// <param name="hitTestInfo"></param>
        /// <returns></returns>
        protected virtual bool IsHitPointValid(HitTestInfo hitTestInfo)
        {
            var isValid = !hitTestInfo.IsEmpty() && hitTestInfo.IsHit;

            return isValid;
        }

        /// <summary>
        /// When overridden in derived classes, indicates whether the series should be inspected in order to get <see cref="SeriesInfo"/> inside the <see cref="GetSeriesInfoAt(Point)"/> method.
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        protected virtual bool IsSeriesValid(IRenderableSeries series)
        {
            return series != null && CheckSeriesMode(series) && series.DataSeries != null;
        }

        private bool CheckSeriesMode(IRenderableSeries series)
        {
            var result = SourceMode == SourceMode.AllSeries ||
                         series.IsVisible && SourceMode == SourceMode.AllVisibleSeries ||
                         series.IsSelected && SourceMode == SourceMode.SelectedSeries ||
                         !series.IsSelected && SourceMode == SourceMode.UnselectedSeries;

            return result;
        }
    }
}
