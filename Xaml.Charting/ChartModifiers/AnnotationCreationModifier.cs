// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationCreationModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// A custom <see cref="ChartModifierBase"/> to be used in conjunction with the <see cref="AnnotationCollection"/>. The <see cref="AnnotationCreationModifier"/> 
    /// allows creation of annotations on mouse-click and drag. See the example of use CreateAnnotationsDynamically in the examples-suite
    /// </summary>
    public class AnnotationCreationModifier: ChartModifierBase
    {
        /// <summary>Defines the YAxisId DependencyProperty</summary>
        public static readonly DependencyProperty YAxisIdProperty =
            DependencyProperty.Register("YAxisId", typeof(string), typeof(AnnotationCreationModifier), new PropertyMetadata(AxisBase.DefaultAxisId));

        /// <summary>Defines the XAxisId DependencyProperty</summary>
        public static readonly DependencyProperty XAxisIdProperty =
            DependencyProperty.Register("XAxisId", typeof(string), typeof(AnnotationCreationModifier), new PropertyMetadata(AxisBase.DefaultAxisId));

        private Point _draggingStartPoint;

        private AnnotationBase _newAnnotation;

        private Type _newAnnotationType;
        private Style _newAnnotationStyle;

        /// <summary>
        /// Event raised when an annotation is created
        /// </summary>
        public event EventHandler AnnotationCreated;

        /// <summary>
        /// Gets or sets the ID of the Y-Axis which this Annotation is measured against
        /// </summary>
        public string YAxisId
        {
            get { return (string)GetValue(YAxisIdProperty); }
            set { SetValue(YAxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ID of the X-Axis which this Annotation is measured against
        /// </summary>
        public string XAxisId
        {
            get { return (string)GetValue(XAxisIdProperty); }
            set { SetValue(XAxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the type of the annotation to create
        /// </summary>
        /// <value>
        /// The type of the annotation.
        /// </value>
        /// <exception cref="System.ArgumentOutOfRangeException">value</exception>
        public Type AnnotationType
        {
            get { return _newAnnotationType; }
            set
            {
                if (value != null && !typeof (IAnnotation).IsAssignableFrom(value))
                {
                    throw new ArgumentOutOfRangeException("value",
                                                          String.Format(
                                                              "Type {0} does not implement IAnnotation interface.",
                                                              value));
                }

                _newAnnotationType = value;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="Style"/> to apply to the annotation being created
        /// </summary>
        public Style AnnotationStyle
        {
            get { return _newAnnotationStyle; }
            set { _newAnnotationStyle = value; }
        }


        /// <summary>
        /// Gets the newly created <see cref="IAnnotation"/>
        /// </summary>
        public IAnnotation Annotation
        {
            get { return _newAnnotation; }
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase" /> instance
        /// </summary>
        protected override void OnIsEnabledChanged()
        {
            base.OnIsEnabledChanged();

            _newAnnotation = null;

            if (IsEnabled && ParentSurface != null)
            {
                ParentSurface.Annotations.ForEachDo(annotation =>
                {
                    annotation.IsSelected = false;
                    annotation.IsEditable = false; 
                });
            }
        }

        /// <summary>
        /// Called when [annotation created].
        /// </summary>
        protected void OnAnnotationCreated()
        {
            var handler = AnnotationCreated;

            if(handler!= null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="mouseEventArgs">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseMove(ModifierMouseArgs mouseEventArgs)
        {
            if (_newAnnotationType == null || _newAnnotation == null || !_newAnnotation.IsAttached || _newAnnotation.IsSelected) return;

            var translatedPoint = GetPointRelativeTo(mouseEventArgs.MousePoint, ModifierSurface);

            _newAnnotation.UpdatePosition(_draggingStartPoint, translatedPoint);
        }

        bool IsSinglePointAnnotation(Type annType)
        {
            return typeof(IAnchorPointAnnotation).IsAssignableFrom(annType) || annType.IsSubclassOf(typeof(LineAnnotationWithLabelsBase));
        }

        public override void OnModifierMouseDown(ModifierMouseArgs mouseButtonEventArgs)
        {
            base.OnModifierMouseDown(mouseButtonEventArgs);

            if (_newAnnotationType == null ||
                !MatchesExecuteOn(mouseButtonEventArgs.MouseButtons, ExecuteOn) ||
                !mouseButtonEventArgs.IsMaster)
                return;

            if (_newAnnotation != null && !_newAnnotation.IsSelected)
                return;

            mouseButtonEventArgs.Handled = true;

            if(_newAnnotation != null && _newAnnotation.IsAttached)
                _newAnnotation.IsSelected = false;

            _draggingStartPoint = GetPointRelativeTo(mouseButtonEventArgs.MousePoint, ModifierSurface);

            if(!IsSinglePointAnnotation(_newAnnotationType))
            {
                _newAnnotation = CreateAnnotation(_newAnnotationType, _newAnnotationStyle);
                _newAnnotation.UpdatePosition(_draggingStartPoint, _draggingStartPoint);
            }
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="mouseButtonEventArgs">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseUp(ModifierMouseArgs mouseButtonEventArgs)
        {
            if (_newAnnotationType == null ||
                !MatchesExecuteOn(mouseButtonEventArgs.MouseButtons, ExecuteOn) ||
                !mouseButtonEventArgs.IsMaster)
                return;

            if (IsSinglePointAnnotation(_newAnnotationType) && _newAnnotation == null)
            {
                _newAnnotation = CreateAnnotation(_newAnnotationType, _newAnnotationStyle);
                var point = GetPointRelativeTo(mouseButtonEventArgs.MousePoint, ModifierSurface);
                _newAnnotation.UpdatePosition(point, point);
            }

            if (_newAnnotation == null) return;

            var ann = _newAnnotation;
            _newAnnotation.IsSelected = true;
            OnAnnotationCreated();
            ann.UpdateAdorners();
        }

        /// <summary>
        /// Creates an annotation of the specified Type and applies the style to it
        /// </summary>
        /// <param name="annotationType">The Type of annotation to create</param>
        /// <param name="annotationStyle">The style to apply to the annotation</param>
        /// <returns>The annotation instance</returns>
        protected virtual AnnotationBase CreateAnnotation(Type annotationType, Style annotationStyle)
        {
            var annotation = (AnnotationBase)Activator.CreateInstance(annotationType);

            annotation.YAxisId = YAxisId;
            annotation.XAxisId = XAxisId;

            if (annotationStyle != null && annotationStyle.TargetType == annotationType)
            {
                var newStyle = new Style(annotationType) {BasedOn = annotationStyle};
                annotation.Style = newStyle;
            }

            ParentSurface.Annotations.Add(annotation);

            return annotation;
        }
    }
}