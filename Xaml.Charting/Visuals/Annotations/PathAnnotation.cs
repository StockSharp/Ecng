// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2012. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// PathAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting
{    
    /// <summary>
    /// The PathAnnotation allows setting of Path Data (e.g. any custom shape) on the Annotation canvas. 
    /// To use, create a new PathAnnotation and set Data= ... as if you were creating a Path in WPF or Silverlight
    /// </summary>
    [TemplatePart(Name = "PART_PathAnnotationRoot", Type = typeof(Path))]
    public class PathAnnotation : AnchorPointAnnotation
    {
        /// <summary>Defines the Stroke DependencyProperty</summary>
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(PathAnnotation), new PropertyMetadata(null));
        /// <summary>Defines the Fill DependencyProperty</summary>
        public static readonly DependencyProperty FillProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(PathAnnotation), new PropertyMetadata(null));
        /// <summary>Defines the Data DependencyProperty</summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(Geometry), typeof(PathAnnotation), new PropertyMetadata(null));
        /// <summary>Defines the StrokeThickness DependencyProperty</summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(PathAnnotation), new PropertyMetadata(1.0));
        /// <summary>Defines the StrokeStartLineCap DependencyProperty</summary>
        public static readonly DependencyProperty StrokeStartLineCapProperty = DependencyProperty.Register("StrokeStartLineCap", typeof(PenLineCap), typeof(PathAnnotation), new PropertyMetadata(PenLineCap.Flat));
        /// <summary>Defines the StrokeEndLineCap DependencyProperty</summary>
        public static readonly DependencyProperty StrokeEndLineCapProperty = DependencyProperty.Register("StrokeEndLineCap", typeof(PenLineCap), typeof(PathAnnotation), new PropertyMetadata(PenLineCap.Flat));
        /// <summary>Defines the StrokeDashCap DependencyProperty</summary>
        public static readonly DependencyProperty StrokeDashCapProperty = DependencyProperty.Register("StrokeDashCap", typeof(PenLineCap), typeof(PathAnnotation), new PropertyMetadata(PenLineCap.Flat));
        /// <summary>Defines the StrokeLineJoin DependencyProperty</summary>
        public static readonly DependencyProperty StrokeLineJoinProperty = DependencyProperty.Register("StrokeLineJoin", typeof(PenLineJoin), typeof(PathAnnotation), new PropertyMetadata(PenLineJoin.Miter));
        /// <summary>Defines the StrokeMiterLimit DependencyProperty</summary>
        public static readonly DependencyProperty StrokeMiterLimitProperty = DependencyProperty.Register("StrokeMiterLimit", typeof(double), typeof(PathAnnotation), new PropertyMetadata(10.0));
        /// <summary>Defines the StrokeDashOffset DependencyProperty</summary>
        public static readonly DependencyProperty StrokeDashOffsetProperty = DependencyProperty.Register("StrokeDashOffset", typeof(double), typeof(PathAnnotation), new PropertyMetadata(0.0));
        /// <summary>Defines the StrokeDashArray DependencyProperty</summary>
        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register("StrokeDashArray", typeof(DoubleCollection), typeof(PathAnnotation), new PropertyMetadata(new DoubleCollection()));

        /// <summary>
        /// Initializes a new instance of the <see cref="PathAnnotation" /> class.
        /// </summary>
        public PathAnnotation()
        {
            DefaultStyleKey = typeof (PathAnnotation);
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate" />.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AnnotationRoot = GetTemplateChild("PART_PathAnnotationRoot") as Path; 
        }

        /// <summary>
        /// Gets or sets the Path data. This is equivalent to setting <see cref="Path.Data"/> 
        /// </summary>        
        public Geometry Data
        {
            get { return (Geometry) GetValue(DataProperty); }
            set { SetValue(DataProperty, value);}
        }

        /// <summary>Gets or sets the <see cref="T:System.Windows.Media.Brush" /> that specifies how the shape's interior is painted. </summary>
        /// <returns>A <see cref="T:System.Windows.Media.Brush" /> that describes how the shape's interior is painted. The default is null.</returns>
        public Brush Fill
        {
            get { return (Brush)base.GetValue(FillProperty); }
            set { base.SetValue(FillProperty, value); }
        }

        /// <summary>Gets or sets the <see cref="T:System.Windows.Media.Brush" /> that specifies how the <see cref="T:System.Windows.Shapes.Shape" /> outline is painted. </summary>
        /// <returns>A <see cref="T:System.Windows.Media.Brush" /> that specifies how the <see cref="T:System.Windows.Shapes.Shape" /> outline is painted. The default is null.</returns>
        public Brush Stroke
        {
            get { return (Brush)base.GetValue(StrokeProperty); }
            set { base.SetValue(StrokeProperty, value); }
        }

        /// <summary>Gets or sets the width of the <see cref="T:System.Windows.Shapes.Shape" /> outline. </summary>
        /// <returns>The width of the <see cref="T:System.Windows.Shapes.Shape" /> outline.</returns>
        public double StrokeThickness
        {
            get { return (double)base.GetValue(StrokeThicknessProperty); }
            set { base.SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>Gets or sets a <see cref="T:System.Windows.Media.PenLineCap" /> enumeration value that describes the <see cref="T:System.Windows.Shapes.Shape" /> at the start of a <see cref="P:System.Windows.Shapes.Shape.Stroke" />. </summary>
        /// <returns>One of the <see cref="T:System.Windows.Media.PenLineCap" /> enumeration values. The default is <see cref="F:System.Windows.Media.PenLineCap.Flat" />.</returns>
        public PenLineCap StrokeStartLineCap
        {
            get { return (PenLineCap)base.GetValue(StrokeStartLineCapProperty); }
            set { base.SetValue(StrokeStartLineCapProperty, value); }
        }

        /// <summary>Gets or sets a <see cref="T:System.Windows.Media.PenLineCap" /> enumeration value that describes the <see cref="T:System.Windows.Shapes.Shape" /> at the end of a line. </summary>
        /// <returns>One of the enumeration values for <see cref="T:System.Windows.Media.PenLineCap" />. The default is <see cref="F:System.Windows.Media.PenLineCap.Flat" />.</returns>
        public PenLineCap StrokeEndLineCap
        {
            get { return (PenLineCap)base.GetValue(StrokeEndLineCapProperty); }
            set { base.SetValue(StrokeEndLineCapProperty, value); }
        }

        /// <summary>Gets or sets a <see cref="T:System.Windows.Media.PenLineCap" /> enumeration value that specifies how the ends of a dash are drawn. </summary>
        /// <returns>One of the enumeration values for <see cref="T:System.Windows.Media.PenLineCap" />. The default is <see cref="F:System.Windows.Media.PenLineCap.Flat" />. </returns>
        public PenLineCap StrokeDashCap
        {
            get { return (PenLineCap)base.GetValue(StrokeDashCapProperty); }
            set { base.SetValue(StrokeDashCapProperty, value); }
        }

        /// <summary>Gets or sets a <see cref="T:System.Windows.Media.PenLineJoin" /> enumeration value that specifies the type of join that is used at the vertices of a <see cref="T:System.Windows.Shapes.Shape" />.</summary>
        /// <returns>One of the enumeration values for <see cref="T:System.Windows.Media.PenLineJoin" /></returns>
        public PenLineJoin StrokeLineJoin
        {
            get { return (PenLineJoin)base.GetValue(StrokeLineJoinProperty); }
            set { base.SetValue(StrokeLineJoinProperty, value); }
        }

        /// <summary>Gets or sets a limit on the ratio of the miter length to half the <see cref="P:System.Windows.Shapes.Shape.StrokeThickness" /> of a <see cref="T:System.Windows.Shapes.Shape" /> element. </summary>
        /// <returns>The limit on the ratio of the miter length to the <see cref="P:System.Windows.Shapes.Shape.StrokeThickness" /> of a <see cref="T:System.Windows.Shapes.Shape" /> element. This value is always a positive number that is greater than or equal to 1.</returns>
        public double StrokeMiterLimit
        {
            get { return (double)base.GetValue(StrokeMiterLimitProperty); }
            set { base.SetValue(StrokeMiterLimitProperty, value); }
        }
        /// <summary>Gets or sets a <see cref="T:System.Double" /> that specifies the distance within the dash pattern where a dash begins.</summary>
        /// <returns>A <see cref="T:System.Double" /> that represents the distance within the dash pattern where a dash begins.</returns>
        public double StrokeDashOffset
        {
            get { return (double)base.GetValue(StrokeDashOffsetProperty); }
            set { base.SetValue(StrokeDashOffsetProperty, value); }
        }

        /// <summary>Gets or sets a collection of <see cref="T:System.Double" /> values that indicate the pattern of dashes and gaps that is used to outline shapes. </summary>
        /// <returns>A collection of <see cref="T:System.Double" /> values that specify the pattern of dashes and gaps. </returns>
        public DoubleCollection StrokeDashArray
        {
            get { return (DoubleCollection)base.GetValue(StrokeDashArrayProperty); }
            set { base.SetValue(StrokeDashArrayProperty, value); }
        }

        /// <summary>
        /// Override in derived classes to handle specific placement of the annotation at the given <see cref="AnnotationCoordinates" />
        /// </summary>
        /// <param name="coordinates">The normalised <see cref="AnnotationCoordinates" /></param>
        protected override void PlaceAnnotation(AnnotationCoordinates coordinates)
        {
            double x1Coord = coordinates.X1Coord;
            double y1Coord = coordinates.Y1Coord;

            Canvas.SetLeft(this, x1Coord);
            Canvas.SetTop(this, y1Coord);
        }

        /// <summary>
        /// This method is used in internally by the <see cref="AnnotationResizeAdorner" />. Gets the adorner point positions
        /// </summary>
        /// <param name="coordinates">The previously calculated <see cref="AnnotationCoordinates" /> in screen pixels.</param>
        /// <returns>
        /// A list of points in screen pixels denoting the Adorner corners
        /// </returns>
        protected override Point[] GetBasePoints(AnnotationCoordinates coordinates)
        {
            var path = AnnotationRoot as Path;

            if (path.Data != null)
            {
                var rect = path.GetBoundsRelativeTo(ModifierSurface);

                var tl = new Point(coordinates.X1Coord, coordinates.Y1Coord);
                var tr = new Point(coordinates.X1Coord + rect.Width, coordinates.Y1Coord);

                var bl = new Point(coordinates.X1Coord, coordinates.Y1Coord + rect.Height);
                var br = new Point(coordinates.X1Coord + rect.Width, coordinates.Y1Coord + rect.Height);

                return new[]
                           {
                               tl, tr, br, bl
                           };
            }

            return new Point[]{};
        }

        /// <summary>
        /// Called internally to marshal pixel points to X1,Y1,X2,Y2 values. 
        /// Taking a pixel point (<paramref name="newPoint"/>) and base point <paramref name="index"/>, sets the X,Y data-values. 
        /// </summary>
        /// <param name="newPoint">The pixel point</param>
        /// <param name="index">The base point index, where 0, 1, 2, 3 refer to the four corners of an Annotation</param>
        /// <param name="canvas">The canvas that this annotation is on</param>
        /// <param name="xCalc">The current <see cref="ICoordinateCalculator{T}"/> for the X-Axis</param>
        /// <param name="yCalc">The current <see cref="ICoordinateCalculator{T}"/> for the Y-Axis</param>
        protected override void SetBasePoint(Point newPoint, int index, Canvas canvas, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc)
        {
        }

        /// <summary>
        /// This method is used internally by the <see cref="AnnotationDragAdorner" />. Programmatically moves the annotation by an X,Y offset.
        /// </summary>
        /// <param name="horizOffset">The horizontal offset to move by.</param>
        /// <param name="vertOffset">The vertical offset to move by.</param>
        /// <param name="canvas">The canvas to move the annotation on.</param>
        /// <param name="xCalc">The xAxis <see cref="ICoordinateCalculator{T}"/> instance.</param>
        /// <param name="yCalc">The yAxis <see cref="ICoordinateCalculator{T}"/> instance.</param>
        protected override void MoveAnnotation(double horizOffset, double vertOffset, Canvas canvas, ICoordinateCalculator<double> xCalc, ICoordinateCalculator<double> yCalc)
        {
            var path = AnnotationRoot as Path;

            var coordinates = GetCoordinates(canvas, xCalc, yCalc, true);

            var rect = path.GetBoundsRelativeTo(ModifierSurface);

            var x1 = coordinates.X1Coord + horizOffset;
            var x2 = coordinates.X1Coord + rect.Width + horizOffset;
            var y1 = coordinates.Y1Coord + vertOffset;
            var y2 = coordinates.Y1Coord + rect.Height + vertOffset;

            if (!IsCoordinateValid(x1, canvas.ActualWidth) || !IsCoordinateValid(x2, canvas.ActualWidth))
            {
                x1 = coordinates.X1Coord;
            }

            if (!IsCoordinateValid(y1, canvas.ActualHeight) || !IsCoordinateValid(y2, canvas.ActualHeight))
            {
                y1 = coordinates.Y1Coord;
            }

            X1 = FromCoordinate(x1, canvas.ActualWidth, xCalc);
            Y1 = FromCoordinate(y1, canvas.ActualHeight, yCalc);
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
            var path = AnnotationRoot as Path;

            point = ParentSurface.ModifierSurface.TranslatePoint(point, this);

            bool result = false;

            if (path.Data != null)
            {
                result =
#if SILVERLIGHT
                path.Data.Bounds.Contains(point);
#else
            path.Data.FillContains(point);
#endif
            }

            return result;
        }
    }
}