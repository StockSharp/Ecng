// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
// 
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LogarithmicNumericAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Visuals.Axes.LabelProviders;
using Ecng.Xaml.Charting.Visuals.Axes.LogarithmicAxis;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a Logarithmic, Value Numeric Axis, capable of rendering double, int, short, byte, long ticks on the X or Y-Axis of a <see cref="UltrachartSurface"/>. 
    /// The <see cref="LogarithmicBase"/> property determines which base is used for the logarithm.
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
    public class LogarithmicNumericAxis : NumericAxis, ILogarithmicAxis
    {
        /// <summary>
        /// Defines the LogarithmicBase DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LogarithmicBaseProperty =
            DependencyProperty.Register("LogarithmicBase", typeof(double), typeof(LogarithmicNumericAxis), new PropertyMetadata(10d, OnLogarithmicBaseChanged));

        private static void OnLogarithmicBaseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as LogarithmicNumericAxis;
            if (axis != null)
            {
                if (axis.LogarithmicBase <= 0)
                {
                    throw new InvalidOperationException(String.Format("The value {0} is not a valid base for the LogarithmicNumericAxis.", axis.LogarithmicBase));
                }

                InvalidateParent(d, e);
            }
        }

        /// <summary>
        /// Gets or sets the value which determines the base used for the logarithm.
        /// </summary>
        [TypeConverter(typeof(LogarithmicBaseConverter))]
        public double LogarithmicBase
        {
            get { return (double)GetValue(LogarithmicBaseProperty); }
            set { SetValue(LogarithmicBaseProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogarithmicNumericAxis"/> class.
        /// </summary>
        public LogarithmicNumericAxis()
        {
            LabelProvider = new LogarithmicNumericLabelProvider();

            this.SetCurrentValue(TickProviderProperty, new LogarithmicNumericTickProvider());
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a logarithmic axis.
        /// </summary>
        public override bool IsLogarithmicAxis
        {
            get { return true; }
        }

        /// <summary>
        /// Gets an <see cref="AxisParams" /> struct with info about the current axis setup
        /// </summary>
        /// <returns></returns>
        public override AxisParams GetAxisParams()
        {
            var axisParams = base.GetAxisParams();

            axisParams.IsLogarithmicAxis = true;
            axisParams.LogarithmicBase = LogarithmicBase;

            return axisParams;
        }

        /// <summary>
        /// Checks whether <paramref name="range" /> is valid visible range for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool IsValidRange(IRange range)
        {
            var isValid = base.IsValidRange(range) &&
                          Math.Sign(range.Min.ToDouble()) == Math.Sign(range.Max.ToDouble());

            return isValid;
        }

        /// <summary>
        /// Returns an instance of an <see cref="IDeltaCalculator" /> which is used to compute the data-values of Axis Gridlines, Ticks and Labels.
        /// When overridden in a derived class (e.g. <see cref="LogarithmicNumericAxis" />, the implementation of GetTickCalculator() changes to
        /// allow calculations specific to that axis type
        /// </summary>
        /// <returns>
        /// An <see cref="IDeltaCalculator" /> instance
        /// </returns>
        protected override IDeltaCalculator GetDeltaCalculator()
        {
            var logCalc = (LogarithmicDeltaCalculator)LogarithmicDeltaCalculator.Instance;
            logCalc.LogarithmicBase = LogarithmicBase;

            return logCalc;
        }

        protected override Numerics.TickCoordinateProviders.TickCoordinates CalculateTicks()
        {
            var logTickProvider = TickProvider as LogarithmicNumericTickProvider;

            if (logTickProvider != null)
            {
                logTickProvider.LogarithmicBase = LogarithmicBase;
            }

            return base.CalculateTicks();
        }

        /// <summary>
        /// Returns an default non zero <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetDefaultNonZeroRange()
        {
            return new DoubleRange(Math.Pow(LogarithmicBase, -1), Math.Pow(LogarithmicBase, 2));
        }
    }
}
