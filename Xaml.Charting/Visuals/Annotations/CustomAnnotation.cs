// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CustomAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.AttachedProperties;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.StrategyManager;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// CustomAnnotation is a ContentControl which can be overlaid on the annotation surfaces. For examples of use, see the Annotations Are Easy and Create Annotations Dynamically examples in the trial download
    /// </summary>
    public class CustomAnnotation : AnchorPointAnnotation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAnnotation"/> class.
        /// </summary>
        public CustomAnnotation()
        {
            DefaultStyleKey = typeof (CustomAnnotation);
            AnnotationRoot = this;
        }        

        /// <summary>
        /// Called when the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property changes.
        /// </summary>
        /// <param name="oldContent">The old value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property.</param>
        /// <param name="newContent">The new value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            var content = newContent as FrameworkElement;

            AnnotationRoot = content ?? this;

            this.Refresh();
        }

#if !SILVERLIGHT
        /// <summary>
        /// Called when the <see cref="P:System.Windows.Controls.ContentControl.ContentTemplate" /> property changes.
        /// </summary>
        /// <param name="oldContentTemplate">The old value of the <see cref="P:System.Windows.Controls.ContentControl.ContentTemplate" /> property.</param>
        /// <param name="newContentTemplate">The new value of the <see cref="P:System.Windows.Controls.ContentControl.ContentTemplate" /> property.</param>
        protected override void OnContentTemplateChanged(DataTemplate oldContentTemplate, DataTemplate newContentTemplate)
        {
            base.OnContentTemplateChanged(oldContentTemplate, newContentTemplate);

            AnnotationRoot = (FrameworkElement)Content ?? this;

            this.Refresh();
        }
