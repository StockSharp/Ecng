// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
//
// AnnotationResizeAdorner.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart.
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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines the inteface to an annotation resize adorner, which is placed over an <see cref="AnnotationBase"/>
    /// when selected, allowing the user to move or resize it by dragging
    /// </summary>
    public interface IAnnotationResizeAdorner : IAnnotationAdorner
    {
        /// <summary>
        /// Gets the Adorner Markers placed by this annotation (e.g. grippers to resize, move)
        /// </summary>
        IEnumerable<Thumb> AdornerMarkers { get; }
    }

    internal class AnnotationResizeAdorner: AdornerBase, IAnnotationResizeAdorner
    {
        private readonly List<Thumb> _adornerMarkers = new List<Thumb>();

        private Point[] _points;

#if SILVERLIGHT
        private Point[] _previousPoints;

        private int _lastMarkerIndex;
#endif
        private double _horizontalChange, _verticalChange;

        private bool _inUpdate;

        public AnnotationResizeAdorner(IAnnotation adornedElement)
            : base(adornedElement)
        {
        }

        public override void Initialize()
        {
            Clear();

            UpdatePositions();
        }

        private void AttachMarkerAt(Point point)
        {
            var marker = new Thumb();

            var adorned = AdornedAnnotation as AnnotationBase;
            if (adorned != null && adorned.ResizingGripsStyle != null)
            {
                marker.Style = adorned.ResizingGripsStyle;
            }

            ParentCanvas.Children.Add(marker);

            marker.DragStarted += OnDragMarkerStarted;
            marker.DragDelta += OnDragMarker;
            marker.DragCompleted += OnDragMarkerCompleted;

            _adornerMarkers.Add(marker);
        }

        private void OnDragMarkerCompleted(object sender, DragCompletedEventArgs e)
        {
            var marker = sender as Thumb;
            marker.ReleaseMouseCapture();
            //Debug.WriteLine("Release Capture Annotation Marker");

            if (AdornedAnnotation is AnnotationBase ann && ann.IsEditable)
                ann.RaiseAnnotationDragEnded(true, true);
        }

        private void OnDragMarkerStarted(object sender, DragStartedEventArgs e)
        {
            var marker = sender as Thumb;
            marker.CaptureMouse();
            //Debug.WriteLine("Capturing Annotation Marker");

            if (AdornedAnnotation is AnnotationBase ann && ann.IsEditable)
                ann.RaiseAnnotationDragStarted(true, true);
        }

        public override void Clear()
        {
            _adornerMarkers.ForEachDo(DetachMarker);
            _adornerMarkers.Clear();
        }

        private void DetachMarker(Thumb marker)
        {
            marker.DragDelta -= OnDragMarker;

            ParentCanvas.Children.Remove(marker);
        }

        private void UpdatePositions(Point[] newPositions)
        {
            foreach (var marker in _adornerMarkers)
            {
                var start = newPositions[_adornerMarkers.IndexOf(marker)];
                start.X -= marker.Width / 2;
                start.Y -= marker.Height / 2;

                Canvas.SetLeft(marker, start.X);
                Canvas.SetTop(marker, start.Y);
            }
        }

        public override void UpdatePositions()
        {
            if (_inUpdate) return;

            try
            {
                _inUpdate = true;
#if SILVERLIGHT
                _previousPoints = _points;
#endif
                _points = AdornedAnnotation.GetBasePoints();

                if (_points != null &&
                    _points.Length != 0 && _adornerMarkers.Count == 0)
                {
                    _points.ForEachDo(AttachMarkerAt);
                }

                UpdatePositions(_points);

                _adornerMarkers.ForEach(m => m.ContextMenu = (AdornedAnnotation as AnnotationBase)?.ContextMenu);

#if SILVERLIGHT
                // Recalculate changes related to new point coordinates
                if (_previousPoints != null && _points != null && _lastMarkerIndex != -1)
                {
                    var newPoint = _points[_lastMarkerIndex];
                    var oldPoint = _previousPoints[_lastMarkerIndex];

                    _horizontalChange -= newPoint.X - oldPoint.X;
                    _verticalChange -= newPoint.Y - oldPoint.Y;
                }
#endif
            }
            finally
            {
                _inUpdate = false;
            }
        }

        private void OnDragMarker(object sender, DragDeltaEventArgs e)
        {
            var annotation = AdornedAnnotation;

            if (!annotation.IsEditable) return;

            var marker = sender as Thumb;

            int changedAtIndex = _adornerMarkers.IndexOf(marker);

            _horizontalChange = e.HorizontalChange;
            _verticalChange = e.VerticalChange;

            double offsetX = annotation.ResizeDirections == XyDirection.YDirection ? 0 : _horizontalChange;
            double offsetY = annotation.ResizeDirections == XyDirection.XDirection ? 0 : _verticalChange;

            var point = _points[changedAtIndex];
            point.X += offsetX;
            point.Y += offsetY;

            if (annotation.IsResizable)
            {
                annotation.SetBasePoint(point, changedAtIndex);
            }
            else
            {
                annotation.MoveAnnotation(offsetX, offsetY);
            }

            (annotation as AnnotationBase)?.RaiseAnnotationDragging(0, 0, true, true);
        }

        /// <summary>
        /// Gets the Adorner Markers placed by this annotation (e.g. grippers to resize, move)
        /// </summary>
        public IEnumerable<Thumb> AdornerMarkers { get { return _adornerMarkers; } }
    }
}
