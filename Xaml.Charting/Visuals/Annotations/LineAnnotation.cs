// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LineAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.StrategyManager;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines a read-only or editable Line annotation, which may be placed on the chart at specific X1,Y1,X2,Y2 coordinates
    /// </summary>
    public class LineAnnotation : LineAnnotationBase
    {
        private Line _line;
        private Line _ghostLine;

        /// <summary>
        /// Initializes a new instance of the <see cref="LineAnnotation" /> class.
        /// </summary>
        public LineAnnotation()
        {
            DefaultStyleKey = typeof (LineAnnotation);
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AnnotationRoot = GetAndAssertTemplateChild<Grid>("PART_LineAnnotationRoot");
            _line = GetAndAssertTemplateChild<Line>("PART_Line");
            _ghostLine = GetAndAssertTemplateChild<Line>("PART_GhostLine");
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategy(this);
            }

            return new CartesianAnnotationPlacementStrategy(this);
        }

        internal class CartesianAnnotationPlacementStrategy : CartesianAnnotationPlacementStrategyBase<LineAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(LineAnnotation annotation)
                : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                PlaceLineAnnotation(coordinates, Annotation);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return CalculateBasePoints(coordinates);
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<LineAnnotation>
        {
            public PolarAnnotationPlacementStrategy(LineAnnotation annotation)
                : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                PlaceLineAnnotation(cartesianCoordinates, Annotation);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                return CalculateBasePoints(cartesianCoordinates);
            }
        }

        private static void PlaceLineAnnotation(AnnotationCoordinates coordinates, LineAnnotation annotation)
        {
            annotation._line.X1 = coordinates.X1Coord;
            annotation._line.X2 = coordinates.X2Coord;
            annotation._line.Y1 = coordinates.Y1Coord;
            annotation._line.Y2 = coordinates.Y2Coord;

            annotation._ghostLine.X1 = coordinates.X1Coord;
            annotation._ghostLine.X2 = coordinates.X2Coord;
            annotation._ghostLine.Y1 = coordinates.Y1Coord;
            annotation._ghostLine.Y2 = coordinates.Y2Coord;
        }

        private static Point[] CalculateBasePoints(AnnotationCoordinates coordinates)
        {
            var pt1 = new Point(coordinates.X1Coord, coordinates.Y1Coord);
            var pt2 = new Point(coordinates.X2Coord, coordinates.Y2Coord);

            return new[] { pt1, pt2, };
        }

        internal Line Line{get { return _line; }}

        internal Line GhostLine { get { return _ghostLine; } }
    }
}