#endif

        /// <summary>
        /// Returns true if the Point is within the bounds of the current <see cref="IHitTestable" /> element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>
        /// true if the Point is within the bounds
        /// </returns>
        public override bool IsPointWithinBounds(Point point)
        {
            var path = AnnotationRoot;

            if (path != null)
            {
                return path.IsPointWithinBounds(point);
            }

            return false;            
        }

        protected override IAnnotationPlacementStrategy GetCurrentPlacementStrategy()
        {
            if (XAxis != null && XAxis.IsPolarAxis)
            {
                return new PolarAnnotationPlacementStrategy(this);
            }

            return new CartesianAnnotationPlacementStrategy(this);
        }

        internal class CartesianAnnotationPlacementStrategy : CartesianAnnotationPlacementStrategyBase<CustomAnnotation>
        {
            public CartesianAnnotationPlacementStrategy(CustomAnnotation annotation) : base(annotation)
            {
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                PlaceCustomAnnotation(coordinates, Annotation);
            }

            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                return CalculateBasePoints(coordinates, Annotation);
            }

            public override bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas canvas)
            {
                bool outOfBounds = coordinates.X1Coord < 0 ||
                                   coordinates.X1Coord > canvas.ActualWidth ||
                                   coordinates.Y1Coord < 0 ||
                                   coordinates.Y1Coord > canvas.ActualHeight;

                return !outOfBounds;
            }

            protected override void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, ref double horizontalOffset, ref double verticalOffset, IAnnotationCanvas canvas)
            {
                var x1 = coordinates.X1Coord + horizontalOffset;
                var y1 = coordinates.Y1Coord + verticalOffset;

                // If any are out of bounds ... 
                if (!IsCoordinateValid(x1, canvas.ActualWidth) || !IsCoordinateValid(y1, canvas.ActualHeight))
                {
                    x1 = double.IsNaN(x1) ? 0 : x1;
                    y1 = double.IsNaN(y1) ? 0 : y1;

                    // Clip to bounds
                    if (x1 < 0) horizontalOffset -= x1;
                    if (x1 > canvas.ActualWidth) horizontalOffset -= x1 - (canvas.ActualWidth - 1);

                    if (y1 < 0) verticalOffset -= y1;
                    if (y1 > canvas.ActualHeight) verticalOffset -= y1 - (canvas.ActualHeight - 1);
                }

                // Reassign
                coordinates.X1Coord = coordinates.X1Coord + horizontalOffset;
                coordinates.Y1Coord = coordinates.Y1Coord + verticalOffset;

                var dataValues = FromCoordinates(coordinates.X1Coord, coordinates.Y1Coord);

                Annotation.SetCurrentValue(X1Property, dataValues[0]);
                Annotation.SetCurrentValue(Y1Property, dataValues[1]);
            }
        }

        internal class PolarAnnotationPlacementStrategy : PolarAnnotationPlacementStrategyBase<CustomAnnotation>
        {
            public PolarAnnotationPlacementStrategy(CustomAnnotation annotation)
                : base(annotation)
            {
            }

            protected override bool IsInBoundsInternal(AnnotationCoordinates coordinates, Size canvasSize)
            {
                bool outOfBounds = coordinates.X1Coord < 0 ||
                                   coordinates.X1Coord > canvasSize.Width||
                                   coordinates.Y1Coord < 0 ||
                                   coordinates.Y1Coord > canvasSize.Height;

                return !outOfBounds;
            }

            public override void PlaceAnnotation(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                PlaceCustomAnnotation(cartesianCoordinates, Annotation);
            }
            
            public override Point[] GetBasePoints(AnnotationCoordinates coordinates)
            {
                var cartesianCoordinates = GetCartesianAnnotationCoordinates(coordinates);

                return CalculateBasePoints(cartesianCoordinates, Annotation);
            }

            protected override void InternalMoveAnntotationTo(AnnotationCoordinates coordinates, Point x1y1Offset, Point x2y2Offset, Size canvasSize)
            {
                var xOffset = x1y1Offset.X;
                var yOffset = x1y1Offset.Y;

                var x1 = coordinates.X1Coord + xOffset;
                var y1 = coordinates.Y1Coord + yOffset;

                // If any are out of bounds ... 
                if (!IsCoordinateValid(x1, canvasSize.Width) || !IsCoordinateValid(y1, canvasSize.Height))
                {
                    x1 = double.IsNaN(x1) ? 0 : x1;
                    y1 = double.IsNaN(y1) ? 0 : y1;

                    // Clip to bounds
                    if (x1 < 0) xOffset -= x1;
                    if (x1 > canvasSize.Width) xOffset -= x1 - (canvasSize.Width - 1);

                    if (y1 < 0) yOffset -= y1;
                    if (y1 > canvasSize.Height) yOffset -= y1 - (canvasSize.Height - 1);
                }

                // Reassign
                coordinates.X1Coord = coordinates.X1Coord + xOffset;
                coordinates.Y1Coord = coordinates.Y1Coord + yOffset;

                var dataValues = FromCoordinates(coordinates.X1Coord, coordinates.Y1Coord);

                Annotation.SetCurrentValue(X1Property, dataValues[0]);
                Annotation.SetCurrentValue(Y1Property, dataValues[1]);
            }
        }

        private static void PlaceCustomAnnotation(AnnotationCoordinates coordinates, CustomAnnotation annotation)
        {
            coordinates = annotation.GetAnchorAnnotationCoordinates(coordinates);

            double x1Coord = coordinates.X1Coord;
            double y1Coord = coordinates.Y1Coord;

            Canvas.SetLeft(annotation, x1Coord);
            Canvas.SetTop(annotation, y1Coord);
        }

        private static Point[] CalculateBasePoints(AnnotationCoordinates coordinates, CustomAnnotation annotation)
        {
            coordinates = annotation.GetAnchorAnnotationCoordinates(coordinates);

            if (annotation.AnnotationRoot == null)
                return new Point[] {};

            return new[]
            {
                new Point(coordinates.X1Coord, coordinates.Y1Coord)
            };
        }
    }
}