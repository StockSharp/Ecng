// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RubberBandXyZoomModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    internal class CartesianRubberBandOverlayPlacementStrategy : IRubberBandOverlayPlacementStrategy
    {
        private readonly IChartModifier _modifier;
        private Shape _rectangle;

        public CartesianRubberBandOverlayPlacementStrategy(IChartModifier modifier)
        {
            _modifier = modifier;
        }

        public double CalculateDraggedDistance(Point start, Point end)
        {
            return PointUtil.Distance(start, end);
        }

        public Shape CreateShape(Brush rubberBandFill, Brush rubberBandStroke, DoubleCollection rubberBandStrokeDashArray)
        {
            _rectangle = new Rectangle
            {
                Fill = rubberBandFill,
                Stroke = rubberBandStroke,
                StrokeDashArray = rubberBandStrokeDashArray
            };
            return _rectangle;
        }

        public Point UpdateShape(bool isXAxisOnly, Point start, Point end)
        {
            //Old SetReticulePosition
            if (isXAxisOnly)
            {
                if (_modifier.XAxis.IsHorizontalAxis)
                {
                    start.Y = 0;
                    end.Y = _modifier.ModifierSurface.ActualHeight;
                }
                else
                {
                    start.X = 0;
                    end.X = _modifier.ModifierSurface.ActualWidth;
                }
            }

            var modifierRect = new Rect(0, 0, _modifier.ModifierSurface.ActualWidth, _modifier.ModifierSurface.ActualHeight);
            end = modifierRect.ClipToBounds(end);

            var rect = new Rect(start, end);
            Canvas.SetLeft(_rectangle, rect.X);
            Canvas.SetTop(_rectangle, rect.Y);

            //Debug.WriteLine("SetRect... x={0}, y={1}, w={2}, h={3}, IsMaster? {4}", rect.X, rect.Y, rect.Width, rect.Height, isMaster);

            _rectangle.Width = rect.Width;
            _rectangle.Height = rect.Height;

            return end;
        }

        public void SetupShape(bool isXAxisOnly, Point start, Point end)
        {
            UpdateShape(isXAxisOnly, start, end);
        }
    }
}