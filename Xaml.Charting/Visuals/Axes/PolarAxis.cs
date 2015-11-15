using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Ecng.Xaml.Charting.Numerics.CoordinateProviders;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a Polar, Value Numeric Axis, capable of rendering double, int, short, byte, long ticks on the XAxis of a <see cref="UltrachartSurface"/>. 
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
    /// <seealso cref="PolarXAxis"/>
    /// <seealso cref="PolarYAxis"/>
    /// <seealso cref="NumericAxis"/>
    /// <seealso cref="LogarithmicNumericAxis"/>
    /// <seealso cref="DateTimeAxis"/>
    /// <seealso cref="CategoryDateTimeAxis"/>
    /// <seealso cref="TimeSpanAxis"/>
    public class PolarXAxis: NumericAxis
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PolarXAxis"/> class.
        /// </summary>
        public PolarXAxis()
        {
            DefaultStyleKey = typeof(PolarXAxis);

            TickCoordinatesProvider = new PolarTickCoordinatesProvider();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a polar axis.
        /// </summary>
        public override bool IsPolarAxis
        {
            get { return true; }
        }

        /// <summary>
        /// Gets whether the current axis is horizontal or not
        /// </summary>
        public override bool IsHorizontalAxis
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="System.Windows.FrameworkElement.OnApplyTemplate()"/>.
        /// </summary>
        /// <remarks></remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            AxisContainer.SizeChanged +=
                (sender, args) =>
                    PolarPanel.SetThickness(AxisContainer,
                        AxisAlignment == AxisAlignment.Top || AxisAlignment == AxisAlignment.Bottom
                            ? AxisContainer.ActualHeight
                            : AxisContainer.ActualWidth);
        }

        /// <summary>
        /// Gets an <see cref="AxisParams"/> struct with info about the current axis setup
        /// </summary>
        /// <returns></returns>
        public override AxisParams GetAxisParams()
        {
            var axisParams = base.GetAxisParams();

            axisParams.IsPolarAxis = IsPolarAxis;
            
            return axisParams;
        }

        protected override double GetOffsetForLabels()
        {
            return 0d;
        }

        /// <summary>
        /// Draws grid lines on chart at specified coordinates
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="gridLineStyle"></param>
        /// <param name="coordsToDraw"></param>
        protected override void DrawGridLine(IRenderContext2D renderContext, Style gridLineStyle, IEnumerable<float> coordsToDraw)
        {
            LineToStyle.Style = gridLineStyle;
            ThemeManager.SetTheme(LineToStyle, ThemeManager.GetTheme(this));

            using (var linePen = renderContext.GetStyledPen(LineToStyle, true))
            {
                if (IsXAxis)
                {
                    var arr = coordsToDraw.ToArray();

                    var transformationStrategy = Services.GetService<IStrategyManager>().GetTransformationStrategy();
                    
                    var radius = PolarUtil.CalculateViewportRadius(transformationStrategy.ViewportSize);
                    var center = transformationStrategy.ReverseTransform(new Point(0, 0));
                    
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var pt = transformationStrategy.ReverseTransform(new Point(arr[i], radius));

                        renderContext.DrawLine(linePen, center, pt);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the offset of the Axis
        /// </summary>
        /// <returns></returns>
        public override double GetAxisOffset()
        {
            return 0;
        }

        /// <summary>
        /// Get coordinates to place tick label
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="coords"></param>
        /// <returns></returns>
        protected override Point GetLabelPosition(float offset, float coords)
        {
            return new Point(coords, offset);
        }

        /// <summary>
        /// Performs a HitTest operation on the <see cref="AxisBase" />. The supplied <paramref name="dataValue" /> is used to convert to <see cref="AxisInfo" /> struct, which contains information about the axis, as well as formatted values
        /// </summary>
        /// <param name="dataValue">The data value.</param>
        /// <returns>The <see cref="AxisInfo"/> result</returns>
        public override AxisInfo HitTest(IComparable dataValue)
        {
            var axisInfo = base.HitTest(dataValue);

            axisInfo.AxisAlignment = AxisAlignment.Top;
            axisInfo.IsHorizontal = true;
            
            return axisInfo;
        }
    }

    /// <summary>
    /// Provides a Polar, Value Numeric Axis, capable of rendering double, int, short, byte, long ticks on the YAxis of a <see cref="UltrachartSurface"/>. 
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
    /// <seealso cref="PolarXAxis"/>
    /// <seealso cref="PolarYAxis"/>
    /// <seealso cref="NumericAxis"/>
    /// <seealso cref="LogarithmicNumericAxis"/>
    /// <seealso cref="DateTimeAxis"/>
    /// <seealso cref="CategoryDateTimeAxis"/>
    /// <seealso cref="TimeSpanAxis"/>
    public class PolarYAxis : NumericAxis
    {
        /// <summary>
        /// Defines the Angle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof (double), typeof (PolarYAxis), new PropertyMetadata(0d));

        /// <summary>
        /// Initializes a new instance of the <see cref="PolarYAxis"/> class.
        /// </summary>
        public PolarYAxis()
        {
            DefaultStyleKey = typeof(PolarYAxis);
        }

        /// <summary>
        /// Gets or set rotation angle for this axis
        /// </summary>
        public double Angle
        {
            get { return (double) GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a polar axis.
        /// </summary>
        public override bool IsPolarAxis
        {
            get { return true; }
        }

        /// <summary>
        /// Draws grid lines on chart at specified coordinates
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="gridLineStyle"></param>
        /// <param name="coordsToDraw"></param>
        protected override void DrawGridLine(IRenderContext2D renderContext, Style gridLineStyle, IEnumerable<float> coordsToDraw)
        {
            LineToStyle.Style = gridLineStyle;
            ThemeManager.SetTheme(LineToStyle, ThemeManager.GetTheme(this));

            using (var linePen = renderContext.GetStyledPen(LineToStyle))
            {
                if (!IsXAxis)
                {
                    var helper = Services.GetService<IStrategyManager>().GetTransformationStrategy();
                    var center = helper.ReverseTransform(new Point(0, 0));
                    
                    var transpBrush = new SolidColorBrush();
                    using (var br = renderContext.CreateBrush(transpBrush))
                    {
                        foreach (var radius in coordsToDraw)
                        {
                            renderContext.DrawEllipse(linePen, br, center, radius*2, radius*2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the offset of the Axis
        /// </summary>
        /// <returns></returns>
        public override double GetAxisOffset()
        {
            return 0;
        }

        /// <summary>
        /// Gets an <see cref="AxisParams"/> struct with info about the current axis setup
        /// </summary>
        /// <returns></returns>
        public override AxisParams GetAxisParams()
        {
            var axisParams = base.GetAxisParams();

            axisParams.IsPolarAxis = IsPolarAxis;

            var axisSize = IsHorizontalAxis ? ActualWidth : ActualHeight;
            if (Math.Abs(axisSize) < double.Epsilon)
            {
                axisParams.Size /= 2;
            }

            return axisParams;
        }
    }
}
