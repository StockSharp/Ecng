using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ecng.Xaml.Charting.Utility
{
    internal static class PolarUtil
    {
        public static double CalculateViewportRadius(Size viewportSize)
        {
            return CalculateViewportRadius(viewportSize.Width, viewportSize.Height);
        }

        public static double CalculateViewportRadius(double width, double height)
        {
            return Math.Min(width, height)/2;
        }

        public static double AngleDistance(ref Point pt1, ref Point pt2)
        {
            return Math.Abs(pt1.X - pt2.X);
        }

        public static Size CalculatePolarViewportSize(Size size)
        {
            var viewportSize = new Size(360, CalculateViewportRadius(size));

            return viewportSize;
        }
    }
}
