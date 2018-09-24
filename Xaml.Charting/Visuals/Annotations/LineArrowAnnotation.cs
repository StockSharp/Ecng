// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LineArrowAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// The LineArrowAnnotation provides a Line with X1,Y1,X2,Y2 coordinates and an arrow head at the tip. 
    /// </summary>
    public class LineArrowAnnotation : LineAnnotationBase
    {
        /// <summary>
        /// Defines the HeadLength DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HeadLengthProperty =
            DependencyProperty.Register("HeadLength", typeof(double), typeof(LineArrowAnnotation), new PropertyMetadata(4.0, OnRenderablePropertyChanged));

        /// <summary>
        /// Defines the HeadWidth DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HeadWidthProperty =
            DependencyProperty.Register("HeadWidth", typeof(double), typeof(LineArrowAnnotation), new PropertyMetadata(8.0, OnRenderablePropertyChanged));

        private Line _line;
        private Line _ghostLine;
        private Polygon _arrowHead;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineArrowAnnotation" /> class.
        /// </summary>
        public LineArrowAnnotation()
        {
            DefaultStyleKey = typeof (LineArrowAnnotation);
        }

        /// <summary>
        /// Gets or sets the Head Length in pixels. Change this along with <see cref="HeadWidth"/> to make the line-arrow head larger or smaller
        /// </summary>
        public double HeadLength
        {
            get { return (double)GetValue(HeadLengthProperty); }
            set { SetValue(HeadLengthProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Head Length in pixels. Change this along with <see cref="HeadLength"/> to make the line-arrow head larger or smaller
        /// </summary>
        public double HeadWidth
        {
            get { return (double)GetValue(HeadWidthProperty); }
            set { SetValue(HeadWidthProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            AnnotationRoot = GetAndAssertTemplateChild<Grid>("PART_LineArrowAnnotationRoot");
            _line = GetAndAssertTemplateChild<Line>("PART_Line");
            _ghostLine = GetAndAssertTemplateChild<Line>("PART_GhostLine");
            _arrowHead = GetAndAssertTemplateChild<Polygon>("PART_ArrowHead");
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
            var canvas = GetCanvas(AnnotationCanvas);

			if(XAxis == null || YAxis == null)
				return false;

            var xCalc = XAxis.GetCurrentCoordinateCalculator();
            var yCalc = YAxis.GetCurrentCoordinateCalculator();

            var coords = GetCoordinates(canvas, xCalc, yCalc);

            var pt1 = new Point(coords.X1Coord, coords.Y1Coord);
            var pt2 = new Point(coords.X2Coord, coords.Y2Coord);

            var headPoints = GetHeadPoints(pt1, pt2, HeadLength, HeadWidth);

            return base.IsPointWithinBounds(point) ||
                PointUtil.IsPointInTriangle(point, headPoints[0], headPoints[1],
                pt2);
        }

        internal Point[] GetHeadPoints(Point pt1, Point pt2, double headLength, double headWidth)
        {
            double theta = Math.Atan2(pt1.Y - pt2.Y, pt1.X - pt2.X);
            double sint = Math.Sin(theta);
            double cost = Math.Cos(theta);

            var pt3 = new Point(
                pt2.X + (HeadWidth * cost - HeadLength * sint),
                pt2.Y + (HeadWidth * sint + HeadLength * cost));

            var pt4 = new Point(
                pt2.X + (HeadWidth * cost + HeadLength * sint),
                pt2.Y + (HeadWidth * sint - HeadLength * cost));

            return new[] { pt2, pt3, pt4 };
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategy(this);
            }

            return new CartesianAnnotationPlacementStrategy(this);
        }

        internal class CartesianAnnotationPlacementStrategy : CartesianAnnotationPlacementStrategyBase<LineArrowAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(LineArrowAnnotation annotation)
                : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                PlaceLineArrowAnnotation(coordinates, Annotation);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return CalculateBasePoints(coordinates);
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<LineArrowAnnotation>
        {
            public PolarAnnotationPlacementStrategy(LineArrowAnnotation annotation)
                : base(annotation)
            {

            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                PlaceLineArrowAnnotation(cartesianCoordinates, Annotation);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);
                
                return CalculateBasePoints(cartesianCoordinates);
            }
        }

        private static void PlaceLineArrowAnnotation(AnnotationCoordinates coordinates, LineArrowAnnotation annotation)
        {
            annotation._line.X1 = coordinates.X1Coord;
            annotation._line.X2 = coordinates.X2Coord;
            annotation._line.Y1 = coordinates.Y1Coord;
            annotation._line.Y2 = coordinates.Y2Coord;

            annotation._ghostLine.X1 = coordinates.X1Coord;
            annotation._ghostLine.X2 = coordinates.X2Coord;
            annotation._ghostLine.Y1 = coordinates.Y1Coord;
            annotation._ghostLine.Y2 = coordinates.Y2Coord;

            var pts = annotation.GetHeadPoints(new Point(coordinates.X1Coord, coordinates.Y1Coord),
                new Point(coordinates.X2Coord, coordinates.Y2Coord),
                annotation.HeadLength, annotation.HeadWidth);

            var ptCollection = new PointCollection();
            ptCollection.Add(pts[0]);
            ptCollection.Add(pts[1]);
            ptCollection.Add(pts[2]);

            annotation._arrowHead.Points = ptCollection;
        }

        private static Point[] CalculateBasePoints(AnnotationCoordinates coordinates)
        {
            var pt1 = new Point(coordinates.X1Coord, coordinates.Y1Coord);
            var pt2 = new Point(coordinates.X2Coord, coordinates.Y2Coord);

            return new[] { pt1, pt2, };
        }

        internal Line Line { get { return _line; } }

        internal Line GhostLine { get { return _ghostLine; } }

        internal Polygon ArrowHead { get { return _arrowHead; } }
    }
}
