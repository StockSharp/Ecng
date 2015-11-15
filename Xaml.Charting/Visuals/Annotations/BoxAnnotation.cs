// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BoxAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Input;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines a rectangle or box annotation, which may be placed on the chart at specific X1,Y1,X2,Y2 coordinates
    /// </summary>
    public class BoxAnnotation : AnnotationBase
    {
        /// <summary>
        /// Defines the CornerRadius DependencyProperty
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(BoxAnnotation), new PropertyMetadata(default(CornerRadius)));

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxAnnotation" /> class.
        /// </summary>
        public BoxAnnotation()
        {
            DefaultStyleKey = typeof (BoxAnnotation);
        }

        /// <summary>
        /// Gets or sets the CornerRadius of the box
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AnnotationRoot = GetAndAssertTemplateChild<Border>("PART_BoxAnnotationRoot");      
        }

        /// <summary>
        /// Gets the <see cref="Cursor" /> to use for the annotation when selected
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetSelectedCursor()
        {
#if SILVERLIGHT
            return Cursors.Hand;
#else
            return Cursors.SizeAll;
#endif
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

            var xCalc = XAxis.GetCurrentCoordinateCalculator();
            var yCalc = YAxis.GetCurrentCoordinateCalculator();

            var coords = GetCoordinates(canvas, xCalc, yCalc);

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

        internal class CartesianAnnotationPlacementStrategy : CartesianAnnotationPlacementStrategyBase<BoxAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(BoxAnnotation annotation) : base(annotation)
            {
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return new[]
                {
                    new Point(coordinates.X1Coord, coordinates.Y1Coord),
                    new Point(coordinates.X2Coord, coordinates.Y1Coord),
                    new Point(coordinates.X2Coord, coordinates.Y2Coord),
                    new Point(coordinates.X1Coord, coordinates.Y2Coord),
                };
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                PlaceBoxAnnotation(Annotation, coordinates);
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<BoxAnnotation>
        {
            public PolarAnnotationPlacementStrategy(BoxAnnotation annotation) : base(annotation)
            {
              
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                coordinates = GetCartesianAnnotationCoordinates(coordinates);

                PlaceBoxAnnotation(Annotation, coordinates);
            }

            public override void SetBasePoint(Point newPoint, int index)
            {
                // TODO: need to move GetPropertiesFromIndex to remove this workaround
                // added because polar box annotation has only 2 base points(x1/y1 and x2/y2) and GetPropertiesFromIndex have 4 indices
                if (index == 1)
                    index = 2;

                base.SetBasePoint(newPoint, index);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                return new[]
                {
                    new Point(cartesianCoordinates.X1Coord, cartesianCoordinates.Y1Coord),
                    new Point(cartesianCoordinates.X2Coord, cartesianCoordinates.Y2Coord),
                };
            }
        }

        private static void PlaceBoxAnnotation(BoxAnnotation annotation, AnnotationCoordinates coordinates)
        {
            double x1Coord = coordinates.X1Coord;
            double x2Coord = coordinates.X2Coord;
            double y1Coord = coordinates.Y1Coord;
            double y2Coord = coordinates.Y2Coord;

            if (x2Coord < x1Coord)
                NumberUtil.Swap(ref x1Coord, ref x2Coord);

            if (y2Coord < y1Coord)
                NumberUtil.Swap(ref y1Coord, ref y2Coord);

            annotation.Width = x2Coord - x1Coord + 1;
            annotation.Height = y2Coord - y1Coord + 1;

            Canvas.SetLeft(annotation, x1Coord);
            Canvas.SetTop(annotation, y1Coord);
        }
    }
}