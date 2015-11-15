using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal class PolarCartesianTransformationHelper
    {
        private readonly Point _origin;

        public PolarCartesianTransformationHelper(Point origin)
        {
            _origin = origin;
        }

        public PolarCartesianTransformationHelper(double viewportWidth, double viewportHeight)
        {
            _origin = new Point(viewportWidth / 2, viewportHeight / 2);
        }

        public Point Origin { get { return _origin; } }

        public Point ToCartesian(double angle, double r)
        {
            angle *= Math.PI/180d;

            var x = _origin.X + r * Math.Cos(angle);
            var y = _origin.Y + r * Math.Sin(angle);

            return new Point(x,y);
        }

        public Point ToPolar(double x, double y)
        {
            if(x.Equals(_origin.X) && y.Equals(_origin.Y))
                return new Point(0,0);

            x -= _origin.X;
            y -= _origin.Y;
            
            var r = Math.Sqrt(x*x + y*y);
            var angle = Math.Atan(y / x) / (Math.PI / 180d);

            if (x < 0)
            {
                angle += 180;
            }
            else if(y<0)
            {
                angle += 360;
            }

            return new Point(angle, r);
        }
    }
}
