using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Represents DataSeries of StackedRenderableSeries for core. this class is not visible to user's code
    /// wraps original IDataSeries which is set by user
    /// </summary>
    internal class StackedDataSeriesComponent : StackedDataSeriesComponent<double, double>
    {
        private readonly IDataSeries _underlyingDataSeries;
        private readonly List<IDataSeries> _previousDataSeriesInSameGroup;

        public StackedDataSeriesComponent(List<IDataSeries> previousDataSeriesInSameGroup, IDataSeries underlyingDataSeries)
            : base(previousDataSeriesInSameGroup.FirstOrDefault() ?? underlyingDataSeries)
        {
            _previousDataSeriesInSameGroup = previousDataSeriesInSameGroup;
            _underlyingDataSeries = underlyingDataSeries;
        }

        protected override IList<IDataSeries> PreviousDataSeriesInSameGroup 
        {
            get
            {
                return _previousDataSeriesInSameGroup;
            }
        }

        protected override IDataSeries UnderlyingDataSeries
        {
            get
            {
                return _underlyingDataSeries;
            }
        }

        public override IList<double> XValues
        {
            get { return ((IDataSeries)this).XValues.Cast<IComparable>().Select(v => v.ToDouble()).ToArray(); }
        }

        public override double GetYMaxAt(int index, double existingYMax)
        {
            // todo {sergey} use results of resampling if it is possible
            var sum = 0d;
            foreach (var otherComponent in _previousDataSeriesInSameGroup)
            {
                sum += ((IComparable)otherComponent.YValues[index]).ToDouble();
            }
            sum += ((IComparable)_underlyingDataSeries.YValues[index]).ToDouble();

            return double.IsNaN(sum) ? existingYMax : Math.Max(existingYMax, sum);
        }
    }
}
