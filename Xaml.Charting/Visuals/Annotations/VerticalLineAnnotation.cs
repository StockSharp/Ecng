// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
//
// VerticalLineAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart.
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
using System.Windows.Input;
using System.Windows.Markup;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// A class for <see cref="VerticalLineAnnotation"/>
    /// </summary>
    [ContentProperty("AnnotationLabels"),
    TemplatePart(Name = "PART_LineAnnotationRoot", Type = typeof(Grid))]
    public class VerticalLineAnnotation: LineAnnotationWithLabelsBase
    {
        /// <summary>
        /// Defines the VerticalAlignment Property
        /// </summary>
        public new static readonly DependencyProperty VerticalAlignmentProperty =
            DependencyProperty.Register("VerticalAlignment", typeof(VerticalAlignment),
                                        typeof(VerticalLineAnnotation),
                                        new PropertyMetadata(VerticalAlignment.Stretch, OnVerticalAlignmentChanged));

        /// <summary>
        /// Defines the LabelDirection Property
        /// </summary>
        public static readonly DependencyProperty LabelsOrientationProperty = DependencyProperty.Register("LabelsOrientation", typeof(Orientation), typeof(VerticalLineAnnotation), new PropertyMetadata(Orientation.Vertical, OnLabelsOrientationChanged));

        /// <summary>
        /// Creates new instance of <see cref="VerticalLineAnnotation"/>
        /// </summary>
        public VerticalLineAnnotation()
        {
            DefaultStyleKey = typeof(VerticalLineAnnotation);
        }

        /// <summary>
        /// Gets or sets value, indicating whether labels will be placed vertically or horizontally
        /// </summary>
        public Orientation LabelsOrientation
        {
            get { return (Orientation) GetValue(LabelsOrientationProperty); }
            set { SetValue(LabelsOrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets docking of <see cref="VerticalLineAnnotation"/>
        /// </summary>
        public new VerticalAlignment VerticalAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalAlignmentProperty); }
            set { SetValue(VerticalAlignmentProperty, value); }
        }

        private void ApplyOrientation(AnnotationLabel label)
        {
            if (LabelsOrientation == Orientation.Horizontal)
            {
                label.RotationAngle = 0;
                return;
            }

            var placement = label.ParentAnnotation.GetLabelPlacement(label);

            if (placement != LabelPlacement.Axis && placement != LabelPlacement.Bottom && placement != LabelPlacement.Top)
            {
                if (placement.IsRight())
                {
                    label.RotationAngle = 90;
                }
                else if (placement.IsLeft() || placement == LabelPlacement.Center)
                {
                    label.RotationAngle = -90;
                }
            }
            else
            {
                label.RotationAngle = 0;
            }
        }

        /// <summary>
        /// Called internally to attach an <see cref="AnnotationLabel" /> to the current instance
        /// </summary>
        /// <param name="label">The AnnotationLabel to attach</param>
        protected override void Attach(AnnotationLabel label)
        {
            base.Attach(label);

            if (!IsHidden)
            {
                ApplyOrientation(label);
            }
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
                usedAxis = XAxis.IsHorizontalAxis ? XAxis : YAxis;
            }
            else
            {
                if (YAxis != null)
                {
                    usedAxis = YAxis.IsHorizontalAxis ? YAxis : XAxis;
                }
            }

            return usedAxis;
        }

        /// <summary>
        /// Positions the <see cref="AnnotationLabel" /> using the value of the <see cref="LabelPlacement" /> enum
        /// </summary>
        /// <param name="label">The label to place</param>
        /// <param name="placement">Placement arguments</param>
        protected override void ApplyPlacement(AnnotationLabel label, LabelPlacement placement)
        {
            var isTop = placement.IsTop();
            var isBottom = placement.IsBottom();
            var isLeft = placement.IsLeft();
            var isRight = placement.IsRight();

            if (isRight || isLeft)
            {
                label.SetValue(Grid.RowProperty, 1);
                label.SetValue(Grid.ColumnProperty, isLeft ? 0 : 2);

                label.VerticalAlignment = VerticalAlignment.Center;
                label.HorizontalAlignment = isLeft ? HorizontalAlignment.Right : HorizontalAlignment.Left;

                if (isBottom)
                {
                    label.VerticalAlignment = VerticalAlignment.Bottom;
                }

                if (isTop)
                {
                    label.VerticalAlignment = VerticalAlignment.Top;
                }
            }
            else
            {
                label.SetValue(Grid.RowProperty, isBottom ? 2 : isTop ? 0 : 1);

                label.SetValue(Grid.ColumnProperty, 1);

                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        internal override LabelPlacement ResolveAutoPlacement()
        {
            var placement = LabelPlacement.Axis;

            if (VerticalAlignment == VerticalAlignment.Top)
                placement = LabelPlacement.Bottom;
            if (VerticalAlignment == VerticalAlignment.Center)
                placement = LabelPlacement.Right;
            if (VerticalAlignment == VerticalAlignment.Stretch)
                placement = LabelPlacement.Axis;

            return placement;
        }

        /// <summary>
        /// Gets the <see cref="Cursor" /> to use for the annotation when selected
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetSelectedCursor()
        {
            return Cursors.SizeWE;
        }

        protected override (double fixedHOffset, double fixedVOffset) MoveAnnotationTo(AnnotationCoordinates coordinates, double horizOffset, double vertOffset)
        {
            var canvas = GetCanvas(AnnotationCanvas);

            // Compute new coordinates in pixels
            var x1 = coordinates.X1Coord + horizOffset;

            // If any are out of bounds ...
            if (!IsCoordinateValid(x1, canvas.ActualWidth))
            {
                // Clip to bounds
                if (x1 < 0) horizOffset -= x1 - 1;
                if (x1 > canvas.ActualWidth) horizOffset -= x1 - (canvas.ActualWidth - 1);

                // Reassign
                x1 = coordinates.X1Coord + horizOffset;
            }

            var point = new Point { X = x1, Y = coordinates.Y1Coord };

            base.SetBasePoint(point, 0, XAxis, YAxis);

            return (horizOffset, vertOffset);
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

            var dataValues = FromCoordinates(0, newPoint.Y);
            var newX = dataValues[0];
            var newY = dataValues[1];

            DependencyProperty X, Y;
            GetPropertiesFromIndex(index, out X, out Y);

            var isVerticalChart = !XAxis.IsHorizontalAxis;

            // If y aren't out of bounds ...
            if (IsCoordinateValid(newPoint.Y, canvas.ActualHeight))
            {
                if (isVerticalChart)
                {
                    this.SetCurrentValue(X, newX);
                }
                else
                {
                    this.SetCurrentValue(Y, newY);
                }
            }
        }

        /// <summary>
        /// Used internally to derive the X1Property, Y1Property, X1Property, Y2Property pair for the given index around the annotation..
        ///
        /// e.g. index 0 returns X1,Y1
        /// index 1 returns X2,Y1
        /// index 2 returns X2,Y2
        /// index 3 returns X1,Y2
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="x">The X coordinate dependency property, either X1Property or X2Property</param>
        /// <param name="y">The Y coordinate dependency property, either Y1Property or Y2Property</param>
        protected override void GetPropertiesFromIndex(int index, out DependencyProperty x, out DependencyProperty y)
        {
            x = X1Property;
            y = Y1Property;

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    y = Y2Property;
                    break;
                case VerticalAlignment.Bottom:
                    y = Y1Property;
                    break;
                case VerticalAlignment.Center:
                    y = index == 0 ? Y1Property : Y2Property;
                    break;
            }
        }

        private static void OnVerticalAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as VerticalLineAnnotation;

            if (annotation != null)
            {
                annotation.Refresh();
            }
        }

        private static void OnLabelsOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = d as VerticalLineAnnotation;

            if (annotation != null)
            {
                foreach (var label in annotation.AnnotationLabels)
                {
                    annotation.ApplyOrientation(label);
                }

                annotation.MeasureRefresh();
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

        internal class CartesianAnnotationPlacementStrategy:CartesianAnnotationPlacementStrategyBase<VerticalLineAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(VerticalLineAnnotation annotation) : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                var canvas = Annotation.GetCanvas(Annotation.AnnotationCanvas);

                double y = 0, x = coordinates.X1Coord;

                if (x.IsRealNumber() && canvas != null)
                {
                    var height = canvas.ActualHeight;

                    switch (Annotation.VerticalAlignment)
                    {
                        case VerticalAlignment.Center:
                            double y1 = Math.Min(coordinates.Y1Coord, coordinates.Y2Coord);
                            double y2 = Math.Max(coordinates.Y1Coord, coordinates.Y2Coord);
                            y = y1;
                            height = y2 - y1;
                            break;
                        case VerticalAlignment.Top:
                            height = coordinates.Y1Coord.IsDefined() ? coordinates.Y1Coord : coordinates.Y2Coord;
                            break;
                        case VerticalAlignment.Bottom:
                            y = coordinates.Y1Coord.IsDefined() ? coordinates.Y1Coord : coordinates.Y2Coord;
                            height -= y;
                            break;
                        case VerticalAlignment.Stretch:
                            break;
                    }

                    PlaceAnnotation(x, y, Math.Max(height, 0), coordinates.XOffset);
                }
            }

            private void PlaceAnnotation(double x, double y, double height, double axisOffset)
            {
                var root = (Annotation.AnnotationRoot as Grid);

                Annotation.Height = height;

                var middleColumnOffset = root.ColumnDefinitions[1].ActualWidth / 2;
                middleColumnOffset = (int)middleColumnOffset;

                var posOffset = root.ColumnDefinitions[0].ActualWidth + middleColumnOffset;
                var xPos = x - (posOffset.IsRealNumber() ? posOffset : 0);

                Annotation.SetValue(Canvas.LeftProperty, xPos);
                Annotation.SetValue(Canvas.TopProperty, y);

                Annotation.TryPlaceAxisLabels(new Point(x - axisOffset, y));
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                Point[] result = null;

                switch (Annotation.VerticalAlignment)
                {
                    case VerticalAlignment.Top:
                        result = new[] { new Point(coordinates.X1Coord, coordinates.Y2Coord) };
                        break;
                    case VerticalAlignment.Bottom:
                        result = new[] { new Point(coordinates.X1Coord, coordinates.Y1Coord) };
                        break;
                    case VerticalAlignment.Center:
                        result = new[]
                                 {
                                     new Point(coordinates.X1Coord, coordinates.Y1Coord),
                                     new Point(coordinates.X1Coord, coordinates.Y2Coord)
                                 };
                        break;
                }

                return result;
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                bool outOfBounds = false;

                if (coordinates.X1Coord < 0 || coordinates.X1Coord > canvas.ActualWidth)
                {
                    outOfBounds = true;
                }
                else
                {
                    switch (Annotation.VerticalAlignment)
                    {
                        case VerticalAlignment.Center:
                            outOfBounds = (coordinates.Y1Coord < 0 && coordinates.Y2Coord < 0) ||
                                          (coordinates.Y1Coord > canvas.ActualHeight && coordinates.Y2Coord > canvas.ActualHeight);
                            break;
                        case VerticalAlignment.Top:
                            outOfBounds = coordinates.Y2Coord < 0;
                            break;
                        case VerticalAlignment.Bottom:
                            outOfBounds = coordinates.Y1Coord > canvas.ActualHeight;
                            break;
                    }
                }

                return !outOfBounds;
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<VerticalLineAnnotation>
        {
            public PolarAnnotationPlacementStrategy(VerticalLineAnnotation annotation)
                : base(annotation)
            {
                throw new InvalidOperationException(string.Format("Unable to place {0} on polar chart.", annotation.GetType().Name));
            }
        }
    }
}
