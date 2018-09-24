// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// VerticalSliceModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="VerticalSliceModifier"/> provides drawing of vertical slices based on collection of <see cref="VerticalLineAnnotation"/>
    /// Add to a <see cref="UltrachartSurface"/> and set VerticalLines collection to enable this behaviour
    /// </summary>
    public class VerticalSliceModifier : VerticalSliceModifierBase
    {
        /// <summary>
        /// Defined IncludeSeries Attached Property
        /// </summary>
        public static readonly DependencyProperty IncludeSeriesProperty = DependencyProperty.RegisterAttached("IncludeSeries", typeof(bool), typeof(VerticalSliceModifier), new PropertyMetadata(true));

        /// <summary>
        /// Gets the include Series or not
        /// </summary>
        public static bool GetIncludeSeries(DependencyObject obj)
        {
            return (bool)obj.GetValue(IncludeSeriesProperty);
        }

        /// <summary>
        /// Sets the include Series or not
        /// </summary>
        public static void SetIncludeSeries(DependencyObject obj, bool value)
        {
            obj.SetValue(IncludeSeriesProperty, value);
        }

        /// <summary>
        ///  Defines the VerticalLines DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VerticalLinesProperty = DependencyProperty.Register("VerticalLines", typeof(VerticalLineAnnotationCollection), typeof(VerticalSliceModifier), new PropertyMetadata(null, OnVerticalLinesDependencyPropertyChanged));

        private Dictionary<BaseRenderableSeries, ObjectPool<TemplatableControl>> rolloverMarkersDictionary = new Dictionary<BaseRenderableSeries, ObjectPool<TemplatableControl>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalSliceModifier"/> class.
        /// </summary>
        public VerticalSliceModifier()
        {
            DefaultStyleKey = typeof(VerticalSliceModifier);
            this.SetCurrentValue(SeriesDataProperty, new ChartDataObject());

            this.SetCurrentValue(VerticalLinesProperty, new VerticalLineAnnotationCollection());

#if SILVERLIGHT
            // Fixes wrong drawing of chart in SL when it loads first time
            Loaded += (sender, args) =>
            {
                if (ParentSurface != null)
                {
                    ParentSurface.InvalidateElement();
                }
            };
#endif
        }

        /// <summary>
        /// Gets or sets <see cref="VerticalLineAnnotationCollection"/> of <see cref="VerticalLineAnnotation"/> for making vertical slices
        /// </summary>
        public VerticalLineAnnotationCollection VerticalLines
        {
            get { return (VerticalLineAnnotationCollection)GetValue(VerticalLinesProperty); }
            set { SetValue(VerticalLinesProperty, value); }
        }

        /// <summary>
        /// Enumerates the RenderableSeries on the parent <see cref="ChartModifierBase.ParentSurface" /> and gets <see cref="SeriesInfo" /> objects in given point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected override IEnumerable<SeriesInfo> GetSeriesInfoAt(Point point)
        {
            var annotations = VerticalLines.Where(x => !x.IsHidden && x.IsAttached && x.XAxis != null && x.XAxis.IsHorizontalAxis);

            foreach (var annotation in annotations)
            {
                if (annotation.X1 == null || annotation.XAxis == null) continue;

                var xCoord = annotation.XAxis.GetCoordinate(annotation.X1);

                foreach (var seriesInfo in base.GetSeriesInfoAt(new Point(xCoord, 0)))
                {
                    yield return seriesInfo;
                }
            }
        }

        /// <summary>
        /// Get rollover marker from <see cref="SeriesInfo"/> to place on chart 
        /// </summary>
        /// <param name="seriesInfo"></param>
        /// <returns></returns>
        protected override FrameworkElement GetRolloverMarkerFrom(SeriesInfo seriesInfo)
        {
            var renderableSeries = seriesInfo.RenderableSeries as BaseRenderableSeries;
            if (renderableSeries == null) return null;

            ObjectPool<TemplatableControl> pool;

            if (!rolloverMarkersDictionary.TryGetValue(renderableSeries, out pool))
            {
                pool = new ObjectPool<TemplatableControl>();
                rolloverMarkersDictionary.Add(renderableSeries, pool);
            }

            var rolloverMarker =
                pool.Get(() => PointMarker.CreateFromTemplate(renderableSeries.RolloverMarkerTemplate, seriesInfo));

            return rolloverMarker;
        }

        /// <summary>
        /// Detaches a RolloverMarker from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        /// <param name="rolloverMarker">The rollover marker.</param>
        protected override void DetachRolloverMarker(FrameworkElement rolloverMarker)
        {
            base.DetachRolloverMarker(rolloverMarker);

            var seriesInfo = rolloverMarker.DataContext as SeriesInfo;
            if (seriesInfo == null)
                return;

            var renderableSeries = (BaseRenderableSeries)seriesInfo.RenderableSeries;
            var pool = rolloverMarkersDictionary[renderableSeries];

            pool.Put((TemplatableControl)rolloverMarker);
        }

        /// <summary>
        /// Called when the mouse leaves the parent <see cref="UltrachartSurface" />
        /// </summary>
        protected override void OnParentSurfaceMouseLeave()
        {

        }

        /// <summary>
        /// When overridden in derived classes, indicates whether mouse point is valid for current modifier
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected override bool IsEnabledAt(Point point)
        {
            return true;
        }

        protected override void FillWithIncludedSeries(IEnumerable<SeriesInfo> infos, ObservableCollection<SeriesInfo> seriesInfos)
        {
            infos.ForEachDo(info =>
            {
                var includeSeries = info.RenderableSeries.GetIncludeSeries(Modifier.VerticalSlice);
                if (includeSeries)
                {
                    seriesInfos.Add(info);
                }
            });
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase" /> instance
        /// </summary>
        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();

            if (IsEnabled)
                OnAttached();
            else
            {
                OnDetached();
            }
        }

        /// <summary>
        /// Called when the element is attached to the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnAttached()
        {
            base.OnAttached();

            SubscribeAnnotationsChangedNotification();
        }

        private void SubscribeAnnotationsChangedNotification()
        {
            var scs = ParentSurface;
            if (scs != null)
            {
                scs.AnnotationsCollectionNewCollectionAssigned -= OnAnnotationsDrasticallyChanged;
                scs.AnnotationsCollectionNewCollectionAssigned += OnAnnotationsDrasticallyChanged;

                VerticalLines.CollectionChanged -= OnVerticalLinesCollectionChanged;
                VerticalLines.CollectionChanged += OnVerticalLinesCollectionChanged;

                foreach (var annotation in VerticalLines)
                {
                    ParentSurface.Annotations.Add(annotation);
                }

                OnAnnotationsDrasticallyChanged(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached()
        {
            base.OnDetached();

            UnsubscribeAnnotationsChangedNotification();
        }

        private void UnsubscribeAnnotationsChangedNotification()
        {
            if (VerticalLines != null)
            {
                VerticalLines.CollectionChanged -= OnVerticalLinesCollectionChanged;

                var scs = ParentSurface;
                if (scs != null)
                {
                    scs.AnnotationsCollectionNewCollectionAssigned -= OnAnnotationsDrasticallyChanged;

                    if (scs.Annotations != null)
                    {
                        scs.Annotations.CollectionChanged -= OnAnnotationsCollectionChanged;                        
                    }                    
                }

                foreach (var verticalLineAnnotation in VerticalLines.ToArray())
                {
                    DetachVerticalLine(verticalLineAnnotation);
                }
            }            
        }

        private void OnVerticalLinesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var newLines = e.NewItems;
            var oldLines = e.OldItems;

            if (oldLines != null)
            {
                oldLines.OfType<VerticalLineAnnotation>().ForEachDo(DetachVerticalLine);
            }
            
            if (newLines != null)
            {
                newLines.OfType<VerticalLineAnnotation>().ForEachDo(AttachVerticalLine);
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                VerticalLines.OldItems.ForEachDo(DetachVerticalLine);
            }

            HandleMasterMouseEvent(CurrentPoint);
        }

        private void AttachVerticalLine(VerticalLineAnnotation annotation)
        {
            DetachVerticalLine(annotation);

            annotation.PropertyChanged += OnPropertyChanged;

            if (ParentSurface != null && ParentSurface.Annotations != null)
            {
                ParentSurface.Annotations.Add(annotation);
            }
        }

        private void DetachVerticalLine(VerticalLineAnnotation annotation)
        {
            annotation.IsHiddenChanged -= OnAnnotationStateChanged;
            annotation.PropertyChanged -= OnPropertyChanged;

            if (ParentSurface != null && ParentSurface.Annotations != null)
            {
                ParentSurface.Annotations.Remove(annotation);
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "PositionChanged")
            {
                HandleMasterMouseEvent(new Point());
            }
        }

        private void OnAnnotationStateChanged(object sender, EventArgs args)
        {
            HandleMasterMouseEvent(new Point());
        }

        private void OnAnnotationsDrasticallyChanged(object sender, EventArgs e)
        {
            if (ParentSurface == null || ParentSurface.Annotations == null)
                return;

            ParentSurface.Annotations.CollectionChanged -= OnAnnotationsCollectionChanged;
            ParentSurface.Annotations.CollectionChanged += OnAnnotationsCollectionChanged;

            OnAnnotationsCollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            VerticalLines.OldItems.ForEachDo(VerticalLines.Add);
        }

        private void OnAnnotationsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ParentSurface == null || ParentSurface.Annotations == null)
                return;

            var annotationCollection = VerticalLines;
            var oldLines = e.OldItems;

            // Remove lines from VerticalLines if they're removed from Annotations
            if (oldLines != null)
            {
                oldLines.OfType<VerticalLineAnnotation>().ForEachDo(annotation => annotationCollection.Remove(annotation));
            }

            if (e.Action==NotifyCollectionChangedAction.Reset)
            {
                annotationCollection.Clear();

                ParentSurface.InvalidateElement();
            }
        }

        private static void OnVerticalLinesDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sliceModifier = (VerticalSliceModifier) d;

            var oldLines = e.OldValue as VerticalLineAnnotationCollection;
            var newLines = e.NewValue as VerticalLineAnnotationCollection;

            if (oldLines != null)
            {
                oldLines.CollectionChanged -= sliceModifier.OnVerticalLinesCollectionChanged;

                oldLines.ForEachDo(sliceModifier.DetachVerticalLine);
            }

            if (newLines != null)
            {
                newLines.CollectionChanged += sliceModifier.OnVerticalLinesCollectionChanged;

                newLines.ForEachDo(sliceModifier.AttachVerticalLine);
            }
        }

        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
        }
    }
}
