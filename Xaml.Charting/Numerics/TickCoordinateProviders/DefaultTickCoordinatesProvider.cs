using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Numerics.TickCoordinateProviders
{
    public class DefaultTickCoordinatesProvider
    {
        private IAxis _parentAxis;

        public IAxis ParentAxis
        {
            get { return _parentAxis; }
            protected set { _parentAxis = value; }
        }

        /// <summary>
        /// Called when the <see cref="DefaultTickCoordinatesProvider"/> is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        /// <param name="parentAxis">The parent <see cref="IAxis"/> instance</param>
        public virtual void Init(IAxis parentAxis)
        {
            ParentAxis = parentAxis;
        }

        /// <summary>
        /// Converts arrays of major and minor ticks (data values) into <see cref="TickCoordinates"/> structure containing pixel coordinates
        /// </summary>
        /// <param name="minorTicks">The minor ticks, cast to double</param>
        /// <param name="majorTicks">The major ticks, cast to double</param>
        /// <returns>The <see cref="TickCoordinates"/> structure containing pixel coordinates</returns>
        public virtual TickCoordinates GetTickCoordinates(double[] minorTicks, double[] majorTicks)
        {
            var minorTickCoords = new float[minorTicks.Length];
            var majorTickCoords = new float[majorTicks.Length];

            var tickCalculator = ParentAxis.GetCurrentCoordinateCalculator();

            if (tickCalculator != null)
            {
                for (int i = 0; i < minorTicks.Length; i++)
                {
                    var coord = (float)tickCalculator.GetCoordinate(minorTicks[i]);
                    minorTickCoords[i] = coord;
                }

                for (int i = 0; i < majorTicks.Length; i++)
                {
                    var coord = (float)tickCalculator.GetCoordinate(majorTicks[i]);
                    majorTickCoords[i] = coord;
                }
            }

            return new TickCoordinates(minorTicks, majorTicks, minorTickCoords, majorTickCoords);
        }
    }
}
