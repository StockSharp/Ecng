// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SeriesSelectionModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Provides the ability to select series via a Chart Modifier
    /// </summary>
    public class SeriesSelectionModifier : InspectSeriesModifierBase
    {
        /// <summary>
        /// Defines the SelectedSelectedSeriesStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectedSeriesStyleProperty =
            DependencyProperty.Register("SelectedSeriesStyle", typeof(Style), typeof(SeriesSelectionModifier), new PropertyMetadata(OnSelectedSeriesStyleChanged));

        /// <summary>
        /// Event raised when the selection changes
        /// </summary>
        public event EventHandler<EventArgs> SelectionChanged;

        private bool _isGroupSelection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesSelectionModifier"/> class.
        /// </summary>
        public SeriesSelectionModifier()
        {
            this.SetCurrentValue(UseInterpolationProperty, true);
            this.SetCurrentValue(ExecuteOnProperty, ExecuteOn.MouseLeftButton);
        }

        /// <summary>
        /// Gets or sets a style for selected series.
        /// </summary>
        /// <value>The style of the selected series.</value>
        /// <remarks></remarks>
        public Style SelectedSeriesStyle
        {
            get { return (Style)GetValue(SelectedSeriesStyleProperty); }
            set { SetValue(SelectedSeriesStyleProperty, value); }
        }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        public override void OnAttached()
        {
            base.OnAttached();

            ApplySelection();
        }

        private void ApplySelection()
        {
            if (ParentSurface != null)
            {
                ParentSurface.SelectedRenderableSeries.ForEachDo(TrySetStyle);
            }
        }

        /// <summary>
        /// Called when the parent surface SelectedSeries collection changes
        /// </summary>
        /// <param name="oldSeries"></param>
        /// <param name="newSeries"></param>
        protected override void OnSelectedSeriesChanged(System.Collections.Generic.IEnumerable<IRenderableSeries> oldSeries, System.Collections.Generic.IEnumerable<IRenderableSeries> newSeries)
        {
            base.OnSelectedSeriesChanged(oldSeries, newSeries);

            if (newSeries != null)
            {
                ApplySelection();
            }
        }

        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        protected override void ClearAll() { }

        /// <summary>
        /// Called when the parent <see cref="UltrachartSurface" /> is rendered
        /// </summary>
        /// <param name="e">The <see cref="UltrachartRenderedMessage" /> which contains the event arg data</param>
        public override void OnParentSurfaceRendered(UltrachartRenderedMessage e)
        {
            // Overrides the implementation in base class
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            base.OnModifierMouseUp(e);

            _isGroupSelection = e.Modifier == MouseModifier.Ctrl;

            HandleMouseEvent(e);
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Slave <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleSlaveMouseEvent(Point mousePoint)
        {
            HandleMasterMouseEvent(mousePoint);
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleMasterMouseEvent(Point mousePoint)
        {
            var hasSelected = !ParentSurface.SelectedRenderableSeries.IsNullOrEmpty();

            var infos = GetSeriesInfoAt(mousePoint).ToArray();

            // If a series was hit, select it
            // and deselect previously selected series unless Ctrl is pressed
            if (infos.Any())
            {
                var series = infos.First().RenderableSeries;
                if (_isGroupSelection)
                {
                    PerformSelection(series);
                }
                else
                {
                    DeselectAllBut(series);
                }

                OnSelectionChanged();
            }
            else if (hasSelected)
            {
                // Any series was hit, so deselect all
                DeselectAll();
                OnSelectionChanged();
            }
        }

        protected virtual void DeselectAllBut(IRenderableSeries series)
        {
            var select = !series.IsSelected || ParentSurface.SelectedRenderableSeries.Count > 1;

            DeselectAll();

            if (select)
            {
                PerformSelection(series);
            }
        }

        protected virtual void PerformSelection(IRenderableSeries series)
        {
            // Handle the selection/deselection of a series
            series.IsSelected = !series.IsSelected;

            if (series.IsSelected)
            {
                TrySetStyle(series);
            }
        }

        protected virtual void DeselectAll()
        {
            if (ParentSurface.SelectedRenderableSeries.Count == 0) return;

            for (int i = ParentSurface.SelectedRenderableSeries.Count - 1; i >= 0; i--)
            {
                ParentSurface.SelectedRenderableSeries[i].IsSelected = false;
            }
        }

        protected virtual void TrySetStyle(IRenderableSeries series)
        {
            if (SelectedSeriesStyle != null)
            {
                var targetType = SelectedSeriesStyle.TargetType;

                var isTarget = targetType.IsAssignableFrom(series.GetType());

                if (series.SelectedSeriesStyle == null &&
                    isTarget)
                {
                    series.SelectedSeriesStyle = SelectedSeriesStyle;
                }
            }
        }

        private void OnSelectionChanged()
        {
            var handler = SelectionChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private static void OnSelectedSeriesStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = (SeriesSelectionModifier)d;

            modifier.ApplySelection();
        }
    }
}
