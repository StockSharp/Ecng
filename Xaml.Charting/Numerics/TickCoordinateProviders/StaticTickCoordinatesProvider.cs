using System;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Numerics.TickCoordinateProviders
{
    public class StaticTickCoordinatesProvider: DefaultTickCoordinatesProvider
    {
        private TickCoordinates _tickCoords;

        private IRange _prevRange;
        private IComparable _prevRangeDiff;

        public override TickCoordinates GetTickCoordinates(double[] minorTicks, double[] majorTicks)
        {
            var visibleRangeChanged = !ParentAxis.VisibleRange.Equals(_prevRange);
            var rangeDiffChanged = ParentAxis.VisibleRange.Diff.CompareTo(_prevRangeDiff) != 0;

            if (!_tickCoords.IsEmpty && visibleRangeChanged && (!rangeDiffChanged || ParentAxis.AutoTicks))
            {
                OverrideTicks(_tickCoords);
            }
            else
            {
               _tickCoords = base.GetTickCoordinates(minorTicks, majorTicks);

               _prevRange = ParentAxis.VisibleRange;
               _prevRangeDiff = _prevRange.Diff;
            }

            return _tickCoords;
        }

        private void OverrideTicks(TickCoordinates tickCoords)
        {
            var tickCalculator = ParentAxis.GetCurrentCoordinateCalculator();

            if (tickCalculator != null)
            {
                IComparable dataValue = null;
                for (int i = 0; i < tickCoords.MinorTickCoordinates.Length; i++)
                {
                    dataValue = ParentAxis.GetDataValue(tickCoords.MinorTickCoordinates[i]);
                    tickCoords.MinorTicks[i] = dataValue.ToDouble();
                }

                for (int i = 0; i < tickCoords.MajorTickCoordinates.Length; i++)
                {
                    dataValue = ParentAxis.GetDataValue(tickCoords.MajorTickCoordinates[i]);
                    tickCoords.MajorTicks[i] = dataValue.ToDouble();
                }
            }
        }
    }
}
