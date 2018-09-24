// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisMarkerAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.AttachedProperties;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// The AxisMarkerAnnotation provides an axis label which is data-bound to its Y-value. Used to place a marker on the Y-Axis it can give feedback about the latest value of a series, or 
    /// important points in a series. 
    /// </summary>
    public class AxisMarkerAnnotation : AnchorPointAnnotation
    {
        /// <summary>
        /// Defines the FormattedValue DependencyProperty
        /// </summary>
        public static readonly DependencyProperty FormattedValueProperty = DependencyProperty.Register("FormattedValue", typeof(string), typeof(AxisMarkerAnnotation), new PropertyMetadata(default(string)));
        /// <summary>
        /// Defines the MarkerPointWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MarkerPointWidthProperty = DependencyProperty.Register("MarkerPointWidth", typeof(double), typeof(AxisMarkerAnnotation), new PropertyMetadata(8.0));
        /// <summary>
        /// Defines the LabelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LabelTemplateProperty = DependencyProperty.Register("LabelTemplate", typeof (DataTemplate), typeof (AxisMarkerAnnotation), new PropertyMetadata(default(DataTemplate)));
        /// <summary>
        /// Defines the PointerTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty PointerTemplateProperty = DependencyProperty.Register("PointerTemplate", typeof (DataTemplate), typeof (AxisMarkerAnnotation), new PropertyMetadata(default(DataTemplate)));
        /// <summary>
        /// Defines the AxisInfo DependencyProperty
        /// </summary>
        protected internal static readonly DependencyProperty AxisInfoProperty = DependencyProperty.Register("AxisInfo", typeof (AxisInfo), typeof (AxisMarkerAnnotation), new PropertyMetadata(default(AxisInfo)));

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisMarkerAnnotation" /> class.
        /// </summary>
        public AxisMarkerAnnotation()
        {
            DefaultStyleKey = typeof(AxisMarkerAnnotation);
            AnnotationCanvas = AnnotationCanvas.YAxis;
        }

        /// <summary>
        /// Gets or sets the Formatted Value of the Axis Marker. By default this is data-bound to Y1
        /// </summary>
        public string FormattedValue
        {
            get { return (string)GetValue(FormattedValueProperty); }
            set { SetValue(FormattedValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Marker tip width, this is the width of the point of the marker in pixels. Default value is 8. 
        /// Change this property if the marker looks squashed!
        /// </summary>
        public double MarkerPointWidth
        {
            get { return (double)GetValue(MarkerPointWidthProperty); }
            set { SetValue(MarkerPointWidthProperty, value); }
        }

        /// <summary>
        /// Gets or sets label template of <see cref="AxisMarkerAnnotation"/>
        /// </summary>
        public DataTemplate LabelTemplate
        {
            get { return (DataTemplate)GetValue(LabelTemplateProperty); }
            set { SetValue(LabelTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets pointer template of <see cref="AxisMarkerAnnotation"/>
        /// </summary>
        public DataTemplate PointerTemplate
        {
            get { return (DataTemplate)GetValue(PointerTemplateProperty); }
            set { SetValue(PointerTemplateProperty, value); }
        }

        /// <summary>
        /// Gets <see cref="IAxis"/> intance where <see cref="AxisMarkerAnnotation"/> is placed 
        /// </summary>
        public IAxis Axis
        {
            get
            {
                return AnnotationCanvas == AnnotationCanvas.YAxis ? YAxis : XAxis;
            }
        }

        /// <summary>
        /// Gets AxisInfo for current MarkerValue
        /// Used internally as DataContext for <see cref="PointerTemplate"/> and <see cref="LabelTemplate"/>
        /// </summary>
        public AxisInfo AxisInfo
        {
            get { return (AxisInfo)GetValue(AxisInfoProperty); }
            private set { SetValue(AxisInfoProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AnnotationRoot = GetAndAssertTemplateChild<FrameworkElement>("PART_AxisMarkerAnnotationRoot");
        }

        /// <summary>
        /// Virtual method to override if you wish to be notified that the <see cref="IAxis.AxisAlignment" /> has changed
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="oldAlignment"></param>
        protected override void OnAxisAlignmentChanged(IAxis axis, AxisAlignment oldAlignment)
        {
            base.OnAxisAlignmentChanged(axis, oldAlignment);

            var cursor = GetSelectedCursor();

            this.SetCurrentValue(CursorProperty, cursor);
        }

        /// <summary>
        /// Updates the coordinate calculators and refreshes the annotation position on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="xCoordinateCalculator">The XAxis <see cref="ICoordinateCalculator{T}" /></param>
        /// <param name="yCoordinateCalculator">The YAxis <see cref="ICoordinateCalculator{T}" /></param>
        public override void Update(ICoordinateCalculator<double> xCoordinateCalculator, ICoordinateCalculator<double> yCoordinateCalculator)
        {
            base.Update(xCoordinateCalculator, yCoordinateCalculator);

            var axis = Axis as AxisBase;
            if (axis != null)
            {
                AxisInfo = axis.HitTest((IComparable)GetValue(GetBaseProperty()));
            }
        }

        private DependencyProperty GetBaseProperty()
        {
            return AnnotationCanvas == AnnotationCanvas.XAxis ? X1Property : Y1Property;
        }

        /// <summary>
        /// Gets the <see cref="Cursor" /> to use for the annotation when selected
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetSelectedCursor()
        {
            return Axis != null && Axis.IsHorizontalAxis ? Cursors.SizeWE : Cursors.SizeNS;
        }

        /// <summary>
        /// Converts a Data-Value to Pixel Coordinate
        /// </summary>
        /// <param name="dataValue">The Data-Value to convert</param>
        /// <param name="canvasMeasurement">The size of the canvas in the X or Y direction</param>
        /// <param name="coordCalc">The current <see cref="ICoordinateCalculator{T}">Coordinate Calculator</see></param>
        /// <param name="direction">The X or Y direction for the transformation</param>
        /// <returns></returns>
        protected override double ToCoordinate(IComparable dataValue, double canvasMeasurement, ICoordinateCalculator<double> coordCalc, XyDirection direction)
        {
            var coord = base.ToCoordinate(dataValue, canvasMeasurement, coordCalc, direction);

            // need to remove offset which was added by coordinate calculator because marker doesn't need it
            coord -= coordCalc.CoordinatesOffset;

            return coord;
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
            var rootGrid = AnnotationRoot as Grid;

            point = ModifierSurface.TranslatePoint(point, this);
            var result = rootGrid.IsPointWithinBounds(point);

            return result;
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategy(this);
            }

            return new CartesianAnnotationPlacementStrategy(this);
        }

        internal class CartesianAnnotationPlacementStrategy : CartesianAnnotationPlacementStrategyBase<AxisMarkerAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(AxisMarkerAnnotation annotation) : base(annotation)
            {
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return CalculateBasePoints(coordinates, Annotation);
            }

            private static Point[] CalculateBasePoints(AnnotationCoordinates coordinates, AxisMarkerAnnotation annotation)
            {
                coordinates = annotation.GetAnchorAnnotationCoordinates(coordinates);

                annotation.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                var axis = annotation.Axis;

                if(axis == null)
                    return new Point[0];

                var modifierSurface = annotation.ModifierSurface;

                if (axis.IsHorizontalAxis)
                {
                    var width = annotation.DesiredSize.Width;
                    var x1Coord = coordinates.X1Coord;

                    return new[]
                    {
                        axis.TranslatePoint(new Point(x1Coord, 0), modifierSurface),
                        axis.TranslatePoint(new Point(x1Coord, axis.ActualHeight), modifierSurface),
                        axis.TranslatePoint(new Point(x1Coord + width, axis.ActualHeight), modifierSurface),
                        axis.TranslatePoint(new Point(x1Coord + width, 0), modifierSurface)
                    };
                }
                else
                {
                    var height = annotation.DesiredSize.Height;
                    var y1Coord = coordinates.Y1Coord;

                    return new[]
                    {
                        axis.TranslatePoint(new Point(0, y1Coord), modifierSurface),
                        axis.TranslatePoint(new Point(axis.ActualWidth, y1Coord), modifierSurface),
                        axis.TranslatePoint(new Point(axis.ActualWidth, y1Coord + height), modifierSurface),
                        axis.TranslatePoint(new Point(0, y1Coord + height), modifierSurface)
                    };
                }
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                PlaceMarker(Annotation.Axis, Annotation, coordinates);
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                bool inBounds;

                if(Annotation.Axis == null)
                    return false;

                if (Annotation.Axis.IsHorizontalAxis)
                {
                    var actualCoord = coordinates.X1Coord;
                    inBounds = actualCoord >= 0 && actualCoord <= canvas.ActualWidth;
                }
                else
                {
                    var actualCoord = coordinates.Y1Coord;
                    inBounds = actualCoord >= 0 && actualCoord <= canvas.ActualHeight;
                }

                return inBounds;
            }

            protected override void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, ref double horizontalOffset, ref double verticalOffset, IAnnotationCanvas canvas)
            {
                IComparable[] dataValues;

                if(Annotation.Axis == null)
                    return;

                if (Annotation.Axis.IsHorizontalAxis)
                {
                    var x1 = CalculateNewPosition(Annotation, coordinates.X1Coord, horizontalOffset, canvas.ActualWidth);

                    dataValues = FromCoordinates(x1, 0);
                }
                else
                {
                    var y1 = CalculateNewPosition(Annotation, coordinates.Y1Coord, verticalOffset, canvas.ActualHeight);

                    dataValues = FromCoordinates(0, y1);
                }

                var property = Annotation.GetBaseProperty();
                var newValue = Annotation.Axis.IsXAxis ? dataValues[0] : dataValues[1];

                Annotation.SetCurrentValue(property, newValue);
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<AxisMarkerAnnotation>
        {
            private readonly double _radius;

            public PolarAnnotationPlacementStrategy(AxisMarkerAnnotation annotation) : base(annotation)
            {
                _radius = PolarUtil.CalculateViewportRadius(TransformationStrategy.ViewportSize);
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                if(Annotation.Axis == null)
                    return;

                if (Annotation.Axis.IsXAxis)
                {
                    ClearAxisMarkerPlacement(Annotation);

                    AxisCanvas.SetCenterLeft(Annotation, coordinates.X1Coord);
                    AxisCanvas.SetBottom(Annotation, 0);
                }
                else
                {
                    coordinates.X1Coord = coordinates.Y1Coord;

                    PlaceMarker(Annotation.Axis, Annotation, coordinates);
                }
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                if(Annotation.Axis == null)
                    return false;

                if (Annotation.Axis.IsXAxis)
                {
                    return true;
                }
                else
                {
                    bool inBounds;

                    if (Annotation.Axis.IsHorizontalAxis)
                    {
                        var actualCoord = coordinates.Y1Coord;
                        inBounds = actualCoord >= 0 && actualCoord <= canvas.ActualWidth;
                    }
                    else
                    {
                        var actualCoord = coordinates.Y1Coord;
                        inBounds = actualCoord >= 0 && actualCoord <= canvas.ActualHeight;
                    }

                    return inBounds;
                }
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                if(Annotation.Axis == null)
                    return new Point[0];

                if (Annotation.Axis.IsXAxis)
                {
                    var actualCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                    return new Point[] {new Point(actualCoordinates.X1Coord, actualCoordinates.Y1Coord),};
                }
                else
                {
                    coordinates.X1Coord = coordinates.Y1Coord;
                    coordinates = Annotation.GetAnchorAnnotationCoordinates(coordinates);

                    return CalculateBasePoints(coordinates, Annotation);
                }
            }

            private static Point[] CalculateBasePoints(AnnotationCoordinates coordinates, AxisMarkerAnnotation annotation)
            {
                annotation.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                var axis = annotation.Axis;
                var modifierSurface = annotation.ModifierSurface;

                if (axis.IsHorizontalAxis)
                {
                    var xCoord = coordinates.X1Coord + annotation.DesiredSize.Width/2;
                    var yCoord = axis.AxisAlignment == AxisAlignment.Top ? axis.ActualHeight : 0;

                    return new[]
                    {
                        axis.TranslatePoint(new Point(xCoord, yCoord), modifierSurface),
                    };
                }
                else
                {
                    var xCoord = axis.AxisAlignment == AxisAlignment.Left ? axis.ActualWidth : 0;
                    var yCoord = coordinates.Y1Coord + annotation.DesiredSize.Height / 2;

                    return new[]
                    {
                        axis.TranslatePoint(new Point(xCoord, yCoord), modifierSurface),
                    };
                }
            }

            protected override Tuple<Point, Point> CalculateAnnotationOffsets(AnnotationCoordinates coordinates, double horizontalOffset, double verticalOffset)
            {
                if (Annotation.Axis.IsXAxis)
                {
                    return base.CalculateAnnotationOffsets(coordinates, horizontalOffset, verticalOffset);
                }
                else
                {
                    // need to calculate offset like for cartesian coordinates on YAxis
                    var offset = new Point(horizontalOffset, verticalOffset);

                    return new Tuple<Point, Point>(offset, offset);
                }
            }

            protected override AnnotationCoordinates GetCartesianAnnotationCoordinates(AnnotationCoordinates coordinates)
            {
                if (Annotation.Axis.IsXAxis)
                {
                    coordinates.Y1Coord = coordinates.Y2Coord = _radius;
                }

                return base.GetCartesianAnnotationCoordinates(coordinates);
            }

            public override void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizontalOffset, double verticalOffset,
                IAnnotationCanvas annotationCanvas)
            {
                var axis = Annotation.Axis;

                if (axis.IsXAxis)
                {
                    var offsets = CalculateAnnotationOffsets(coordinates, horizontalOffset, verticalOffset);
                    var xOffset = offsets.Item1.X;

                    var x1 = CalculateNewPosition(Annotation, coordinates.X1Coord, xOffset, 360);

                    var dataValues = FromCoordinates(x1, 0);

                    Annotation.SetCurrentValue(X1Property, dataValues[0]);
                }
                else
                {
                    var coord = axis.IsHorizontalAxis
                        ? CalculateNewPosition(Annotation, coordinates.Y1Coord, horizontalOffset,
                            annotationCanvas.ActualWidth)
                        : CalculateNewPosition(Annotation, coordinates.Y1Coord, verticalOffset,
                            annotationCanvas.ActualHeight);

                    var value = Annotation.FromCoordinate(coord, axis);

                    Annotation.SetCurrentValue(Y1Property, value);
                }
            }
        }

        private static void PlaceMarker(IAxis axis, AxisMarkerAnnotation axisMarker, AnnotationCoordinates coordinates)
        {
            coordinates = axisMarker.GetAnchorAnnotationCoordinates(coordinates);

            var point = new Point(coordinates.X1Coord, coordinates.Y1Coord);

            ClearAxisMarkerPlacement(axisMarker);

            if(axis == null)
                return;

            if (axis.IsHorizontalAxis)
            {
                var property = axis.AxisAlignment == AxisAlignment.Bottom
                   ? AxisCanvas.TopProperty
                   : AxisCanvas.BottomProperty;

                axisMarker.SetValue(property, 0d);

                AxisCanvas.SetLeft(axisMarker, point.X);
            }
            else
            {
                var property = axis.AxisAlignment == AxisAlignment.Right
                    ? AxisCanvas.LeftProperty
                    : AxisCanvas.RightProperty;

                axisMarker.SetValue(property, 0d);

                AxisCanvas.SetTop(axisMarker, point.Y);
            }
        }

        private static void ClearAxisMarkerPlacement(FrameworkElement axisLabel)
        {
            axisLabel.SetValue(AxisCanvas.LeftProperty, double.NaN);
            axisLabel.SetValue(AxisCanvas.RightProperty, double.NaN);
            axisLabel.SetValue(AxisCanvas.CenterLeftProperty, double.NaN);
            axisLabel.SetValue(AxisCanvas.TopProperty, double.NaN);
            axisLabel.SetValue(AxisCanvas.BottomProperty, double.NaN);
        }

        private static double CalculateNewPosition(AxisMarkerAnnotation annotation, double currentPosition, double offset, double canvasSize)
        {
            var nextPosition = currentPosition + offset;

            if (!annotation.IsCoordinateValid(nextPosition, canvasSize))
            {
                if (nextPosition < 0)
                {
                    nextPosition = 0;
                }

                if (nextPosition > canvasSize)
                {
                    nextPosition = canvasSize - 1;
                }
            }

            return nextPosition;
        }
    }
}