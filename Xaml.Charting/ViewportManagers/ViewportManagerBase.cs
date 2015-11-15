// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ViewportManagerBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Threading;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines a base class for a ViewportManager, which may be used to control visible range and ranging on a <see cref="UltrachartSurface"/>
    /// </summary>
    public abstract class ViewportManagerBase : IViewportManager
    {
        private IUltrachartSurface _scs;
        private IServiceContainer _services;

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        public IServiceContainer Services
        {
            get { return _services; }
            set { _services = value; }
        }

        /// <summary>
        /// Gets the value indicating whether a <see cref="ViewportManagerBase"/> has the <see cref="UltrachartSurface"/> attached to.
        /// </summary>
        public bool IsAttached { get; private set; }

        /// <summary>
        /// Called when the <see cref="ViewportManagerBase"/> is attached to a parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="scs">The UltrachartSurface instance</param>
        public virtual void AttachUltrachartSurface(IUltrachartSurface scs)
        {
            _scs = scs;
            _services = _scs.Services;

            IsAttached = true;
        }

        /// <summary>
        /// Called when the <see cref="ViewportManagerBase"/> is detached from a parent <see cref="UltrachartSurface"/>
        /// </summary>
        public virtual void DetachUltrachartSurface()
        {
            _scs = null;
            _services = null;

            IsAttached = false;
        }

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        public bool IsSuspended { get { return UpdateSuspender.GetIsSuspended(this); } }

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>
        /// The disposable Update Suspender
        /// </returns>
        public IUpdateSuspender SuspendUpdates()
        {
            return new UpdateSuspender(this);
        }

        /// <summary>
        /// Resumes updates on the target, intended to be called by IUpdateSuspender
        /// </summary>
        /// <param name="suspender"></param>
        public void ResumeUpdates(IUpdateSuspender suspender)
        {
        }

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        public void DecrementSuspend()
        {
        }

        /// <summary>
        /// Asynchronously requests that the element redraws itself plus children.
        /// Will be ignored if the element is ISuspendable and currently IsSuspended (within a SuspendUpdates/ResumeUpdates call)
        /// </summary>
        public void InvalidateElement()
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.InvalidateElement());
            }
        }

        private void SafeInvoke(Action action)
        {
            var dispatcher = _services.GetService<IDispatcherFacade>();
            dispatcher.BeginInvokeIfRequired(action, DispatcherPriority.ApplicationIdle);
        }

        /// <summary>
        /// Zooms the chart to the extents of the data, plus any X or Y Grow By fraction set on the X and Y Axes
        /// </summary>
        public void ZoomExtents()
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.ZoomExtents());
            }
        }

        /// <summary>
        /// Zooms to extents with the specified animation duration
        /// </summary>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        public void AnimateZoomExtents(TimeSpan duration)
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.AnimateZoomExtents(duration));
            }
        }

        /// <summary>
        /// Zooms the chart to the extents of the data in the Y-Direction, accounting for the current data in view in the X-direction
        /// </summary>
        public void ZoomExtentsY()
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.ZoomExtentsY());
            }
        }

        /// <summary>
        /// Zooms the chart to the extents of the data in the Y-Direction, accounting for the current data in view in the X-direction
        /// </summary>
        /// <param name="duration"></param>
        public void AnimateZoomExtentsY(TimeSpan duration)
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.AnimateZoomExtentsY(duration));
            }
        }

        /// <summary>
        /// Zooms the chart to the extents of the data in the X-Direction
        /// </summary>
        public void ZoomExtentsX()
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.ZoomExtentsX());
            }
        }

        /// <summary>
        /// Zooms the chart to the extents of the data in the X-Direction
        /// </summary>
        /// <param name="duration"></param>
        public void AnimateZoomExtentsX(TimeSpan duration)
        {
            if (IsAttached)
            {
                SafeInvoke(() => _scs.AnimateZoomExtentsX(duration));
            }
        }

        /// <summary>
        /// Overridden by derived types, called when the parent <see cref="UltrachartSurface" /> requests to perform autoranging.
        /// The Range returned by this method will be applied to the chart on render
        /// </summary>
        /// <param name="axis">The axis</param>
        /// <returns>
        /// The new VisibleRange for the YAxis
        /// </returns>
        public virtual IRange CalculateAutoRange(IAxis axis)
        {
            if (axis.AutoRange == AutoRange.Always || axis.AutoRange == AutoRange.Once)
            {
                var newXRange = axis.GetMaximumRange();

                if (newXRange != null && newXRange.IsDefined)
                {
                    return newXRange;
                }
            }

            return axis.VisibleRange;
        }

        /// <summary>
        /// Called by the <see cref="UltrachartSurface" /> during render to calculate the new XAxis VisibleRange. Override in derived types to return a custom value
        /// </summary>
        /// <param name="xAxis"></param>
        /// <returns>
        /// The new <see cref="IRange" /> VisibleRange for the axis
        /// </returns>
        public IRange CalculateNewXAxisRange(IAxis xAxis)
        {
            if (!IsSuspended)
            {
                return OnCalculateNewXRange(xAxis);
            }

            return xAxis.VisibleRange;
        }

        /// <summary>
        /// Called by the <see cref="UltrachartSurface" /> during render to calculate the new YAxis VisibleRange. Override in derived types to return a custom value
        /// </summary>
        /// <param name="yAxis">The YAxis to calculate for</param>
        /// <param name="renderPassInfo">The current <see cref="RenderPassInfo" /> containing render data</param>
        /// <returns>
        /// The new <see cref="IRange" /> VisibleRange for the axis
        /// </returns>
        public IRange CalculateNewYAxisRange(IAxis yAxis, RenderPassInfo renderPassInfo)
        {
            if (!IsSuspended)
            {
                return OnCalculateNewYRange(yAxis, renderPassInfo);
            }

            return yAxis.VisibleRange;
        }

        /// <summary>
        /// Overridden by derived types, called when the parent <see cref="UltrachartSurface"/> requests the XAxis VisibleRange. 
        /// 
        /// The Range returned by this method will be applied to the chart on render
        /// </summary>
        /// <param name="xAxis">The XAxis</param>
        /// <returns>The new VisibleRange for the XAxis</returns>
        protected abstract IRange OnCalculateNewXRange(IAxis xAxis);

        /// <summary>
        /// Overridden by derived types, called when the parent <see cref="UltrachartSurface" /> requests a YAxis VisibleRange.
        /// The Range returned by this method will be applied to the chart on render
        /// </summary>
        /// <param name="yAxis">The YAxis</param>
        /// <param name="renderPassInfo">The render pass info.</param>
        /// <returns>
        /// The new VisibleRange for the YAxis
        /// </returns>
        protected abstract IRange OnCalculateNewYRange(IAxis yAxis, RenderPassInfo renderPassInfo);

        /// <summary>
        /// Called when the <see cref="IAxisParams.VisibleRange"/> changes for an axis. Override in derived types to get a notification of this occurring
        /// </summary>
        /// <param name="axis">The <see cref="IAxis"/>instance</param>
        public virtual void OnVisibleRangeChanged(IAxis axis)
        {
        }

        /// <summary>
        /// Called when the <see cref="IUltrachartSurface" /> is rendered.
        /// </summary>
        /// <param name="ultraChartSurface">The UltrachartSurface instance</param>
        public virtual void OnParentSurfaceRendered(IUltrachartSurface ultraChartSurface)
        {
        }

        /// <summary>
        /// Triggers a redraw on the parent surface with the specified RangeMode
        /// </summary>
        /// <param name="rangeMode">The <see cref="RangeMode"/> specifying how to redraw the parent surface</param>
        public void InvalidateParentSurface(RangeMode rangeMode)
        {
            if (IsAttached)
            {
                var eventAggregator = Services.GetService<IEventAggregator>();

                switch (rangeMode)
                {
                    case RangeMode.None:
                        eventAggregator.Publish(new InvalidateUltrachartMessage(this));
                        break;
                    case RangeMode.ZoomToFit:
                        eventAggregator.Publish(new ZoomExtentsMessage(this));
                        break;
                    case RangeMode.ZoomToFitY:
                        eventAggregator.Publish(new ZoomExtentsMessage(this, zoomYOnly: true));
                        break;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:InvalidateParentSurface" /> event.
        /// </summary>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs" /> instance containing the event data.</param>
        protected void OnInvalidateParentSurface(DependencyPropertyChangedEventArgs e)
        {
            if (IsAttached)
            {
                Services.GetService<IEventAggregator>().Publish(new InvalidateUltrachartMessage(this));
            }
        }
    }
}