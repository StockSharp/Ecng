// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
//
// LineAnnotationBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart.
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
using System.Windows.Input;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// A base class with shared properties for <see cref="LineAnnotation"/>, <see cref="HorizontalLineAnnotation"/> and <see cref="VerticalLineAnnotation"/>
    /// </summary>
    public abstract class LineAnnotationBase : AnnotationBase
    {
        /// <summary>Defines the StrokeDashArray DependencyProperty</summary>
        public static readonly DependencyProperty StrokeDashArrayProperty = DependencyProperty.Register("StrokeDashArray", typeof(DoubleCollection), typeof(LineAnnotationBase), new PropertyMetadata(null));
        /// <summary>Defines the StrokeThickness DependencyProperty</summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(LineAnnotationBase), new PropertyMetadata(1d, (d, args) => ((LineAnnotationBase)d).MeasureRefresh()));

        /// <summary>Defines the Stroke DependencyProperty</summary>
        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register("Stroke", typeof(Brush), typeof(LineAnnotationBase), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the Stroke brush of the line
        /// </summary>
        public Brush Stroke
        {
            get { return (Brush)GetValue(StrokeProperty); }
            set { SetValue(StrokeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the StrokeThickness of the line
        /// </summary>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        /// <summary>
        /// Gets or sets the StrokeDashArray of the line
        /// </summary>
        public DoubleCollection StrokeDashArray
        {
            get { return (DoubleCollection)GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
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
            if (index == 0)
            {
                x = X1Property;
                y = Y1Property;
            }
            else
            {
                x = X2Property;
                y = Y2Property;
            }
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

        protected void MeasureRefresh()
        {
            Dispatcher.BeginInvokeAlways(() =>
            {
                AnnotationRoot?.MeasureArrange();
                Refresh();
            });
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

            return PointUtil.DistanceFromLine(point, new Point(coords.X1Coord, coords.Y1Coord), new Point(coords.X2Coord, coords.Y2Coord)) < 7.07;
        }
    }
}
