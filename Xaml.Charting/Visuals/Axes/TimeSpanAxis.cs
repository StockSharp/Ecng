// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimeSpanAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
    /// Provides a Linear, Value TimeSpan Axis, capable of rendering TimeSpan ticks on the X-Axis of a <see cref="UltrachartSurface"/>. 
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
    public class TimeSpanAxis : TimeSpanAxisBase
    {
        private static readonly List<Type> SupportedTypes = new List<Type>(new[]
            {
                typeof(TimeSpan)
            });

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpanAxis"/> class.
        /// </summary>
        /// <remarks></remarks>
        public TimeSpanAxis()
        {
            DefaultStyleKey = typeof(TimeSpanAxis);

            DefaultLabelProvider = new TimeSpanLabelProvider();

            this.SetCurrentValue(TickProviderProperty, new TimeSpanTickProvider());
        }

        /// <summary>
        /// Returns an undefined <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetUndefinedRange()
        {
            return new TimeSpanRange();
        }

        /// <summary>
        /// Coerce <seealso cref="IRange"/> if current range is zero range
        /// </summary>
        /// <param name="maximumRange">Current maximum range</param>
        protected override IRange CoerceZeroRange(IRange maximumRange)
        {
            return GetDefaultNonZeroRange().AsDoubleRange();
        }

        /// <summary>
        /// Returns an default non zero <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetDefaultNonZeroRange()
        {
            return new TimeSpanRange(TimeSpan.Zero, TimeSpan.FromSeconds(1.0));
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
            return new TimeSpanRange(min.ToTimeSpan(), max.ToTimeSpan());
        }

        /// <summary>
        /// Returns an instance of an 
        /// <see cref="IDeltaCalculator" /> which is used to compute the data-values of 
        /// <see cref="TimeSpanAxisBase.MajorDelta" />, 
        /// <see cref="TimeSpanAxisBase.MinorDelta" />. 
        /// Overridden by derived types to allow calculations specific to that axis type.
        /// </summary>
        /// <returns>
        /// An <see cref="IDeltaCalculator" /> instance
        /// </returns>
        protected override IDeltaCalculator GetDeltaCalculator()
        {
            return TimeSpanDeltaCalculator.Instance;
        }

        /// <summary>
        /// When overridden in a derived class, converts a tick value to a data value. For instance, this may be overridden in the
        /// <see cref="CategoryDateTimeAxis" /> to convert between indices and DateTimes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected override IComparable ConvertTickToDataValue(IComparable value)
        {
            return value.ToTimeSpan();
        }

        /// <summary>
        /// Checks whether <paramref name="range" /> is not Null and is of valid type for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool IsOfValidType(IRange range)
        {
            return range is TimeSpanRange;
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