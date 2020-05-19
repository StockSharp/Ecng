// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HorizontalLineAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Markup;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// A class for <see cref="HorizontalLineAnnotation"/>
    /// </summary>
    [ContentProperty("AnnotationLabels"),
    TemplatePart(Name = "PART_LineAnnotationRoot", Type = typeof(Grid))]
    public class HorizontalLineAnnotation : LineAnnotationWithLabelsBase
    {
        /// <summary>
        /// Defines the HorizontalAlignment Property
        /// </summary>
        public new static readonly DependencyProperty HorizontalAlignmentProperty =
            DependencyProperty.Register("HorizontalAlignment", typeof (HorizontalAlignment),
                                        typeof (HorizontalLineAnnotation),
                                        new PropertyMetadata(HorizontalAlignment.Left, OnHorizontalAlignmentChanged));

        /// <summary>
        /// Defines the YDragStep Property
        /// </summary>
        public static readonly DependencyProperty YDragStepProperty = DependencyProperty.Register("YDragStep", typeof(double), typeof(HorizontalLineAnnotation), new PropertyMetadata(0d));

        /// <summary>
        /// Creates new instance of <see cref="HorizontalLineAnnotation"/>
        /// </summary>
        public HorizontalLineAnnotation()
        {
            DefaultStyleKey = typeof (HorizontalLineAnnotation);
        }

        /// <summary>
        /// Gets or sets docking of <see cref="HorizontalLineAnnotation"/>
        /// </summary>
        public new HorizontalAlignment HorizontalAlignment
        {
            get { return (HorizontalAlignment) GetValue(HorizontalAlignmentProperty); }
            set { SetValue(HorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value which determines a drag step for this <see cref="HorizontalLineAnnotation"/>
        /// </summary>
        public double YDragStep
        {
            get { return (double)GetValue(YDragStepProperty); }
            set { SetValue(YDragStepProperty, value); }
        }

        /// <summary>
        /// Returns axis, which current annotation shows data value for
        /// </summary>
        /// <returns></returns>
        public override IAxis GetUsedAxis()
        {
            IAxis usedAxis = null;

            if (XAxis != null)
            {
                usedAxis = XAxis.IsHorizontalAxis ? YAxis : XAxis;
            }
            else
            {
                if (YAxis != null)
                {
                    usedAxis = YAxis.IsHorizontalAxis ? XAxis : YAxis;
                }
            }

            return usedAxis;
        }

        internal override LabelPlacement ResolveAutoPlacement()
        {
            var placement = LabelPlacement.Top;

            if (HorizontalAlignment == HorizontalAlignment.Right) placement = LabelPlacement.TopRight;
            if (HorizontalAlignment == HorizontalAlignment.Left) placement = LabelPlacement.TopLeft;
            if (HorizontalAlignment == HorizontalAlignment.Center) placement = LabelPlacement.Top;
            if (HorizontalAlignment == HorizontalAlignment.Stretch) placement = LabelPlacement.Axis;

            return placement;
        }

        protected override (double fixedHOffset, double fixedVOffset) MoveAnnotationTo(AnnotationCoordinates coordinates, double horizOffset, double vertOffset)
        {
            var axis = GetUsedAxis();
            var canvas = GetCanvas(AnnotationCanvas);

            // Compute new coordinates in pixels
            var y1 = coordinates.Y1Coord + vertOffset;

            // If any are out of bounds ... 
            if (!IsCoordinateValid(y1, canvas.ActualHeight))
            {
                // Clip to bounds
                if (y1 < 0) vertOffset -= y1 - 1;
                if (y1 > canvas.ActualHeight) vertOffset -= y1 - (canvas.ActualHeight - 1);

                // Reassign
                y1 = coordinates.Y1Coord + vertOffset;
            }

            if (YDragStep > 0 && !axis.IsHorizontalAxis && !axis.IsXAxis)
            {
                var dragStartCoord = FromCoordinate(coordinates.Y1Coord, axis);
                var newValue = FromCoordinate(y1, axis);

                var diff = Math.Abs(dragStartCoord.ToDouble() - newValue.ToDouble());
                var times = (int) (diff/YDragStep);

                var sign = !axis.FlipCoordinates ? -Math.Sign(vertOffset) : Math.Sign(vertOffset);

                var expectedValue = dragStartCoord.ToDouble() + sign*times*YDragStep;

                y1 = ToCoordinate(expectedValue, axis);
                vertOffset = y1 - coordinates.Y1Coord;
            }

            if (IsCoordinateValid(y1, canvas.ActualHeight))
            {
                var point = new Point {X = coordinates.X1Coord, Y = y1};

                base.SetBasePoint(point, 0, XAxis, YAxis);
            }

            return (horizOffset, vertOffset);
        }

        /// <summary>
        /// Used internally to derive the X1Property, Y1Property, X1Property, Y2Property pair for the given index around the annotation..
        /// e.g. index 0 returns X1,Y1
        /// index 1 returns X2,Y1
        /// index 2 returns X2,Y2
        /// index 3 returns X1,Y2
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        protected override void GetPropertiesFromIndex(int index, out DependencyProperty X, out DependencyProperty Y)
        {
            X = X1Property;
            Y = Y1Property;

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    X = X2Property;
                    break;
                case HorizontalAlignment.Right:
                    X = X1Property;
                    break;
                case HorizontalAlignment.Center:
                    X = index == 0 ? X1Property : X2Property;
                    break;
            }
        }

        /// <summary>
        /// Called internally to marshal pixel points to X1,Y1,X2,Y2 values.
        /// Taking a pixel point (<paramref name="newPoint" />) and base point <paramref name="index" />, sets the X,Y data-values.
        /// </summary>
        /// <param name="newPoint">The pixel point</param>
        /// <param name="index">The base point index, where 0, 1, 2, 3 refer to the four corners of an Annotation</param>
        /// <param name="xAxis">The current X-Axis</param>
        /// <param name="yAxis">The current Y-Axis</param>
        protected override void SetBasePoint(Point newPoint, int index, IAxis xAxis, IAxis yAxis)
        {
            var canvas = GetCanvas(AnnotationCanvas);

            var dataValues = FromCoordinates(newPoint);
            var newX = dataValues[0];
            var newY = dataValues[1];

            DependencyProperty X, Y;
            GetPropertiesFromIndex(index, out X, out Y);

            var isVerticalChart = !XAxis.IsHorizontalAxis;

            //Performs horizontal drag depending on chart orientation,
            //e.g. if X is horizontal axis, change X coord,
            //     if Y is horizontal axis, change Y coord
            if (IsCoordinateValid(newPoint.X, canvas.ActualWidth))
            {
                if (isVerticalChart)
                {
                    this.SetCurrentValue(Y, newY);
                }
                else
                {
                    this.SetCurrentValue(X, newX);
                }
            }
        }

        private static void OnHorizontalAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as HorizontalLineAnnotation;

            if (annotation != null)
            {
                annotation.Refresh();
            }
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategy(this);
            }

            return new CartesianAnnotationPlacementStrategy(this);
        }

        internal class CartesianAnnotationPlacementStrategy:CartesianAnnotationPlacementStrategyBase<HorizontalLineAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(HorizontalLineAnnotation annotation) : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                var canvas = Annotation.GetCanvas(Annotation.AnnotationCanvas);
                
                double x = 0, y = coordinates.Y1Coord;

                if (y.IsRealNumber() && canvas != null)
                {
                    var width = canvas.ActualWidth;

                    switch (Annotation.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Center:
                            double x1 = Math.Min(coordinates.X1Coord, coordinates.X2Coord);
                            double x2 = Math.Max(coordinates.X1Coord, coordinates.X2Coord);
                            x = x1;
                            width = x2 - x1;
                            break;
                        case HorizontalAlignment.Left:
                            width = coordinates.X1Coord.IsDefined() ? coordinates.X1Coord : coordinates.X2Coord;
                            break;
                        case HorizontalAlignment.Right:
                            x = coordinates.X1Coord.IsDefined() ? coordinates.X1Coord : coordinates.X2Coord;
                            width -= x;
                            break;
                        case HorizontalAlignment.Stretch:
                            break;
                    }

                    PlaceAnnotation(x, y, Math.Max(width, 0), coordinates.YOffset);
                }
            }

            private void PlaceAnnotation(double x, double y, double width, double axisOffset)
            {
                var root = (Annotation.AnnotationRoot as Grid);

                Annotation.Width = width;

                var middleRowOffset = root.RowDefinitions[1].ActualHeight / 2d;
                var posOffset = root.RowDefinitions[0].ActualHeight + middleRowOffset;

                var yPos = y - (posOffset.IsRealNumber() ? posOffset : 0d);

                Annotation.SetValue(Canvas.LeftProperty, x);
                Annotation.SetValue(Canvas.TopProperty, yPos);

                Annotation.TryPlaceAxisLabels(new Point(x, y - axisOffset));
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                Point[] result = null;

                switch (Annotation.HorizontalAlignment)
                {
                    case HorizontalAlignment.Left:
                        result = new[] { new Point(coordinates.X2Coord, coordinates.Y1Coord) };
                        break;
                    case HorizontalAlignment.Right:
                        result = new[] { new Point(coordinates.X1Coord, coordinates.Y1Coord) };
                        break;
                    case HorizontalAlignment.Center:
                        result = new[]
                                 {
                                     new Point(coordinates.X1Coord, coordinates.Y1Coord),
                                     new Point(coordinates.X2Coord, coordinates.Y1Coord)
                                 };
                        break;
                }

                return result;
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                bool outOfBounds = false;

                if (coordinates.Y1Coord < 0 || coordinates.Y1Coord > canvas.ActualHeight)
                {
                    outOfBounds = true;
                }
                else
                {
                    switch (Annotation.HorizontalAlignment)
                    {
                        case HorizontalAlignment.Center:
                            outOfBounds = (coordinates.X1Coord < 0 && coordinates.X2Coord < 0) ||
                                          (coordinates.X1Coord > canvas.ActualWidth && coordinates.X2Coord > canvas.ActualWidth);
                            break;
                        case HorizontalAlignment.Left:
                            outOfBounds = coordinates.X2Coord < 0;
                            break;
                        case HorizontalAlignment.Right:
                            outOfBounds = coordinates.X1Coord > canvas.ActualWidth;
                            break;
                    }
                }

                return !outOfBounds;
            }
        }


        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<HorizontalLineAnnotation>
        {
            public PolarAnnotationPlacementStrategy(HorizontalLineAnnotation annotation)
                : base(annotation)
            {
                throw new InvalidOperationException(String.Format("Unable to place {0} on polar chart.", annotation.GetType().Name));
            }
        }
    }
}