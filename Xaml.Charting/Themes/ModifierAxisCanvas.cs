// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ModifierAxisCanvas.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// A canvas which overlays an axis and is used to place annotations, such as cursor labels and <see cref="AxisMarkerAnnotation"/>
    /// </summary>
    public class ModifierAxisCanvas : AxisCanvas, IAnnotationCanvas
    {
        internal AxisBase ParentAxis { get; set; }

        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            return this.TranslatePoint(point, relativeTo);
        }

        public bool IsPointWithinBounds(Point point)
        {
            return this.IsPointWithinBounds(point);
        }

        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            return this.GetBoundsRelativeTo(relativeTo);
        }
    }

    public class PolarModifierAxisCanvas : ModifierAxisCanvas
    {
        private PolarCartesianTransformationHelper _transformationHelper;
        private double _outerRadius;
        private double _innerRadius;
        private const double MaxDegree = 360;

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _transformationHelper = new PolarCartesianTransformationHelper(arrangeSize.Width, arrangeSize.Height);
            
            var thickness = PolarPanel.GetThickness(ParentAxis);
            _outerRadius = PolarUtil.CalculateViewportRadius(arrangeSize);
            _innerRadius = _outerRadius - thickness;
            
            return base.ArrangeOverride(arrangeSize);
        }

        protected override Rect GetArrangedRect(Size arrangeSize, UIElement element)
        {
            double xOrigin;
            var x = GetXCoord(element, out xOrigin);

            double yOrigin;
            var y = GetYCoord(element, out yOrigin);

            var location = _transformationHelper.ToCartesian(x, y);

            var renderTransorm = new RotateTransform() {Angle = x + 90, CenterX = xOrigin, CenterY = yOrigin};
            element.RenderTransform = renderTransorm;
            element.RenderTransformOrigin = new Point(xOrigin, yOrigin);


            location.X -= element.DesiredSize.Width*xOrigin;
            location.Y -= element.DesiredSize.Height*yOrigin;

            return new Rect(location, element.DesiredSize);
        }

        private double GetYCoord(UIElement element, out double yOrigin)
        {
            double y = 0.0;
            yOrigin = 0.0;
            double top = GetTop(element);
            double centerTop = GetCenterTop(element);

            if (!top.IsNaN())
            {
                y = _outerRadius - top;
                yOrigin = 0.0;
            }
            else if (!centerTop.IsNaN())
            {
                y = _outerRadius - centerTop;
                yOrigin = 0.5;
            }
            else
            {
                double bottom = GetBottom(element);
                if (!bottom.IsNaN())
                {
                    y = _innerRadius + bottom;
                    yOrigin = 1.0;
                }
                else
                {
                    double centerBottom = GetCenterBottom(element);
                    if (!centerBottom.IsNaN())
                    {
                        y = _innerRadius + centerBottom;
                        yOrigin = 0.5;
                    }
                }
            }
            return y;
        }

        private static double GetXCoord(UIElement element, out double xOrigin)
        {
            double x = 0.0;
            xOrigin = 0.0;
            double left = GetLeft(element);
            double centerLeft = GetCenterLeft(element);

            if (!left.IsNaN())
            {
                x = left;
                xOrigin = 0.0;
            }
            else if (!centerLeft.IsNaN())
            {
                x = centerLeft;
                xOrigin = 0.5;
            }
            else
            {
                double right = GetRight(element);
                if (!right.IsNaN())
                {
                    x = MaxDegree - right;
                    xOrigin = 1.0;
                }
                else
                {
                    double centerRight = GetCenterRight(element);
                    if (!centerRight.IsNaN())
                    {
                        x = MaxDegree - centerRight;
                        xOrigin = 0.5;
                    }
                }
            }
            return x;
        }
    }
}