// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisBaseCommon.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Numerics.CoordinateProviders;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// A common Base Class with DependencyProperties and method for 2D and 3D Axis types. 
    /// </summary>
    public abstract class AxisBaseCommon : ContentControl, ISuspendable, IInvalidatableElement, INotifyPropertyChanged
    {
        /// <summary>
        /// Defines the TickCoordinatesProvider DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TickCoordinatesProviderProperty = DependencyProperty.Register("TickCoordinatesProvider", typeof(ITickCoordinatesProvider), typeof(AxisBase), new PropertyMetadata(OnTickCoordinatesProviderChanged));

        /// <summary>
        /// Defines the IsStaticAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsStaticAxisProperty = DependencyProperty.Register("IsStaticAxis", typeof(bool), typeof(AxisBase), new PropertyMetadata(false, AxisBase.OnIsStaticAxisChanged));

        /// <summary>
        /// Defines the IsPrimaryAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsPrimaryAxisProperty = DependencyProperty.Register("IsPrimaryAxis", typeof(bool), typeof(AxisBase), new PropertyMetadata (false, AxisBase.OnIsPrimaryAxisChanged));

        /// <summary>
        /// Defines the AutoRange DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AutoRangeProperty = DependencyProperty.Register("AutoRange", typeof(AutoRange), typeof(AxisBase), new PropertyMetadata(AutoRange.Once, InvalidateParent));

        /// <summary>
        /// Defines the MajorDelta DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MajorDeltaProperty = DependencyProperty.Register("MajorDelta", typeof(IComparable), typeof(AxisBase), new PropertyMetadata(default(IComparable), InvalidateParent));

        /// <summary>
        /// Defines the MinorDelta DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinorDeltaProperty = DependencyProperty.Register("MinorDelta", typeof(IComparable), typeof(AxisBase), new PropertyMetadata(default(IComparable), InvalidateParent));

        /// <summary>
        /// Defines the MinorDelta DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinorsPerMajorProperty = DependencyProperty.Register("MinorsPerMajor", typeof(int), typeof(AxisBase), new PropertyMetadata(5, InvalidateParent));

        /// <summary>
        /// Defines the GrowBy DependencyProperty
        /// </summary>
        public static readonly DependencyProperty GrowByProperty = DependencyProperty.Register("GrowBy", typeof(IRange<double>), typeof(AxisBase), new PropertyMetadata(default(IRange), InvalidateParent));

        /// <summary>
        /// Defines the VisibleRange DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VisibleRangeProperty = DependencyProperty.Register("VisibleRange", typeof(IRange), typeof(AxisBase), new PropertyMetadata(default(IRange), OnVisibleRangeDependencyPropertyChanged));

        /// <summary>
        /// Defines the VisibleRangeLimit DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VisibleRangeLimitProperty = DependencyProperty.Register("VisibleRangeLimit", typeof(IRange), typeof(AxisBase), new PropertyMetadata(default(IRange), OnVisibleRangeLimitDependencyPropertyChanged));

        /// <summary>
        /// Defines the VisibleRangeLimitMode DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VisibleRangeLimitModeProperty = DependencyProperty.Register("VisibleRangeLimitMode", typeof (RangeClipMode), typeof (AxisBase), new PropertyMetadata(RangeClipMode.MinMax));

        /// <summary>
        /// Defines the Animated VisibleRange DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AnimatedVisibleRangeProperty = DependencyProperty.Register("AnimatedVisibleRange", typeof (IRange), typeof (AxisBase), new PropertyMetadata(default(IRange), OnAnimatedVisibleRangeDependencyPropertyChanged));

        /// <summary>
        /// Defines the VisibleRangePoint DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VisibleRangePointProperty = DependencyProperty.Register("VisibleRangePoint", typeof(Point), typeof(AxisBase), new PropertyMetadata(default(Point), OnVisibleRangePointDependencyPropertyChanged));

        /// <summary>
        /// Defines the MaxAutoTicks DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MaxAutoTicksProperty = DependencyProperty.Register("MaxAutoTicks", typeof(int), typeof(AxisBase), new PropertyMetadata(10, InvalidateParent));

        /// <summary>
        /// Defines the AutoTicks DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AutoTicksProperty = DependencyProperty.Register("AutoTicks", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Defines the TickProvider DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TickProviderProperty = DependencyProperty.Register("TickProvider", typeof (ITickProvider), typeof (AxisBase), new PropertyMetadata(default(ITickProvider), InvalidateParent));

        /// <summary>
        /// Defines the MinimalZoomConstrain DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinimalZoomConstrainProperty = DependencyProperty.Register("MinimalZoomConstrain", typeof (IComparable), typeof (AxisBase), new PropertyMetadata(default(IComparable)));

        /// <summary>
        /// Defines the Id DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register("Id", typeof(string), typeof(AxisBase), new PropertyMetadata(AxisBase.DefaultAxisId, InvalidateParent));

        /// <summary>
        /// Defines the FlipCoordinates DependencyProperty
        /// </summary>
        public static readonly DependencyProperty FlipCoordinatesProperty = DependencyProperty.Register("FlipCoordinates", typeof(bool), typeof(AxisBase), new PropertyMetadata(false, OnFlipCoordinatesChanged));

        /// <summary>
        /// Defines the TextFormatting DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TextFormattingProperty = DependencyProperty.Register("TextFormatting", typeof(string), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the AxisTitle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AxisTitleProperty = DependencyProperty.Register("AxisTitle", typeof(string), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the TitleStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TitleStyleProperty = DependencyProperty.Register("TitleStyle", typeof(Style), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the TitleFontWeight DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TitleFontWeightProperty = DependencyProperty.Register("TitleFontWeight", typeof(FontWeight), typeof(AxisBase), new PropertyMetadata(FontWeights.Normal, InvalidateParent));

        /// <summary>
        /// Defines the TitleFontWeight DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register("TitleFontSize", typeof(double), typeof(AxisBase), new PropertyMetadata(12.0, InvalidateParent));

        /// <summary>
        /// Defines the TickTextBrush DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TickTextBrushProperty = DependencyProperty.Register("TickTextBrush", typeof(Brush), typeof(AxisBase), new PropertyMetadata(InvalidateParent));

        /// <summary>
        /// Defines the MajorTickLineStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MajorTickLineStyleProperty = DependencyProperty.Register("MajorTickLineStyle", typeof(Style), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the MinorTickLineStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinorTickLineStyleProperty = DependencyProperty.Register("MinorTickLineStyle", typeof(Style), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the DrawMajorTicks DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawMajorTicksProperty = DependencyProperty.Register("DrawMajorTicks", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Defines the DrawMinorTicks DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawMinorTicksProperty = DependencyProperty.Register("DrawMinorTicks", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Defines the DrawLabels DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawLabelsProperty = DependencyProperty.Register("DrawLabels", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Defines the MajorGridLineStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MajorGridLineStyleProperty = DependencyProperty.Register("MajorGridLineStyle", typeof(Style), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the MinorGridLineStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinorGridLineStyleProperty = DependencyProperty.Register("MinorGridLineStyle", typeof(Style), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the DrawMajorGridLines DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawMajorGridLinesProperty = DependencyProperty.Register("DrawMajorGridLines", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Defines the DrawMinorGridLines DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawMinorGridLinesProperty = DependencyProperty.Register("DrawMinorGridLines", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Defines the DrawMajorBands DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DrawMajorBandsProperty = DependencyProperty.Register("DrawMajorBands", typeof(bool), typeof(AxisBase), new PropertyMetadata(false, InvalidateParent));

        /// <summary>
        /// Defines the AxisBandsFill DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AxisBandsFillProperty = DependencyProperty.Register("AxisBandsFill", typeof(Color), typeof(AxisBase), new PropertyMetadata(default(Color), InvalidateParent));

        /// <summary>
        /// Defines the AutoTicks DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TickLabelStyleProperty = DependencyProperty.Register("TickLabelStyle", typeof(Style), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        private IServiceContainer _serviceContainer;
        private IUltrachartSurfaceBase _parentSurface;
        private Point _fromPoint;
        private bool _isAnimationChange;

        // Two last valid ranges
        private IRange _lastValidRange, _secondLastValidRange;        

        /// <summary>
        /// Raised when the VisibleRange is changed
        /// </summary>
        public event EventHandler<VisibleRangeChangedEventArgs> VisibleRangeChanged;        

        /// <summary>
        /// Raised when properties are changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisBaseCommon"/> class.
        /// </summary>
        protected AxisBaseCommon()
        {
            _secondLastValidRange = _lastValidRange = GetDefaultNonZeroRange();

            // Workaround: Silverlight SetCurrentValue doesnt exist, so we can't do this here. Instead the value is set in OnApplyTemplate. 
            // This prevents some examples like the CreateMultiPaneStockCharts demo fail in SL purely because of SL API deficiencies, so we don't want this 
            // workaround to be in the WPF side of the chart
#if !SILVERLIGHT
            this.SetCurrentValue(VisibleRangeProperty, GetUndefinedRange());
#endif
            this.SetCurrentValue(TickCoordinatesProviderProperty, new DefaultTickCoordinatesProvider());

            new Ecng.Xaml.Licensing.Core.LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a logarithmic axis.
        /// </summary>
        public virtual bool IsLogarithmicAxis
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the ParentSurface that this Axis is associated with
        /// </summary>
        /// <value>The parent surface.</value>
        /// <remarks></remarks>
        public IUltrachartSurfaceBase ParentSurface
        {
            get { return _parentSurface; }
            set
            {
                _parentSurface = value;
                if (_parentSurface != null && _parentSurface.Services != null)
                {
                    Services = _parentSurface.Services;
                }

                OnPropertyChanged("ParentSurface");
            }
        }

        /// <summary>
        /// Gets whether the VisibleRange is valid, e.g. is not null, is not NaN and the difference between Max and Min is not zero
        /// </summary>
        /// <remarks></remarks>
        public bool HasValidVisibleRange
        {
            get { return IsVisibleRangeValid(); }
        }

        /// <summary>
        /// Gets whether the VisibleRange has default value
        /// </summary>
        public bool HasDefaultVisibleRange
        {
            get
            {
                var defaultRange = GetDefaultNonZeroRange();

                // _lastValidRange excluded from check because it has same value as VisibleRange
                return VisibleRange.Equals(defaultRange) && _secondLastValidRange.Equals(defaultRange);
            }
        }

        /// <summary>
        /// Gets or sets whether current Axis is the main one in axis collection
        /// </summary>
        /// <remarks>Primary axis determinate coordinate grid</remarks>
        public bool IsPrimaryAxis
        {
            get { return (bool) GetValue(IsPrimaryAxisProperty); }
            set { SetValue(IsPrimaryAxisProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether current Axis is a static axis
        /// </summary>
        public bool IsStaticAxis
        {
            get { return (bool)GetValue(IsStaticAxisProperty); }
            set { SetValue(IsStaticAxisProperty, value); }
        }

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        public IServiceContainer Services
        {
            get { return _serviceContainer; }
            set
            {
                _serviceContainer = value;
            }
        }

        /// <summary>
        /// Gets or sets the Axis Title
        /// </summary>
        /// <value>The axis title.</value>
        /// <remarks></remarks>
        public string AxisTitle
        {
            get { return (string) GetValue(AxisTitleProperty); }
            set { SetValue(AxisTitleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Style of the Axis Title
        /// </summary>
        public Style TitleStyle
        {
            get { return (Style)GetValue(TitleStyleProperty); }
            set { SetValue(TitleStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Axis Title Font Weight
        /// </summary>
        public FontWeight TitleFontWeight
        {
            get { return (FontWeight)GetValue(TitleFontWeightProperty); }
            set { SetValue(TitleFontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Axis Title Font Size
        /// </summary>
        public double TitleFontSize
        {
            get { return (double)GetValue(TitleFontSizeProperty); }
            set { SetValue(TitleFontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Text Formatting String for Tick Labels on this axis
        /// </summary>
        /// <value>The text formatting.</value>
        /// <remarks></remarks>
        public string TextFormatting
        {
            get { return (string) GetValue(TextFormattingProperty); }
            set { SetValue(TextFormattingProperty, value); }
        }

        /// <summary>
        /// Gets or sets AutoRange Mode
        /// </summary>
        public AutoRange AutoRange
        {
            get { return (AutoRange)GetValue(AutoRangeProperty); }
            set { SetValue(AutoRangeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the GrowBy Factor. e.g. GrowBy(0.1, 0.2) will increase the axis extents by 10% (min) and 20% (max) outside of the data range
        /// </summary>
        /// <value>The grow by factor as a DoubleRange.</value>
        [TypeConverter(typeof(StringToDoubleRangeTypeConverter))]
        public IRange<double> GrowBy
        {
            get { return (IRange<double>)GetValue(GrowByProperty); }
            set { SetValue(GrowByProperty, value); }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether to flip the tick and pixel coordinate generation for this axis, causing the axis ticks to decrement and chart to be flipped in the axis direction
        /// </summary>
        /// <value>
        ///   If <c>true</c> reverses the ticks and coordinates for the axis.
        /// </value>
        public bool FlipCoordinates
        {
            get { return (bool) GetValue(FlipCoordinatesProperty); }
            set { SetValue(FlipCoordinatesProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Major Delta
        /// </summary>
        /// <value>The major delta.</value>
        /// <remarks></remarks>
        public IComparable MajorDelta
        {
            get { return (IComparable)GetValue(MajorDeltaProperty); }
            set { SetValue(MajorDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the number of Minor Delta ticks per Major Tick
        /// </summary>
        /// <value>The major delta.</value>
        /// <remarks></remarks>
        public int MinorsPerMajor
        {
            get { return (int)GetValue(MinorsPerMajorProperty); }
            set { SetValue(MinorsPerMajorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the max ticks.
        /// </summary>
        /// <value>The max ticks.</value>
        /// <remarks></remarks>
        public int MaxAutoTicks
        {
            get { return (int)GetValue(MaxAutoTicksProperty); }
            set { SetValue(MaxAutoTicksProperty, value); }
        }

        /// <summary>
        /// Gets or sets value, that indicates whether calculate ticks automatically. Default is True.
        /// </summary>
        /// <remarks></remarks>
        public bool AutoTicks
        {
            get { return (bool)GetValue(AutoTicksProperty); }
            set { SetValue(AutoTicksProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="ITickProvider"/> instance on current axis,
        /// which is used to compute the data-values of Axis Gridlines, Ticks and Labels.
        /// </summary>
        public ITickProvider TickProvider
        {
            get { return (ITickProvider)GetValue(TickProviderProperty); }
            set { SetValue(TickProviderProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="ITickCoordinatesProvider"/> instance on current axis,
        /// which is used to transform the data-values received from the <see cref="IAxis.TickProvider"/> instance
        /// to the coordinates for Axis Gridlines, Ticks and Labels drawing.
        /// </summary>
        public ITickCoordinatesProvider TickCoordinatesProvider
        {
            get { return (ITickCoordinatesProvider)GetValue(TickCoordinatesProviderProperty); }
            set { SetValue(TickCoordinatesProviderProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Minor Delta
        /// </summary>
        /// <value>The minor delta.</value>
        /// <remarks></remarks>
        public IComparable MinorDelta
        {
            get { return (IComparable)GetValue(MinorDeltaProperty); }
            set { SetValue(MinorDeltaProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Major Tick Line Style (TargetType <see cref="Line"/>), applied to all major ticks on this axis
        /// </summary>
        /// <value>The major tick line style.</value>
        /// <remarks></remarks>
        public Style MajorTickLineStyle
        {
            get { return (Style)GetValue(MajorTickLineStyleProperty); }
            set { SetValue(MajorTickLineStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Minor Tick Line Style (TargetType <see cref="Line"/>), applied to all major ticks on this axis
        /// </summary>
        /// <value>The minor tick line style.</value>
        /// <remarks></remarks>
        public Style MinorTickLineStyle
        {
            get { return (Style)GetValue(MinorTickLineStyleProperty); }
            set { SetValue(MinorTickLineStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Major Grid Line Style (TargetType <see cref="Line"/>), applied to all major gridlines drawn by this axis
        /// </summary>
        /// <value>The major grid line style.</value>
        /// <remarks></remarks>
        public Style MajorGridLineStyle
        {
            get { return (Style)GetValue(MajorGridLineStyleProperty); }
            set { SetValue(MajorGridLineStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Minor Grid Line Style (TargetType <see cref="Line"/>), applied to all minor gridlines drawn by this axis
        /// </summary>
        /// <value>The minor grid line style.</value>
        /// <remarks></remarks>
        public Style MinorGridLineStyle
        {
            get { return (Style)GetValue(MinorGridLineStyleProperty); }
            set { SetValue(MinorGridLineStyleProperty, value); }
        }

        /// <summary>
        /// If True, draws Minor Tick Lines, else skips this step
        /// </summary>
        /// <remarks></remarks>
        public bool DrawMinorTicks
        {
            get { return (bool)GetValue(DrawMinorTicksProperty); }
            set { SetValue(DrawMinorTicksProperty, value); }
        }

        /// <summary>
        /// If True, draw labels for each major tick on the Axis, else skips this step
        /// </summary>
        public bool DrawLabels
        {
            get { return (bool)GetValue(DrawLabelsProperty); }
            set { SetValue(DrawLabelsProperty, value); }
        }

        /// <summary>
        /// If True, draws Major Tick Lines, else skips this step
        /// </summary>
        /// <remarks></remarks>
        public bool DrawMajorTicks
        {
            get { return (bool)GetValue(DrawMajorTicksProperty); }
            set { SetValue(DrawMajorTicksProperty, value); }
        }

        /// <summary>
        /// If True, draws Major Grid Lines, else skips this step
        /// </summary>
        /// <remarks></remarks>
        public bool DrawMajorGridLines
        {
            get { return (bool)GetValue(DrawMajorGridLinesProperty); }
            set { SetValue(DrawMajorGridLinesProperty, value); }
        }

        /// <summary>
        /// If True, draws Minor Grid Lines, else skips this step
        /// </summary>
        /// <remarks></remarks>
        public bool DrawMinorGridLines
        {
            get { return (bool)GetValue(DrawMinorGridLinesProperty); }
            set { SetValue(DrawMinorGridLinesProperty, value); }
        }

        /// <summary>
        /// If True, draws Major Axis Bands (a filled area between major gridlines), else skips this step
        /// </summary>
        /// <remarks></remarks>
        public bool DrawMajorBands
        {
            get { return (bool)GetValue(DrawMajorBandsProperty); }
            set { SetValue(DrawMajorBandsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Fill of the Axis Bands. Also see <see cref="DrawMajorBands"/> to enable this behaviour
        /// </summary>
        /// <remarks></remarks>
        public Color AxisBandsFill
        {
            get { return (Color)GetValue(AxisBandsFillProperty); }
            set { SetValue(AxisBandsFillProperty, value); }
        }

        /// <summary>
        /// Gets or sets the string Id of this axis. Used to associated <see cref="IRenderableSeries"/> and <see cref="YAxisDragModifier"/>
        /// </summary>
        public string Id
        {
            get { return (string)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        /// <summary>
        /// Gets or sets a style for the labels on this Axis.
        /// </summary>
        public Style TickLabelStyle
        {
            get { return (Style) GetValue(TickLabelStyleProperty); }
            set { SetValue(TickLabelStyleProperty, value); }
        }

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        /// <remarks></remarks>
        public bool IsSuspended
        {
            get { return UpdateSuspender.GetIsSuspended(this); }
        }

        /// <summary>
        /// Get the <see cref="RenderSurface"/> instance off the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected IRenderSurface RenderSurface { get { return ParentSurface != null ? ParentSurface.RenderSurface : null; } }

        /// <summary>
        /// Gets or sets the animated VisibleRange of the Axis. 
        /// When this property is set, the axis animates the VisibleRange to the new value over a duration of 500ms
        /// </summary>
        /// <value>The visible range.</value>
        /// <remarks></remarks>
        [TypeConverter(typeof(StringToDoubleRangeTypeConverter))]
        public IRange AnimatedVisibleRange
        {
            get { return (IRange)GetValue(AnimatedVisibleRangeProperty); }
            set { SetValue(AnimatedVisibleRangeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the VisibleRange of the Axis.
        /// </summary>
        /// <value>The visible range.</value>
        /// <remarks></remarks>
        [TypeConverter(typeof(StringToDoubleRangeTypeConverter))]
        public IRange VisibleRange
        {
            get { return (IRange)GetValue(VisibleRangeProperty); }
            set
            {
                SetValue(VisibleRangeProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the VisibleRangeLimit of the Axis. This will be used to clip the axis during ZoomExtents and AutoRange operations
        /// </summary>
        /// <value>The visible range.</value>
        /// <remarks></remarks>
        [TypeConverter(typeof(StringToDoubleRangeTypeConverter))]
        public IRange VisibleRangeLimit
        {
            get { return (IRange)GetValue(VisibleRangeLimitProperty); }
            set
            {
                SetValue(VisibleRangeLimitProperty, value);
            }
        }

        /// <summary>
        /// Gets or setts the VisibleRangeLimitMode of the Axis. This property defines which parts of <see cref="VisibleRangeLimit"/> will be used by axis
        /// </summary>
        public RangeClipMode VisibleRangeLimitMode
        {
            get { return (RangeClipMode)GetValue(VisibleRangeLimitModeProperty); }
            set { SetValue(VisibleRangeLimitModeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the MinimalZoomConstrain of the Axis. This is used to set minimum distance between Min and Max of the VisibleRange 
        /// </summary>
        /// <value>The minimum distance between Min and Max of the VisibleRange</value>
        public IComparable MinimalZoomConstrain
        {
            get { return (IComparable)GetValue(MinimalZoomConstrainProperty); }
            set { SetValue(MinimalZoomConstrainProperty, value); }
        }

        internal bool IsLicenseValid { get; set; }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="System.Windows.FrameworkElement.OnApplyTemplate()"/>.
        /// </summary>
        /// <remarks></remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

#if SILVERLIGHT
            // Fix for VisibleRange DependencyProperty Precdence issue (Create Multi Pane Stock Charts example). See AxisBase.ctor for comments 
            if (VisibleRange == null)
                this.SetCurrentValue(VisibleRangeProperty, GetUndefinedRange());      
#endif
        }

        /// <summary>
        /// Animates the visible range of the axis to the destination VisibleRange, over the specified Duration. 
        /// Also see <see cref="AxisBaseCommon.AnimatedVisibleRange"/> property which has a default duration of 500ms
        /// </summary>
        /// <param name="to">The end range</param>
        /// <param name="duration">The duration of the animation.</param>
        public void AnimateVisibleRangeTo(IRange to, TimeSpan duration)
        {
            Guard.ArgumentNotNull(to, "to");

            if (!HasValidVisibleRange)
            {
                VisibleRange = to;
                return;
            }

            _fromPoint = VisibleRange.TransformRangeToPoint(IsLogarithmicAxis);
            var toPoint = to.TransformRangeToPoint(IsLogarithmicAxis);

            var pointAnimation = new PointAnimation
            {
                From = new Point(0, 0),
                To = new Point(toPoint.X - _fromPoint.X, toPoint.Y - _fromPoint.Y),
                Duration = duration,
                EasingFunction =
                    new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 7.0 }
            };

            var prevRange = (IRange)VisibleRange.Clone();
            pointAnimation.Completed += (s, e) =>
            {
                VisibleRange = to;

                _isAnimationChange = false;

                pointAnimation.FillBehavior = FillBehavior.Stop;

                OnVisibleRangeChanged(new VisibleRangeChangedEventArgs(prevRange, to, _isAnimationChange));
            };

            Storyboard.SetTarget(pointAnimation, this);
            Storyboard.SetTargetProperty(pointAnimation, new PropertyPath("VisibleRangePoint"));

            var storyboard = new Storyboard { Duration = duration };
            storyboard.Children.Add(pointAnimation);

            _isAnimationChange = true;

            storyboard.Begin();
        }

        /// <summary>
        /// Asynchronously requests that the element redraws itself plus children.
        /// Will be ignored if the element is ISuspendable and currently IsSuspended (within a SuspendUpdates/ResumeUpdates call)
        /// </summary>
        /// <remarks></remarks>
        public void InvalidateElement()
        {
            if (ParentSurface != null)
            {
                ParentSurface.InvalidateElement();
            }
        }

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        /// <remarks></remarks>
        public void DecrementSuspend()
        {
        }

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>The disposable Update Suspender</returns>
        /// <remarks></remarks>
        public IUpdateSuspender SuspendUpdates()
        {
            return new UpdateSuspender(this);
        }

        /// <summary>
        /// Resumes updates on the target, intended to be called by IUpdateSuspender
        /// </summary>
        /// <remarks></remarks>
        public void ResumeUpdates(IUpdateSuspender updateSuspender)
        {
            if (updateSuspender.ResumeTargetOnDispose)
            {
                InvalidateElement();
            }
        }

        /// <summary>
        /// Raises the VisibleRangeChanged event
        /// </summary>
        /// <param name="args">The <see cref="VisibleRangeChangedEventArgs"/> containing event data</param>
        protected virtual void OnVisibleRangeChanged(VisibleRangeChangedEventArgs args)
        {            
            var handler = VisibleRangeChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event, as part of <see cref="INotifyPropertyChanged"/> implementation
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Asserts the <see cref="IRange"/> is of the correct type for this axis
        /// </summary>
        /// <param name="range">The range to assert</param>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual void AssertRangeType(IRange range)
        {
            if (!IsOfValidType(range))
            {
                throw new InvalidOperationException(string.Format(
                    "Axis type {0} requires that VisibleRange is of type {1}",
                    GetType().Name, VisibleRange.GetType().FullName));
            }
        }

        private static void OnIsStaticAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;

            if (axis != null)
            {
                if (axis.IsStaticAxis && axis.IsCategoryAxis)
                {
                    throw new InvalidOperationException(
                        "The CategoryDateTimeAxis type does not support the Static mode (IsStatic).");
                }

                axis.TickCoordinatesProvider = axis.IsStaticAxis
                    ? new StaticTickCoordinatesProvider()
                    : new DefaultTickCoordinatesProvider();
            }
        }

        private static void OnTickCoordinatesProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;

            if (axis != null)
            {
                axis.TickCoordinatesProvider.Init(axis);
            }
        }

        /// <summary>
        /// Provides a DependencyProperty callback which invalidates the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        protected static void InvalidateParent(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBaseCommon)d);

            //if (axis.HasAxisPanel())
            //{
                axis.InvalidateElement();
            //}
        }

        private static void OnVisibleRangeLimitDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBase)d);

            var clip = e.NewValue as IRange;
            if (clip == null || axis.VisibleRange == null) return;

            var clone = (IRange)axis.VisibleRange.Clone();
            clone = clone.ClipTo(clip);

            axis.SetCurrentValue(VisibleRangeProperty, clone);
        }

        private static void OnAnimatedVisibleRangeDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBase)d);

            axis.AnimateVisibleRangeTo((IRange)e.NewValue, TimeSpan.FromMilliseconds(500));
        }

        private static void OnVisibleRangeDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBase)d);

            if (!axis.Dispatcher.CheckAccess())
            {
                Action action = () => OnVisibleRangeDependencyPropertyChanged(d, e);
                axis.Dispatcher.BeginInvoke(action);
                return;
            }

            var oldRange = e.OldValue as IRange;
            var newRange = e.NewValue as IRange;

            if (oldRange != null)
            {
                oldRange.PropertyChanged -= axis.OnMaxMinVisibleRangePropertiesChanged;
            }

            if (newRange != null)
            {
                // Try to change the range if expected case
                if (!axis.HasValidVisibleRange || !axis.IsVisibleRangeMinimalConstrainValid())
                {
                    axis.CoerceVisibleRange();
                }

                // If new range is not valid, set to default range and throw an exception
                if (!axis.HasValidVisibleRange || !axis.IsVisibleRangeMinimalConstrainValid())
                {
                    axis.SetCurrentValue(VisibleRangeProperty, axis._lastValidRange);
                    axis.AssertRangeType(newRange);
                    return;
                }

                newRange.PropertyChanged += axis.OnMaxMinVisibleRangePropertiesChanged;

                if (axis.TryApplyVisibleRange(newRange, oldRange))
                {
                    axis._secondLastValidRange = axis._lastValidRange;
                    axis._lastValidRange = newRange;
                }
            }
        }

        private static void OnVisibleRangePointDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (Point)e.NewValue;
            var axis = (AxisBase)d;

            // Debug.WriteLine("New Range: Min {0}, Max {1}", p.X, p.Y);
            axis.Dispatcher.BeginInvokeAlways(() =>
            {
                var min = axis.IsLogarithmicAxis ? Math.Pow(10, axis._fromPoint.X + p.X) : axis._fromPoint.X + p.X;
                var max = axis.IsLogarithmicAxis ? Math.Pow(10, axis._fromPoint.Y + p.Y) : axis._fromPoint.Y + p.Y;

                axis.VisibleRange =
                    RangeFactory.NewRange(axis.VisibleRange.GetType(), min,
                                          max);
            });
        }

        private static void OnIsPrimaryAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBaseCommon;

            if (axis != null && axis.IsPrimaryAxis)
            {
                axis.InvalidateElement();
            }
        }

        private static void OnFlipCoordinatesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;
            if (axis != null)
            {
                if (axis.FlipCoordinates && axis.IsCategoryAxis)
                {
                    throw new InvalidOperationException(
                        "The CategoryDateTimeAxis type does not support coordinate reversal (FlipCoordinates).");
                }

                InvalidateParent(d, e);
            }
        }

        /// <summary>
        /// When current VisibleRange is invalid, tries to replace it by <paramref name="oldRange"/>,
        /// if both ranges are invalid, throws an exception
        /// </summary>
        /// <param name="newRange">The range to apply</param>
        /// <param name="oldRange">The previous VisibleRange</param>
        /// <returns>The value, which indicates whether the VisibleRange is applied or no</returns>
        private bool TryApplyVisibleRange(IRange newRange, IRange oldRange)
        {
            var isApplied = false;

            ValidateVisibleRange(newRange);

            //Invalidate the surface and fire event if the range changed not during animation
            if (!newRange.Equals(oldRange))
            {
                OnVisibleRangeChanged(new VisibleRangeChangedEventArgs(oldRange, newRange, _isAnimationChange));

                InvalidateElement();

                isApplied = true;
            }

            return isApplied;
        }

        /// <summary>
        /// Throws appropriate exceptions if the VisibleRange has a wrong type, or VisibleRange.Min > VisibleRange.Max
        /// </summary>
        private void ValidateVisibleRange(IRange range)
        {
            AssertRangeType(range);

            if (range.Min.CompareTo(range.Max) > 0)
            {
                throw new ArgumentException(
                    string.Format("VisibleRange.Min (value={0}) must be less than VisibleRange.Max (value={1})",
                                    range.Min, range.Max));
            }
        }

        private void OnMaxMinVisibleRangePropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            var oldMin = VisibleRange.Min;
            var oldMax = VisibleRange.Max;

            switch (e.PropertyName)
            {
                case "Min":
                    oldMin = (IComparable)((PropertyChangedEventArgsWithValues)e).OldValue;
                    break;
                case "Max":
                    oldMax = (IComparable)((PropertyChangedEventArgsWithValues)e).OldValue;
                    break;
            }

            var oldRange = RangeFactory.NewWithMinMax(VisibleRange, oldMin, oldMax);
            var isApplied = TryApplyVisibleRange(VisibleRange, oldRange);

            //Update the binding only if the ranges are different and the new range is applied
            if (isApplied)
            {
                var expr = GetBindingExpression(VisibleRangeProperty);

                if (expr != null &&
                    expr.ParentBinding.UpdateSourceTrigger != System.Windows.Data.UpdateSourceTrigger.Explicit)
                {
                    expr.UpdateSource();
#if !SILVERLIGHT
                    expr.UpdateTarget();
#endif
                }
            }
        }
    }
}