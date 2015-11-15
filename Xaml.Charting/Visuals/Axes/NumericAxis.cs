// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// NumericAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a Linear, Value Numeric Axis, capable of rendering double, int, short, byte, long ticks on the X or Y-Axis of a <see cref="UltrachartSurface"/>. 
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
    public class NumericAxis : AxisBase
    {
        private AxisParams _axisParams;

        private static readonly List<Type> SupportedTypes = new List<Type>(new[]
        {
            typeof (int),
            typeof (double),
            typeof (decimal), // TODO: Move decimal, long out of here to avoid precision issues?
            typeof (long),
            typeof (float), 
            typeof (short),
            typeof (byte),
            typeof (uint),
            typeof (ushort),
            typeof (sbyte),
        });

        /// <summary>
        /// Defines the ScientificNotation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ScientificNotationProperty =
            DependencyProperty.Register("ScientificNotation", typeof (ScientificNotation), typeof (NumericAxis),
                                        new PropertyMetadata(ScientificNotation.None, InvalidateParent));

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericAxis"/> class.
        /// </summary>
        /// <remarks></remarks>
        public NumericAxis()
        {
            DefaultStyleKey = typeof (NumericAxis);

            DefaultLabelProvider = new NumericLabelProvider();

            this.SetCurrentValue(TickProviderProperty, new NumericTickProvider());
        }

        /// <summary>
        /// Gets or sets the major delta.
        /// </summary>
        /// <value>The major delta.</value>
        /// <remarks></remarks>
        public new double MajorDelta
        {
            get { return (double) GetValue(MajorDeltaProperty); }
            set { SetValue(MajorDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minor delta.
        /// </summary>
        /// <value>The minor delta.</value>
        /// <remarks></remarks>
        public new double MinorDelta
        {
            get { return (double) GetValue(MinorDeltaProperty); }
            set { SetValue(MinorDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the MinimalZoomConstrain of the Axis. This is used to set minimum distance between Min and Max of the VisibleRange 
        /// </summary>
        /// <value>The minimum distance between Min and Max of the VisibleRange</value>
        public new double? MinimalZoomConstrain
        {
            get { return (double?)GetValue(MinimalZoomConstrainProperty); }
            set { SetValue(MinimalZoomConstrainProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets used number format
        /// </summary>
        public ScientificNotation ScientificNotation
        {
            get { return (ScientificNotation) GetValue(ScientificNotationProperty); }
            set { SetValue(ScientificNotationProperty, value); }
        }

        /// <summary>
        /// Returns an instance of an <see cref="IDeltaCalculator"/> which is used to compute the data-values of Axis Gridlines, Ticks and Labels. 
        /// When overridden in a derived class (e.g. <see cref="LogarithmicNumericAxis"/>, the implementation of GetTickCalculator() changes to 
        /// allow calculations specific to that axis type
        /// </summary>
        /// <returns>An <see cref="IDeltaCalculator"/> instance</returns>
        protected override IDeltaCalculator GetDeltaCalculator()
        {
            return NumericDeltaCalculator.Instance;
        }

        /// <summary>
        /// Calcuates the delta's for use in this render pass
        /// </summary>
        /// <remarks></remarks>
        protected override void CalculateDelta()
        {
            var visibleRange = (IRange<double>)VisibleRange;

            if (AutoTicks)
            {
                var maxAutoTicks = GetMaxAutoTicks();

                var deltaCalculator = GetDeltaCalculator();
                var delta = (IAxisDelta<double>)deltaCalculator.GetDeltaFromRange(visibleRange.Min, visibleRange.Max, MinorsPerMajor, maxAutoTicks);

                MajorDelta = delta.MajorDelta;
                MinorDelta = delta.MinorDelta;

                UltrachartDebugLogger.Instance.WriteLine(
                    "CalculateDelta: Min={0}, Max={1}, Major={2}, Minor={3}, MaxAutoTicks={4}",
                    visibleRange.Min,
                    visibleRange.Max,
                    delta.MajorDelta,
                    delta.MinorDelta,
                    maxAutoTicks);
            }
        }

        /// <summary>
        /// Gets the aligned VisibleRange of the axis, with optional ZoomToFit flag.
        /// If ZoomToFit is true, it will return the DataRange plus any GrowBy applied to the axis
        /// </summary>
        /// <param name="renderPassInfo">Struct containing data for the current render pass</param>
        /// <returns>
        /// The VisibleRange of the axis
        /// </returns>
        public override IRange CalculateYRange(RenderPassInfo renderPassInfo)
        {
            if (IsXAxis) throw new InvalidOperationException("CalculateYRange is only valid on Y-Axis types");

            double max = double.MinValue;
            double min = double.MaxValue;

            var ranges = new Dictionary<string, DoubleRange>();

            int numberSeries = renderPassInfo.PointSeries.Length;
            for (int i = 0; i < numberSeries; i++)
            {
                var rSeries = renderPassInfo.RenderableSeries[i];
                var pSeries = renderPassInfo.PointSeries[i];

                if (rSeries == null || pSeries == null || rSeries.YAxisId != Id)
                    continue;

                DoubleRange pRange;

                // Treat OHLC series specially, if there are less than 1000 candles in view
                // use the high, low of this series not the close. This avoids YAxis
                // flickering as the latest candle ticks
                var dSeries = renderPassInfo.DataSeries[i];
                var indexRange = renderPassInfo.IndicesRanges[i];
                if (dSeries.DataSeriesType == DataSeriesType.Ohlc && indexRange.Diff.CompareTo(1000) < 0)
                {
                    // Get the lowest/highest in the range of latest values to latest - 1000.
                    pRange = dSeries.GetWindowedYRange(new IndexRange(indexRange.Min, indexRange.Max)).AsDoubleRange();
                }
                else
                {
                    pRange = pSeries.GetYRange();
                }

                string key = string.Empty;
                if (rSeries is IStackedRenderableSeries)
                {
                    var series = rSeries as IStackedRenderableSeries;

                    key = series.StackedGroupId;

                    pRange = (DoubleRange) series.GetYRange(renderPassInfo.IndicesRanges[i], IsLogarithmicAxis);

                    if (ranges.ContainsKey(key))
                    {
                        var tempRange = ranges[series.StackedGroupId];
                        ranges[series.StackedGroupId] = (DoubleRange) pRange.Union(tempRange);
                    }
                }
                else
                {
                    min = min < pRange.Min ? min : pRange.Min;
                    max = max > pRange.Max ? max : pRange.Max;
                }

                if (!ranges.ContainsKey(key))
                {
                    ranges.Add(key, pRange);
                }
            }

            foreach (var range in ranges)
            {
                min = min < range.Value.Min ? min : range.Value.Min;
                max = max > range.Value.Max ? max : range.Value.Max;
            }

            var visibleRange = RangeFactory.NewRange(min, max);

            var logBase = IsLogarithmicAxis ? ((ILogarithmicAxis) this).LogarithmicBase : 0;
            return GrowBy != null ? visibleRange.GrowBy(GrowBy.Min, GrowBy.Max, IsLogarithmicAxis, logBase) : visibleRange;
        }

        /// <summary>
        /// Returns an undefined <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override IRange GetUndefinedRange()
        {
            return new DoubleRange(double.NaN, double.NaN);
        }

        /// <summary>
        /// Returns an default non zero <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetDefaultNonZeroRange()
        {
            return new DoubleRange(0.0, 10.0);
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public override IAxis Clone()
        {
            var numericAxis = (IAxis)Activator.CreateInstance(GetType());

            if (VisibleRange != null) numericAxis.VisibleRange = (IRange) VisibleRange.Clone();
            if (GrowBy != null) numericAxis.GrowBy = (IRange<double>) GrowBy.Clone();

            return numericAxis;
        }

        /// <summary>
        /// Checks whether <paramref name="range" /> is of valid type for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool IsOfValidType(IRange range)
        {
            var doubleRange = range as DoubleRange;

            var isValid = doubleRange != null;

            return isValid;
        }

        /// <summary>
        /// Returns a list of types which current axis is designed to work with
        /// </summary>
        /// <returns></returns>
        protected override List<Type> GetSupportedTypes()
        {
            return SupportedTypes;
        }

        /// <summary>
        /// Gets an <see cref="AxisParams"/> struct with info about the current axis setup
        /// </summary>
        /// <returns></returns>
        public override AxisParams GetAxisParams()
        {
            return _axisParams;
        }

        public override void OnBeginRenderPass(RenderPassInfo renderPassInfo = default(RenderPassInfo), IPointSeries firstPointSeries = null) {
            _axisParams = base.GetAxisParams();

            var rser = renderPassInfo.RenderableSeries.OfType<TimeframeSegmentRenderableSeries>().FirstOrDefault();

            if(firstPointSeries != null && rser != null) {
                var step = rser.PriceScale;
                var range = (IRange<double>)VisibleRange;

                if(step > 0) {
                    _axisParams.DataPointStep = step;
                    _axisParams.DataPointPixelSize = renderPassInfo.ViewportSize.Height / ((range.Max - range.Min) / step);
                }
            }

            base.OnBeginRenderPass(renderPassInfo, firstPointSeries);
        }

        public override double CurrentDatapointPixelSize {get {return _axisParams.DataPointPixelSize;}}

        protected override IRange CoerceZeroRange(IRange maximumRange) {
            var dr = maximumRange as DoubleRange;

            if(_axisParams.DataPointStep <= 0 || dr == null)
                return base.CoerceZeroRange(maximumRange);

            return new DoubleRange(dr.Min - _axisParams.DataPointStep, dr.Min + _axisParams.DataPointStep);
        }

        /// <summary>
        /// Transforms a pixel coordinate into a data value for this axis. 
        /// </summary>
        /// <param name="pixelCoordinate"></param>
        /// <returns></returns>
        public override IComparable GetDataValue(double pixelCoordinate) {
            var val = base.GetDataValue(pixelCoordinate);

            if(!(_axisParams.DataPointStep > 0))
                return val;

            if(val is double) {
                var d = (double)val;

                if(d.IsNaN()) return d;

                return d.Round(_axisParams.DataPointStep);
            }

            return val;
        }
    }
}