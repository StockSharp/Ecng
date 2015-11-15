// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationDragAdorner.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Windows;
using Ecng.Xaml.Charting.ChartModifiers;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    internal class AnnotationDragAdorner: AdornerBase
    {
        private bool _isDragging;
        private Point _startPoint;

        public AnnotationDragAdorner(IAnnotation adornedElement)
            : base(adornedElement)
        {}

        public override void Initialize()
        {
        }

        public override void UpdatePositions()
        {
        }

        public override void Clear()
        {
        }

        public void InitiateDrag()
        {
            
        }

        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            base.OnModifierMouseMove(e);

            var annotation = AdornedAnnotation;

            if (!_isDragging)
            { return; }

            var mousePoint = GetPointRelativeToRoot(e.MousePoint);
            var offsetX = mousePoint.X < 0 || mousePoint.X > ParentCanvas.ActualWidth ? 0 : mousePoint.X - _startPoint.X;
            var offsetY = mousePoint.Y < 0 || mousePoint.Y > ParentCanvas.ActualHeight ? 0 : mousePoint.Y - _startPoint.Y;

            //Debug.WriteLine("Mouse: x={0}, y={1}. Offset: x={2}, y={3}", e.MousePoint.X, e.MousePoint.Y, offsetX, offsetY);

            offsetX = annotation.DragDirections == XyDirection.YDirection ? 0 : offsetX;
            offsetY = annotation.DragDirections == XyDirection.XDirection ? 0 : offsetY;

            annotation.MoveAnnotation(offsetX, offsetY);
            _startPoint = mousePoint;
            e.Handled = true;
            UpdatePositions();
        }

        public new void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);

            var annotation = AdornedAnnotation;
            if (!annotation.IsEditable) return;

            //if (annotation.IsPointWithinBounds(e.MousePoint))
            //{
                _isDragging = true;

                _startPoint = GetPointRelativeToRoot(e.MousePoint);

                annotation.CaptureMouse();

                e.Handled = true;
            //}
        }

        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            base.OnModifierMouseUp(e);

            var annotation = AdornedAnnotation;

            if(!_isDragging)
            { return; }

            _isDragging = false;

            annotation.ReleaseMouseCapture();
        }
    }
}
