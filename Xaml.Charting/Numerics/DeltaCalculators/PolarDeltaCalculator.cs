using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics
{
    internal class PolarDeltaCalculator : IDeltaCalculator
    {
        private const uint DefaultTicksCount = 12;

        /// <summary>
        /// Given an absolute Axis Min and Max, returns a TickRange instance containing sensible MinorDelta and MajorDelta values
        /// </summary>
        public IAxisDelta<double> GetDeltaFromRange(double min, double max, int minorsPerMajor, uint maxTicks = DefaultTicksCount)
        {
            Guard.ArgumentIsRealNumber(min);
            Guard.ArgumentIsRealNumber(max);

            return new DoubleAxisDelta(10, 30);
        }

        IAxisDelta IDeltaCalculator.GetDeltaFromRange(IComparable min, IComparable max, int minorsPerMajor, uint maxTicks)
        {
            return GetDeltaFromRange(min.ToDouble(), max.ToDouble(), minorsPerMajor, maxTicks);
        }
    }
}
