using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics.CoordinateProviders
{
    public class PolarTickCoordinatesProvider : DefaultTickCoordinatesProvider
    {
        protected override bool IsInBounds(double coord)
        {
            bool isInBounds;

            if (ParentAxis.IsXAxis)
            {
                var lowerBound = 0;
                var upperBound = 360;

                isInBounds = coord > lowerBound && coord < upperBound;

                // Prevents the first & last ticks from overlapping on a circular axis
                isInBounds |= ParentAxis.FlipCoordinates ? coord <= upperBound : coord >= lowerBound;
            }
            else
            {
                isInBounds = base.IsInBounds(coord);    
            }

            return isInBounds;
        }
    }
}
