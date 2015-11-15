// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CategoryDateTimeAxis.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Numerics.CoordinateProviders;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Ecng.Xaml.Licensing.Core;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a Linear, Category DateTime Axis, capable of rendering DateTime ticks on the X-Axis of a <see cref="UltrachartSurface"/>. 
    /// The CategoryDateTimeAxis is specifically used in stock-charts or financial charts, since the category-nature of the axis automatically
    /// collapses weekend gaps and overnight gaps in trading charts. 
    /// 
    /// Each data-point is treated as equidistant despite the X Data-value.
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
    public class CategoryDateTimeAxis : AxisBase, ICategoryAxis
    {
        
        /// <summary>
        /// Defines the BarTimeFrame DependencyProperty. A default value of -1 allows the <see cref="CategoryDateTimeAxis"/> to estimate the timeframe
        /// </summary>
        public static readonly DependencyProperty BarTimeFrameProperty = DependencyProperty.Register("BarTimeFrame", typeof (double), typeof (CategoryDateTimeAxis), new PropertyMetadata(-1.0));

        /// <summary>
        /// Defines the SubDayTextFormatting DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SubDayTextFormattingProperty = DependencyProperty.Register("SubDayTextFormatting", typeof(string), typeof(CategoryDateTimeAxis), new PropertyMetadata(null, InvalidateParent));


        private static readonly List<Type> SupportedTypes = new List<Type>(new[]
                                                                               {
                                                                                   typeof(DateTime)
                                                                               });

        private AxisParams _axisParams;
        private double _dataPointWidth = double.NaN;

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryDateTimeAxis"/> class.
        /// </summary>
        public CategoryDateTimeAxis()
        {
            DefaultStyleKey = typeof (CategoryDateTimeAxis);

            DefaultLabelProvider = new TradeChartAxisLabelProvider();

            this.SetCurrentValue(TickProviderProperty, new NumericTickProvider());
            this.SetCurrentValue(TickCoordinatesProviderProperty, new CategoryTickCoordinatesProvider());
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
        /// Gets or sets the Bar Time Frame in seconds. This is the number of seconds that each data-point represents on the <see cref="CategoryDateTimeAxis"/> and is required for proper rendering. 
        /// A default value of -1 allows the <see cref="CategoryDateTimeAxis"/> to estimate the timeframe
        /// </summary>
        public double BarTimeFrame
        {
            get { return (double) GetValue(BarTimeFrameProperty); }
            set { SetValue(BarTimeFrameProperty, value); }
        }

        /// <summary>
        /// Gets or sets the MinimalZoomConstrain of the Axis. This is used to set minimum distance between Min and Max of the VisibleRange 
        /// </summary>
        /// <value>The minimum distance between Min and Max of the VisibleRange</value>
        public new int? MinimalZoomConstrain
        {
            get { return (int?)GetValue(MinimalZoomConstrainProperty); }
            set { SetValue(MinimalZoomConstrainProperty, value); }
        }

        /// <summary>
        /// Gets the current data-point width, which is the width of one data-point in pixels on the category axis
        /// </summary>
        public override double CurrentDatapointPixelSize
        {
            get { return _dataPointWidth; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is category axis.
        /// </summary>
        public override bool IsCategoryAxis
        {
            get { return true; }
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
            throw new InvalidOperationException("CalculateYRange is only valid on Y-Axis types");
        }

        /// <summary>
        /// Gets the Maximum Range of the axis, which is equal to the DataRange including any GrowBy factor applied
        /// </summary>
        /// <returns></returns>
        public override IRange GetMaximumRange()
        {
            if (!IsXAxis)
                throw new InvalidOperationException("CategoryDateTimeAxis is only valid as an X-Axis");

            IRange currentVisibleRange = VisibleRange != null && VisibleRange.IsDefined
                              ? VisibleRange
                              : GetDefaultNonZeroRange();

            var dataRange = CalculateDataRange();
            if (dataRange != null)
            {
                currentVisibleRange = dataRange.GrowBy(GrowBy.Min, GrowBy.Max);

                if (VisibleRangeLimit != null)
                {
                    currentVisibleRange.ClipTo(VisibleRangeLimit, VisibleRangeLimitMode);
                }
            }
            
            return currentVisibleRange;
        }

        /// <summary>
        /// Calculates data range of current axis
        /// </summary>
        /// <returns></returns>
        protected override IRange CalculateDataRange()
        {
            return ParentSurface != null && !ParentSurface.RenderableSeries.IsNullOrEmpty() ? GetIndexRange() : null;
        }

        private IndexRange GetIndexRange()
        {
            IndexRange dataRange = null;

            var baseRSeries = ParentSurface.RenderableSeries.FirstOrDefault(x => x.XAxisId == Id);
            if (baseRSeries != null && baseRSeries.DataSeries != null && baseRSeries.DataSeries.Count > 0)
            {
                dataRange = new IndexRange(0, baseRSeries.DataSeries.XValues.Count - 1);
            }

            return dataRange;
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public override IAxis Clone()
        {
            var catAxis = new CategoryDateTimeAxis();
            if (VisibleRange != null) catAxis.VisibleRange = (IRange)VisibleRange.Clone();
            if (GrowBy != null) catAxis.GrowBy = (IRange<double>)GrowBy.Clone();
            catAxis.BarTimeFrame = -1;
            return catAxis;
        }

        /// <summary>
        /// Calcuates the delta's for use in this render pass
        /// </summary>
        protected override void CalculateDelta()
        {
            var tickCalculator = GetDeltaCalculator();

            var maxAutoTicks = GetMaxAutoTicks();

            // Compute delta's in bar number (point number) from VisibleRange which is an IndexRange
            var delta = tickCalculator.GetDeltaFromRange(
                VisibleRange.Min,
                VisibleRange.Max,
                MinorsPerMajor,
                maxAutoTicks);

            MajorDelta = delta.MajorDelta;
            MinorDelta = delta.MinorDelta;
        }

        /// <summary>
        /// Returns an instance of an <see cref="IDeltaCalculator"/> which is used to compute the data-values of <see cref="AxisBase.MajorDelta"/>, <see cref="AxisBase.MinorDelta"/>. 
        /// Overridden by derived types to allow calculations specific to that axis type.
        /// </summary>
        /// <returns>An <see cref="IDeltaCalculator"/> instance</returns>
        protected override IDeltaCalculator GetDeltaCalculator()
        {
            return NumericDeltaCalculator.Instance;
        }

        /// <summary>
        /// Given the Data Value, returns the x or y pixel coordinate at that value on the Axis. This operation is the opposite of <see cref="AxisBase.HitTest(Point)" />
        /// </summary>
        /// <param name="value">The DataValue as input</param>
        /// <returns>
        /// The pixel coordinate on this Axis corresponding to the input DataValue
        /// </returns>
        /// <example>
        /// Given an axis with a VisibleRange of 1..10 and height of 100, a value of 7 passed in to GetCoordinate would return 70 pixels
        ///   </example>
        /// <remarks>
        /// If the Axis is an XAxis, the coordinate returned is an X-pixel. If the axis is a Y Axis, the coordinate returned is a Y-pixel
        /// </remarks>
        public override double GetCoordinate(IComparable value)
        {
            var coordCalc = GetCurrentCoordinateCalculator();
            if (coordCalc == null)
                return double.NaN;

            var catCoordCalc = coordCalc as ICategoryCoordinateCalculator;
            if (catCoordCalc != null && value is DateTime)
            {
                value = catCoordCalc.TransformDataToIndex((DateTime)value);
            }
            return _currentCoordinateCalculator.GetCoordinate(value.ToDouble());
        }

        /// <summary>
        /// Transforms a pixel coordinate into a data value for this axis.
        /// </summary>
        /// <param name="pixelCoordinate"></param>
        /// <returns></returns>
        public override IComparable GetDataValue(double pixelCoordinate)
        {
            if (_currentCoordinateCalculator == null)
                return int.MaxValue;

            // Returns index of dataPoint, need to transform it to dataValue
            var value = _currentCoordinateCalculator.GetDataValue(pixelCoordinate);

            var categoryCoordinateCalculator = GetCurrentCoordinateCalculator() as ICategoryCoordinateCalculator;
            var dataValue = categoryCoordinateCalculator != null
                                        ? categoryCoordinateCalculator.TransformIndexToData((int)value)
                                        : value.ToDateTime();

            return dataValue;
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
        /// Checks whether <paramref name="range" /> is of valid type for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public override bool IsOfValidType(IRange range)
        {
            var indexRange = range as IndexRange;

            var isValid = indexRange != null;

            return isValid;
        }

        /// <summary>
        /// Returns an undefined <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetUndefinedRange()
        {
            return new IndexRange(0, int.MaxValue);
        }

        /// <summary>
        /// Returns an default non zero <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public override IRange GetDefaultNonZeroRange()
        {
            return new IndexRange(0, 10);
        }

        /// <summary>
        /// Gets an <see cref="AxisParams"/> struct with info about the current axis setup
        /// </summary>
        /// <returns></returns>
        public override AxisParams GetAxisParams()
        {
            return _axisParams;
        }

        /// <summary>
        /// Called at the start of a render pass, passing in the root <see cref="IPointSeries"/> which will define the categories
        /// </summary>
        /// <param name="firstPointSeries">the root <see cref="IPointSeries"/> which will define the categories</param>
        public override void OnBeginRenderPass(RenderPassInfo renderPassInfo = default(RenderPassInfo), IPointSeries firstPointSeries = null)
        {
            _axisParams = base.GetAxisParams();

            if (firstPointSeries != null)
            {
                ComputeAxisParams(firstPointSeries);
            }
            else
            {
                _axisParams.IsCategoryAxis = false;
                _axisParams.CategoryPointSeries = null;
            }

            base.OnBeginRenderPass(renderPassInfo, firstPointSeries);
        }

        public override void ScrollByDataPoints(int pointAmount, TimeSpan duration)
        {
            var currentRange = VisibleRange as IndexRange;
            if (currentRange == null) return;

            var newRange = new IndexRange(currentRange.Min + pointAmount, currentRange.Max + pointAmount);
            TryApplyVisibleRangeLimit(newRange);

            this.TrySetOrAnimateVisibleRange(newRange, duration);
        }

        private void ComputeAxisParams(IPointSeries firstPointSeries)
        {
            _axisParams.IsCategoryAxis = true;
            _axisParams.CategoryPointSeries = firstPointSeries;

            var visibleRange = VisibleRange;
            int min = visibleRange != null ? Math.Max((int) visibleRange.Min, 0) : 0;
            int max = visibleRange != null ? Math.Max(min, (int) visibleRange.Max) : 0;
            
            _axisParams.PointRange = new IndexRange(min, max);

            _axisParams.DataPointStep = GetBarTimeFrame();

            _dataPointWidth = ComputeDataPointWidth(
                (IndexRange)visibleRange,
                _axisParams.Size);

            _axisParams.DataPointPixelSize = _dataPointWidth;
            _axisParams.Size -= _dataPointWidth;
        }

        internal double GetBarTimeFrame()
        {
            var baseXValues = _axisParams.BaseXValues;

            var currentBarTimeFrame = (double)TimeSpan.FromSeconds(BarTimeFrame).Ticks;

            if (BarTimeFrame <= 0.0)
            {
                var defaultBarTimeFrame = (double)TimeSpan.FromSeconds(60).Ticks;

                if (baseXValues != null && baseXValues.Count >= 2)
                {
                    var lastIndex = baseXValues.Count - 1;
                    currentBarTimeFrame = (((DateTime)baseXValues[lastIndex]).ToDouble() - ((DateTime)baseXValues[0]).ToDouble()) / lastIndex;
                }

                // BarTimeFrame must be greater then 0
                currentBarTimeFrame = currentBarTimeFrame > 0d ? currentBarTimeFrame : defaultBarTimeFrame;
            }

            return currentBarTimeFrame;
        }

        internal static double ComputeDataPointWidth(IndexRange visibleRange, double size)
        {
            if (visibleRange == null) return 1;

            var totalBars = visibleRange.Max - visibleRange.Min + 1;
            return size/totalBars;
        }

        /// <summary>
        /// Converts the CategoryDateTimeAxis's <see cref="AxisBase.VisibleRange"/> of type <see cref="IndexRange"/> to a <see cref="DateRange"/> of concrete date-values.
        /// Note: If either index is outside of the range of data on the axis, the date values will be inteporlated.
        /// </summary>
        /// <param name="visibleRange">The input <see cref="IndexRange"/></param>
        /// <returns>The <see cref="DateRange"/> with transformed dates that correspond to input indices</returns>
        public DateRange ToDateRange(IndexRange visibleRange)
        {
            Guard.NotNull(visibleRange, "visibleRange");

            DateRange dateRange = null;

            var coordCalc = GetCurrentCoordinateCalculator() as ICategoryCoordinateCalculator;
            if (coordCalc != null)
            {
                dateRange = new DateRange(
                    coordCalc.TransformIndexToData(visibleRange.Min).ToDateTime(),
                    coordCalc.TransformIndexToData(visibleRange.Max).ToDateTime());
            }

            return dateRange;
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