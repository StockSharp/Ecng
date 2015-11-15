// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanAxisBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// A common base class for <see cref="DateTimeAxis"/> and <see cref="TimeSpanAxis"/>
    /// </summary>
    public abstract class TimeSpanAxisBase : AxisBase, IAxis
    {
        /// <summary>
        /// Gets or sets the minor delta.
        /// </summary>
        /// <value>The minor delta.</value>
        /// <remarks></remarks>
        IComparable IAxisParams.MinorDelta { get { return MinorDelta; } set { MinorDelta = (TimeSpan)value; } }

        /// <summary>
        /// Gets or sets the major delta.
        /// </summary>
        /// <value>The major delta.</value>
        /// <remarks></remarks>
        IComparable IAxisParams.MajorDelta { get { return MajorDelta; } set { MajorDelta = (TimeSpan)value; } }

        /// <summary>
        /// Gets or sets the major delta.
        /// </summary>
        /// <value>The major delta.</value>
        /// <remarks></remarks>
        public new TimeSpan MajorDelta
        {
            get { return (TimeSpan)GetValue(MajorDeltaProperty); }
            set { SetValue(MajorDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minor delta.
        /// </summary>
        /// <value>The minor delta.</value>
        /// <remarks></remarks>
        public new TimeSpan MinorDelta
        {
            get { return (TimeSpan)GetValue(MinorDeltaProperty); }
            set { SetValue(MinorDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the MinimalZoomConstrain of the Axis. This is used to set minimum distance between Min and Max of the VisibleRange 
        /// </summary>
        /// <value>The minimum distance between Min and Max of the VisibleRange</value>
        public new TimeSpan? MinimalZoomConstrain
        {
            get { return (TimeSpan?)GetValue(MinimalZoomConstrainProperty); }
            set { SetValue(MinimalZoomConstrainProperty, value); }
        }

        /// <summary>
        /// Calculates the zoom-to-fit Y-Range of the axis, accounting for the data that
        /// is in the viewport and applying any GrowBy margin        
        /// </summary>
        /// <param name="renderPassInfo">Struct containing data for the current render pass</param>
        /// <returns>
        /// The VisibleRange of the axis
        /// </returns>
        public override IRange CalculateYRange(RenderPassInfo renderPassInfo)
        {
            if (IsXAxis) throw new InvalidOperationException("CalculateYRange is only valid on Y-Axis types");

            double max = TimeSpan.MinValue.ToDouble();
            double min = TimeSpan.MaxValue.ToDouble();

            int numberSeries = renderPassInfo.PointSeries.Length;
            for (int i = 0; i < numberSeries; i++)
            {
                var rSeries = renderPassInfo.RenderableSeries[i];
                var pSeries = renderPassInfo.PointSeries[i];

                if (rSeries == null || pSeries == null || rSeries.YAxisId != Id)
                    continue;

                var pSeriesRange = pSeries.GetYRange();
                min = min < pSeriesRange.Min ? min : pSeriesRange.Min;
                max = max > pSeriesRange.Max ? max : pSeriesRange.Max;
            }

            var visibleRange = ToVisibleRange(min, max);
            return visibleRange.GrowBy(GrowBy.Min, GrowBy.Max);
        }

        /// <summary>
        /// When overriden in a derived class, converts a Min Max <see cref="IComparable"/> value into an <see cref="IRange"/> of the correct type for this axis
        /// </summary>
        /// <param name="min">The min value</param>
        /// <param name="max">The max value</param>
        /// <returns>The <see cref="IRange"/> instance</returns>
        protected abstract IRange ToVisibleRange(IComparable min, IComparable max);

        /// <summary>
        /// Gets the Maximum Range of the axis, which is equal to the DataRange including any GrowBy factor applied
        /// </summary>
        /// <returns></returns>
        public override IRange GetMaximumRange()
        {
            var maximumRange = base.GetMaximumRange();

            return ToVisibleRange(maximumRange.Min, maximumRange.Max);
        }

        /// <summary>
        /// Calculates data range of current axis
        /// </summary>
        /// <returns></returns>
        protected override IRange CalculateDataRange()
        {
            var dataRange = base.CalculateDataRange();

            if (dataRange != null)
                dataRange = ToVisibleRange(dataRange.Min, dataRange.Max);

            return dataRange;
        }


        /// <summary>
        /// Calcuates the delta's for use in this render pass
        /// </summary>
        /// <remarks></remarks>
        protected override void CalculateDelta()
        {
            if (AutoTicks)
            {
                var tickCalculator = (IDateDeltaCalculator)GetDeltaCalculator();
                var maxAutoTicks = GetMaxAutoTicks();

                var delta = tickCalculator.GetDeltaFromRange(VisibleRange.Min, VisibleRange.Max, MinorsPerMajor,
                                                                     maxAutoTicks);

                MajorDelta = delta.MajorDelta;
                MinorDelta = delta.MinorDelta;

                UltrachartDebugLogger.Instance.WriteLine("CalculateDelta: Major={0}, Minor={1}", delta.MajorDelta,
                                                       delta.MinorDelta);
            }
        }

        /// <summary>
        /// Transforms a pixel coordinate into a data value for this axis.
        /// </summary>
        /// <param name="pixelCoordinate"></param>
        /// <returns></returns>
        public override IComparable GetDataValue(double pixelCoordinate)
        {
            var dataValue = base.GetDataValue(pixelCoordinate);

            return ConvertTickToDataValue(dataValue);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public override IAxis Clone()
        {
            var cloneAxis = (TimeSpanAxisBase)Activator.CreateInstance(GetType());

            if (VisibleRange != null) cloneAxis.VisibleRange = (IRange)VisibleRange.Clone();
            if (GrowBy != null) cloneAxis.GrowBy = (IRange<double>)GrowBy.Clone();

            return cloneAxis;
        }
    }
}
