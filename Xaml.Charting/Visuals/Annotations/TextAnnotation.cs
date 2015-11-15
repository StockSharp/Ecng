// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TextAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines a Text annotation, which may be used to place read-only labels or read-write Textboxes on the parent <see cref="UltrachartSurface"/>
    /// </summary>
    [TemplatePart(Name = "PART_InputTextArea", Type = typeof(TextBox))]
    public class TextAnnotation : AnchorPointAnnotation
    {
        /// <summary>Defines the CornerRadius DependencyProperty</summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(TextAnnotation), new PropertyMetadata(default(CornerRadius)));
        /// <summary>Defines the Text DependencyProperty</summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(TextAnnotation), new PropertyMetadata(string.Empty));
        /// <summary>Defines the TextAlignment DependencyProperty</summary>
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register("TextAlignment", typeof(TextAlignment), typeof(TextAnnotation), new PropertyMetadata(TextAlignment.Left));
        /// <summary>Defines the TextStretch DependencyProperty</summary>
        public static readonly DependencyProperty TextStretchProperty = DependencyProperty.Register("TextStretch", typeof(Stretch), typeof(TextAnnotation), new PropertyMetadata(Stretch.None));

        private TextBox _inputTextArea;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextAnnotation" /> class.
        /// </summary>
        public TextAnnotation()
        {
            DefaultStyleKey = typeof (TextAnnotation);

#if !SILVERLIGHT
            SetCurrentValue(ContextMenuProperty, new ContextMenu {Visibility = Visibility.Collapsed});
#endif
        }

        /// <summary>
        /// Gets or sets the CornerRadius of the TextAnnotation
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="TextAlignment"/>
        /// </summary>
        public TextAlignment TextAlignment
        {
            get { return (TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Text of the TextAnnotation
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Gets or sets how Text stretches to fill its container. Applicable if the X1,Y1,X2,Y2 properties are all set, else text will be uniform
        /// </summary>
        // Not used for now
        private Stretch TextStretch
        {
            get { return (Stretch)GetValue(TextStretchProperty); }
            set { SetValue(TextStretchProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AnnotationRoot = GetAndAssertTemplateChild<Border>("PART_TextAnnotationRoot");
            _inputTextArea = GetAndAssertTemplateChild<TextBox>("PART_InputTextArea");

#if !SILVERLIGHT
            var binding = new Binding("ContextMenu") {RelativeSource = RelativeSource.TemplatedParent};
            _inputTextArea.SetBinding(ContextMenuProperty, binding);
#endif

            PerformFocusOnInputTextArea();
        }

        /// <summary>
        /// Focuses the text input area on the <see cref="TextAnnotation"/>
        /// </summary>
        protected override void FocusInputTextArea()
        {
            if (_inputTextArea != null)
            {
                _inputTextArea.IsEnabled = true;

                _inputTextArea.Focus();
            }
        }

        /// <summary>
        /// Remove focus from input text area. Applicable only for Text and label annotation
        /// </summary>
        protected override void RemoveFocusInputTextArea()
        {
            if (_inputTextArea != null)
            {
                _inputTextArea.IsEnabled = false;
            }
        }

        private AnnotationCoordinates CoerceValues()
        {
            var canvas = GetCanvas(AnnotationCanvas);

            var xCalc = XAxis.GetCurrentCoordinateCalculator();
            var yCalc = YAxis.GetCurrentCoordinateCalculator();

            var coordinates = GetCoordinates(canvas, xCalc, yCalc);

            var x2Coord = coordinates.X2Coord;
            var y2Coord = coordinates.Y2Coord;

            var x1y1Coords = new Point(coordinates.X1Coord,coordinates.Y1Coord);
            if (double.IsNaN(x2Coord) || double.IsNaN(y2Coord))
            {
                x2Coord = x1y1Coords.X + ActualWidth;
                y2Coord = x1y1Coords.Y + ActualHeight;

                var dataValues = FromCoordinates(x2Coord, y2Coord);

                this.SetCurrentValue(X2Property, dataValues[0]);
                this.SetCurrentValue(Y2Property, dataValues[1]);

                coordinates.X2Coord = x2Coord;
                coordinates.Y2Coord = y2Coord;
            }

            return coordinates;
        }

        /// <summary>
        /// Returns true if the Point is within the bounds of the current <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>
        /// true if the Point is within the bounds
        /// </returns>
        public override bool IsPointWithinBounds(Point point)
        {
            var coords = CoerceValues();

            return new Rect(new Point(coords.X1Coord, coords.Y1Coord), new Point(coords.X2Coord, coords.Y2Coord)).Contains(point);
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategy(this);
            }

            return new CartesianAnnotationPlacementStrategy(this);
        }

        internal class CartesianAnnotationPlacementStrategy:CartesianAnnotationPlacementStrategyBase<TextAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(TextAnnotation annotation) : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                coordinates = Annotation.GetAnchorAnnotationCoordinates(coordinates);

                double x1Coord = coordinates.X1Coord;
                double x2Coord = coordinates.X2Coord;
                double y1Coord = coordinates.Y1Coord;
                double y2Coord = coordinates.Y2Coord;

                if (x2Coord < x1Coord)
                    NumberUtil.Swap(ref x1Coord, ref x2Coord);

                if (y2Coord < y1Coord)
                    NumberUtil.Swap(ref y1Coord, ref y2Coord);

                var width = x2Coord - x1Coord + 1;
                var height = y2Coord - y1Coord + 1;

                if (x1Coord.IsDefined() && y1Coord.IsDefined())
                {
                    if (width.IsDefined())
                        Annotation.MinWidth = width;
                    if (height.IsDefined())
                        Annotation.MinHeight = height;

                    Canvas.SetLeft(Annotation, x1Coord);
                    Canvas.SetTop(Annotation, y1Coord);
                }
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                coordinates = Annotation.GetAnchorAnnotationCoordinates(coordinates);

                if (double.IsNaN(coordinates.X2Coord) || double.IsNaN(coordinates.Y2Coord))
                {
                    coordinates = Annotation.CoerceValues();
                }

                return new[]
                {
                    new Point(coordinates.X1Coord, coordinates.Y1Coord),
                };
            }

            protected override void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, ref double horizontalOffset, ref double verticalOffset, IAnnotationCanvas canvas)
            {
                var x1 = coordinates.X1Coord + horizontalOffset;
                var x2 = coordinates.X2Coord + horizontalOffset;
                var y1 = coordinates.Y1Coord + verticalOffset;
                var y2 = coordinates.Y2Coord + verticalOffset;

                // If any are out of bounds ... 
                if (!IsCoordinateValid(x1, canvas.ActualWidth) || !IsCoordinateValid(y1, canvas.ActualHeight) ||
                    !IsCoordinateValid(x2, canvas.ActualWidth) || !IsCoordinateValid(y2, canvas.ActualHeight))
                {
                    x1 = double.IsNaN(x1) ? 0 : x1;
                    x2 = double.IsNaN(x2) ? x1 : x2;
                    y1 = double.IsNaN(y1) ? 0 : y1;
                    y2 = double.IsNaN(y2) ? y1 : y2;

                    // Clip to bounds
                    if (Math.Max(x1, x2) < 0) horizontalOffset -= Math.Max(x1, x2);
                    if (Math.Min(x1, x2) > canvas.ActualWidth)
                        horizontalOffset -= Math.Min(x1, x2) - (canvas.ActualWidth - 1);

                    if (Math.Max(y1, y2) < 0) verticalOffset -= Math.Max(y1, y2);
                    if (Math.Min(y1, y2) > canvas.ActualHeight)
                        verticalOffset -= Math.Min(y1, y2) - (canvas.ActualHeight - 1);
                }

                // Reassign
                coordinates.X1Coord = coordinates.X1Coord + horizontalOffset;
                coordinates.X2Coord = coordinates.X2Coord + horizontalOffset;
                coordinates.Y1Coord = coordinates.Y1Coord + verticalOffset;
                coordinates.Y2Coord = coordinates.Y2Coord + verticalOffset;

                var dataValues = FromCoordinates(coordinates.X1Coord, coordinates.Y1Coord);
                Annotation.SetCurrentValue(X1Property, dataValues[0]);
                Annotation.SetCurrentValue(Y1Property, dataValues[1]);

                dataValues = FromCoordinates(coordinates.X2Coord, coordinates.Y2Coord);
                Annotation.SetCurrentValue(X2Property, dataValues[0]);
                Annotation.SetCurrentValue(Y2Property, dataValues[1]);
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<TextAnnotation>
        {
            public PolarAnnotationPlacementStrategy(TextAnnotation annotation)
                : base(annotation)
            {

            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                Canvas.SetLeft(Annotation, cartesianCoordinates.X1Coord - Annotation.HorizontalOffset);
                Canvas.SetTop(Annotation, cartesianCoordinates.Y1Coord - Annotation.VerticalOffset);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                return new Point[]
                {
                    new Point(cartesianCoordinates.X1Coord - Annotation.HorizontalOffset, cartesianCoordinates.Y1Coord - Annotation.VerticalOffset),
                };
            }

            protected override bool IsInBoundsInternal(AnnotationCoordinates coordinates, Size canvasSize)
            {
                bool outOfBounds = coordinates.X1Coord < 0 ||
                                   coordinates.X1Coord > canvasSize.Width ||
                                   coordinates.Y1Coord < 0 ||
                                   coordinates.Y1Coord > canvasSize.Height;

                return !outOfBounds;
            }

            protected override void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, Point x1y1Offset, Point x2y2Offset, Size canvasSize)
            {
                var x1 = coordinates.X1Coord + x1y1Offset.X;
                var x2 = coordinates.X2Coord + x2y2Offset.X;
                var y1 = coordinates.Y1Coord + x1y1Offset.Y;
                var y2 = coordinates.Y2Coord + x2y2Offset.Y;

                // If any are out of bounds ... 
                if (!IsCoordinateValid(x1, canvasSize.Width) || !IsCoordinateValid(y1, canvasSize.Height) ||
                    !IsCoordinateValid(x2, canvasSize.Width) || !IsCoordinateValid(y2, canvasSize.Height))
                {
                    x1 = double.IsNaN(x1) ? 0 : x1;
                    x2 = double.IsNaN(x2) ? x1 : x2;
                    y1 = double.IsNaN(y1) ? 0 : y1;
                    y2 = double.IsNaN(y2) ? y1 : y2;

                    // Clip to bounds
                    if (Math.Max(x1, x2) < 0)
                    {
                        var horizOffset = Math.Max(x1, x2);
                        x1y1Offset.X -= horizOffset;
                        x2y2Offset.X -= horizOffset;
                    }
                    if (Math.Min(x1, x2) > canvasSize.Width)
                    {
                        var horizOffset = Math.Min(x1, x2) - (canvasSize.Width - 1);
                        x1y1Offset.X -= horizOffset;
                        x2y2Offset.X -= horizOffset;
                    }

                    if (Math.Max(y1, y2) < 0)
                    {
                        var vertOffset = Math.Max(y1, y2);
                        x1y1Offset.Y -= vertOffset;
                        x2y2Offset.Y -= vertOffset;
                    }
                    if (Math.Min(y1, y2) > canvasSize.Height)
                    {
                        var vertOffset = Math.Min(y1, y2) - (canvasSize.Height - 1);
                        x1y1Offset.Y -= vertOffset;
                        x2y2Offset.Y -= vertOffset;
                    }
                }

                // Reassign
                coordinates.X1Coord = coordinates.X1Coord + x1y1Offset.X;
                coordinates.X2Coord = coordinates.X2Coord + x2y2Offset.X;
                coordinates.Y1Coord = coordinates.Y1Coord + x1y1Offset.Y;
                coordinates.Y2Coord = coordinates.Y2Coord + x2y2Offset.Y;

                var dataValues = FromCoordinates(coordinates.X1Coord, coordinates.Y1Coord);
                Annotation.SetCurrentValue(X1Property, dataValues[0]);
                Annotation.SetCurrentValue(Y1Property, dataValues[1]);

                dataValues = FromCoordinates(coordinates.X2Coord, coordinates.Y2Coord);
                Annotation.SetCurrentValue(X2Property, dataValues[0]);
                Annotation.SetCurrentValue(Y2Property, dataValues[1]);
            }
        }
    }
}