// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DateTimeAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a Linear, Value DateTime Axis, capable of rendering DateTime ticks on the X-Axis of a <see cref="UltrachartSurface"/>. 
    /// </summary>
    /// <remarks>
    /// <para>All <see cref="AxisBase"/> derived types have a <see cref="AxisBase.TextFormatting"/> property to define axis text labels, however a more advanced
    /// way of defining axis label text is via the <see cref="AxisBase.LabelProvider"/> property - expecting a custom <see cref="ILabelProvider"/> derived type. </para>
    /// <para>In order to separately format cursor labels please see the <see cref="AxisBase.CursorTextFormatting"/> or again implement a customer <see cref="ILabelProvider"/>. </para>
    /// <para>All axis types have many properties to define how they operate. These include <see cref="AxisBase.DrawMajorGridLines"/>, <see cref="AxisBase.DrawMinorGridLines"/>, 
    /// <see cref="AxisBase.DrawMajorTicks"/>, <see cref="AxisBase.DrawMinorTicks"/>, <see cref="AxisBase.DrawMajorBands"/>, <see cref="AxisBase.DrawLabels"/>. </para>
    /// <para>Finally, all axis components can be styled. Please see the examples suite, the XAML Styling example to see how to use XAML to style axis elements.</para>
    /// </remarks>
    /// <seealso cref="AxisBase"/>
    /// <seealso cref="IAxis"/>
    /// <seealso cref="NumericAxis"/>
    /// <seealso cref="LogarithmicNumericAxis"/>
    /// <seealso cref="DateTimeAxis"/>
    /// <seealso cref="CategoryDateTimeAxis"/>
    /// <seealso cref="TimeSpanAxis"/>
    [UltrachartLicenseProvider(typeof(AxisUltrachartLicenseProvider))]
    public class DateTimeAxis : TimeSpanAxisBase
    {
        private static readonly List<Type> SupportedTypes = new List<Type>(new[]
                                                                               {
                                                                                   typeof(DateTime)
                                                                               });

       /// <summary>
        /// Defines the SubDayTextFormatting DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SubDayTextFormattingProperty = DependencyProperty.Register("SubDayTextFormatting", typeof(string), typeof(DateTimeAxis), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeAxis"/> class.
        /// </summary>
        /// <remarks></remarks>
        public DateTimeAxis()
        {
            DefaultStyleKey = typeof(DateTimeAxis);

            DefaultLabelProvider = new DateTimeLabelProvider();

            this.SetCurrentValue(TickProviderProperty, new DateTimeTickProvider());
        }

        /// <summary>
        /// Gets or sets the Text Formatting String used for Axis Tick Labels when the range of the axis is sub-day
        /// </summary>
        /// <value>The text formatting.</value>
        /// <remarks></remarks>
        public string SubDayTextFormatting
        {
            get { return (string)GetValue(SubDayTextFormattingProperty); }
            set { SetValue(SubDayTextFormattingProperty, value); }
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

            double max = DateTime.MinValue.ToDouble();
            double min = DateTime.MaxValue.ToDouble();

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

            var visibleRange = new DateRange(new DateTime(Math.Min((long)min, DateTime.MaxValue.Ticks)), new DateTime(Math.Max((long)max, DateTime.MinValue.Ticks)));
            return visibleRange.GrowBy(GrowBy.Min, GrowBy.Max);
        }

        /// <summary>
        /// Gets the Maximum Range of the axis, which is equal to the DataRange including any GrowBy factor applied
        /// </summary>
        /// <returns></returns>
        public override IRange GetMaximumRange()
        {
            var maximumRange = base.GetMaximumRange();

            return new DateRange(maximumRange.Min.ToDateTime(), maximumRange.Max.ToDateTime());
        }

        /// <summary>
        /// Coerce <seealso cref="IRange"/> if current range is zero range
        /// </summary>
        /// <param name="maximumRange">Current maximum range</param>
        /// <returns></returns>
        protected override IRange CoerceZeroRange(IRange maximumRange)
        {
            var dayTicks = TimeSpan.FromDays(1).Ticks;

            return RangeFactory.NewRange(maximumRange.Min.ToDouble() - dayTicks,
                maximumRange.Max.ToDouble() + dayTicks);
        }

        /// <summary>
        /// Asserts the type passed in is supported by the current axis implementation
        /// </summary>
        /// <param name="dataType"></param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public override void AssertDataType(Type dataType)
        {
            if (!SupportedTypes.Contains(dataType))
            {
                throw new InvalidOperationException(string.Format("DateTimeAxis does not support the type {0}. Supported types include {1}",
                    dataType, string.Join(", ", SupportedTypes.Select(x => x.Name).ToArray())));
            }
        }

        /// <summary>
        /// Returns an undefined <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetUndefinedRange()
        {
            return new DateRange();
        }

        /// <summary>
        /// Returns an default non zero <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetDefaultNonZeroRange()
        {
            var now = DateTime.UtcNow.Date;
            return new DateRange(now.AddDays(-1), now.AddDays(1));
        }

        /// <summary>
        /// When overriden in a derived class, converts a Min Max <see cref="IComparable" /> value into an <see cref="IRange" /> of the correct type for this axis
        /// </summary>
        /// <param name="min">The min value</param>
        /// <param name="max">The max value</param>
        /// <returns>
        /// The <see cref="IRange" /> instance
        /// </returns>
        protected override IRange ToVisibleRange(IComparable min, IComparable max)
        {
            return new DateRange(min.ToDateTime(), max.ToDateTime());
        }

        /// <summary>
        /// Returns an instance of an <see cref="IDeltaCalculator"/> which is used to compute the data-values of <see cref="TimeSpanAxisBase.MajorDelta"/>, <see cref="TimeSpanAxisBase.MinorDelta"/>. 
        /// Overridden by derived types to allow calculations specific to that axis type.
        /// </summary>
        /// <returns>An <see cref="IDeltaCalculator"/> instance</returns>
        protected override IDeltaCalculator GetDeltaCalculator()
        {
            return DateTimeDeltaCalculator.Instance;
        }

        /// <summary>
        /// When overridden in a derived class, converts a tick value to a data value. For instance, this may be overridden in the
        /// <see cref="CategoryDateTimeAxis" /> to convert between indices and DateTimes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override IComparable ConvertTickToDataValue(IComparable value)
        {
            return value.ToDateTime();
        }

        /// <summary>
        /// Checks whether <paramref name="range" /> is not Null and is of valid type for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool IsOfValidType(IRange range)
        {
            return range is DateRange;
        }

        /// <summary>
        /// Returns a list of types which current axis is designed to work with
        /// </summary>
        /// <returns></returns>
        protected override List<Type> GetSupportedTypes()
        {
            return SupportedTypes;
        }
    }
}