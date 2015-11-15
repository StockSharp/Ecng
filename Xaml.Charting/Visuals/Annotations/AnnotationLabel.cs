// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnnotationLabel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Enumeration constants to define label placement
    /// </summary>
    public enum LabelPlacement
    {
        /// <summary>Places on the right</summary>
        Right,
        /// <summary>Places at the top right</summary>
        TopRight,
        /// <summary>Places at the bottom right</summary>
        BottomRight,
        /// <summary>Places at the bottom</summary>
        Bottom,
        /// <summary>Places on the left</summary>
        Left,
        /// <summary>Places on the top left</summary>
        TopLeft,
        /// <summary>Places on the bottom left</summary>
        BottomLeft,
        /// <summary>Places at the top</summary>
        Top,
        /// <summary>Places on the axis</summary>
        Axis,
        /// <summary>Automatic Placement (Default)</summary>
        Auto
    }

    /// <summary>
    /// Defines an AnnotationLabel which may be used in <see cref="HorizontalLineAnnotation"/> and <see cref="VerticalLineAnnotation"/> instances
    /// </summary>
    [TemplatePart(Name = "PART_InputTextArea", Type = typeof(TextBox))]
    public class AnnotationLabel: Control
    {
        /// <summary>Defines the Text DependnecyProperty</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(AnnotationLabel), new PropertyMetadata(String.Empty));
        /// <summary>Defines the LabelPlacement DependnecyProperty</summary>
        public static readonly DependencyProperty LabelPlacementProperty = DependencyProperty.Register("LabelPlacement", typeof(LabelPlacement), typeof(AnnotationLabel), new PropertyMetadata(LabelPlacement.Auto, OnLabelPlacementChanged));
        /// <summary>Defines the LabelStyle DependnecyProperty</summary>
        public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register("LabelStyle", typeof(Style), typeof(AnnotationLabel), new PropertyMetadata(null, OnLabelPlacementChanged));
        /// <summary>Defines the AxisLabel DependnecyProperty</summary>
        public static readonly DependencyProperty AxisLabelStyleProperty = DependencyProperty.Register("AxisLabelStyle", typeof(Style), typeof(AnnotationLabel), new PropertyMetadata(null, OnLabelPlacementChanged));
        /// <summary>Defines the CornerRadius DependnecyProperty</summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(AnnotationLabel), new PropertyMetadata(default(CornerRadius)));
        /// <summary>Defines the RotationAngle DependnecyProperty</summary>
        public static readonly DependencyProperty RotationAngleProperty = DependencyProperty.Register("RotationAngle", typeof(double), typeof(AnnotationLabel), new PropertyMetadata(0d));
        /// <summary>Defines the CanEditText DependnecyProperty</summary>
        public static readonly DependencyProperty CanEditTextProperty = DependencyProperty.Register("CanEditText", typeof(bool), typeof(AnnotationLabel), new PropertyMetadata(false));

        /// <summary>Defines the TextFormatting DependnecyProperty</summary>
        [Obsolete("We're sorry! AnnotationLabel.TextFormatting is obsolete. Please use a value converter or set StringFormat on a binding.")]
        public static readonly DependencyProperty TextFormattingProperty = DependencyProperty.Register("TextFormatting", typeof(string), typeof(AnnotationLabel), new PropertyMetadata(String.Empty));

        private LineAnnotationWithLabelsBase _parentAnnotation;
        private TextBox _inputTextArea;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationLabel" /> class.
        /// </summary>
        public AnnotationLabel()
        {
            DefaultStyleKey = typeof(AnnotationLabel);

#if !SILVERLIGHT
            SetCurrentValue(ContextMenuProperty, new ContextMenu { Visibility = Visibility.Collapsed });
#endif

            MouseLeftButtonDown += (s, e) =>
                             {
                                 TryFocusInputTextArea();
                                 
                                 ParentAnnotation.TrySelectAnnotation();
                             };
        }

        /// <summary>
        /// Returns <value>True</value> if <see cref="LabelPlacement"/> == <value>LabelPlacement.Axis</value>
        /// or if ParentAnnotation.ResolveAutoPlacement() == <value>LabelPlacement.Axis</value>
        /// in case when <see cref="LabelPlacement"/> == <value>LabelPlacement.Auto</value>
        /// </summary>
        internal bool IsAxisLabel
        {
            get
            {
                return LabelPlacement == LabelPlacement.Axis ||
                       (ParentAnnotation != null &&
                        (ParentAnnotation.GetLabelPlacement(this) == LabelPlacement.Axis));
            }
        }

        /// <summary>
        /// Gets or sets whether the text in the label is editable
        /// </summary>
        public bool CanEditText
        {
            get { return (bool)GetValue(CanEditTextProperty); }
            set { SetValue(CanEditTextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the angle, in degrees, of clockwise rotation
        /// </summary>
        public double RotationAngle
        {
            get { return (double)GetValue(RotationAngleProperty); }
            set { SetValue(RotationAngleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the parent <see cref="LineAnnotationWithLabelsBase"/> that this label is attached to
        /// </summary>
        public LineAnnotationWithLabelsBase ParentAnnotation
        {
            get { return _parentAnnotation; }
            set
            {
                if (_parentAnnotation != null)
                {
                    _parentAnnotation.Unselected -= OnParentAnnotationUnselected;
                }

                _parentAnnotation = value;

                if (_parentAnnotation != null)
                {
                    _parentAnnotation.Unselected += OnParentAnnotationUnselected;

                    ApplyStyle();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Text of the label
        /// </summary>
        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="LabelPlacement"/>
        /// </summary>
        public LabelPlacement LabelPlacement
        {
            get { return (LabelPlacement)GetValue(LabelPlacementProperty); }
            set { SetValue(LabelPlacementProperty, value); }
        }

        /// <summary>
        /// Gets or sets the TextFormatting to use on the label, when the Text property is bound to a Data-Value. 
        /// This works in a similar way to the <see cref="AxisBase"/> TextFormatting property
        /// </summary>
        [Obsolete("We're sorry! AnnotationLabel.TextFormatting is obsolete. Please use a value converter or set StringFormat on a binding.", true)]
        public string TextFormatting
        {
            get { throw new Exception("We're sorry! AnnotationLabel.TextFormatting is obsolete. Please use a value converter or set StringFormat on a binding.");}
            set { throw new Exception("We're sorry! AnnotationLabel.TextFormatting is obsolete. Please use a value converter or set StringFormat on a binding."); }
        }

        /// <summary>
        /// Gets or sets a <see cref="Style"/> to apply to the label
        /// </summary>
        public Style LabelStyle
        {
            get { return (Style)GetValue(LabelStyleProperty); }
            set { SetValue(LabelStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="Style"/> to apply to the Axis Label
        /// </summary>
        public Style AxisLabelStyle
        {
            get { return (Style)GetValue(AxisLabelStyleProperty); }
            set { SetValue(AxisLabelStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the CornerRadius of the Label element
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes (such as a rebuilding layout pass) call <see cref="M:System.Windows.Controls.Control.ApplyTemplate" />. In simplest terms, this means the method is called just before a UI element displays in an application. For more information, see Remarks.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if(_inputTextArea != null)
            {
                _inputTextArea.ClearValue(TextBox.TextProperty);
            }

            _inputTextArea = GetAndAssertTemplateChild<TextBox>("PART_InputTextArea");

#if !SILVERLIGHT
            var binding = new Binding("ContextMenu") { RelativeSource = RelativeSource.TemplatedParent };
            _inputTextArea.SetBinding(ContextMenuProperty, binding);
#endif
        }

        /// <summary>
        /// Gets the TemplateChild by the specified name and casts to type <typeparamref name="T" />, asserting that the result is not null
        /// </summary>
        /// <typeparam name="T">The Type of the templated part</typeparam>
        /// <param name="childName">Name of the templated part.</param>
        /// <returns>The template part instance</returns>
        /// <exception cref="System.InvalidOperationException">Unable to Apply the Control Template. Child is missing or of the wrong type</exception>
        protected T GetAndAssertTemplateChild<T>(string childName) where T : class
        {
            var templateChild = GetTemplateChild(childName) as T;

            if (templateChild == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Unable to Apply the Control Template. {0} is missing or of the wrong type", childName));
            }
            return templateChild;
        }

        private void TryFocusInputTextArea()
        {
            if (CanEditText && ParentAnnotation.CanEditText && ParentAnnotation.IsSelected)
            {
                _inputTextArea.IsEnabled = true;

                _inputTextArea.Focus();
            }
        }

        private void RemoveFocusInputTextArea()
        {
            if (_inputTextArea != null)
            {
                _inputTextArea.IsEnabled = false;
            }
        }

        private void ApplyStyle()
        {
            Style = IsAxisLabel ? AxisLabelStyle : LabelStyle;
        }

        private static void OnLabelPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotationLabel = d as AnnotationLabel;

            if (annotationLabel != null && annotationLabel.ParentAnnotation != null)
            {
                annotationLabel.ApplyStyle();

                annotationLabel.ParentAnnotation.InvalidateLabel(annotationLabel);
            }
        }

        private void OnParentAnnotationUnselected(object sender, EventArgs e)
        {
            RemoveFocusInputTextArea();
        }
    }
}
