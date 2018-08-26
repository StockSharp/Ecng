// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Numerics.CoordinateProviders;
using Ecng.Xaml.Charting.Numerics.TickCoordinateProviders;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides base functionality for Axes throughout Ultrachart. Derived types include <see cref="NumericAxis"/>, which supports any 
    /// numeric value, <see cref="DateTimeAxis"/>, which supports Date values. Axes may be styled, see the <see href="http://www.ultrachart.com/tutorials">tutorials</see> for more details
    /// </summary>
    [TemplatePart(Name = "PART_AxisCanvas", Type = typeof(IAxisPanel))]
    [TemplatePart(Name = "PART_ModifierAxisCanvas", Type = typeof(AxisCanvas))]
    [TemplatePart(Name = "PART_AxisContainer", Type = typeof(StackPanel))]
    [TemplatePart(Name = "PART_AxisRenderSurface", Type = typeof(HighSpeedRenderSurface))]
    public abstract class AxisBase : ContentControl, IAxis, INotifyPropertyChanged, IXmlSerializable
    {        
        /// <summary>
        /// Defines the TickCoordinatesProvider DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TickCoordinatesProviderProperty =
            DependencyProperty.Register("TickCoordinatesProvider", typeof(ITickCoordinatesProvider), typeof(AxisBase), new PropertyMetadata(OnTickCoordinatesProviderChanged));

        /// <summary>
        /// Defines the IsStaticAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsStaticAxisProperty =
            DependencyProperty.Register("IsStaticAxis", typeof(bool), typeof(AxisBase), new PropertyMetadata(false, OnIsStaticAxisChanged));

        /// <summary>
        /// Defines the IsPrimaryAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsPrimaryAxisProperty = DependencyProperty.Register("IsPrimaryAxis", typeof(bool), typeof(AxisBase), new PropertyMetadata (false, OnIsPrimaryAxisChanged));

        /// <summary>
        /// Defines the IsCenterAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsCenterAxisProperty = DependencyProperty.Register("IsCenterAxis", typeof (bool), typeof (AxisBase), new PropertyMetadata(default(bool), OnIsCenterAxisDependencyPropertyChanged));

        /// <summary>
        /// Defines the AxisMode DependencyProperty
        /// </summary>
        [Obsolete("We're sorry! AxisBase.AxisMode is obsolete. To create a chart with Logarithmic axis, please the LogarithmicNumericAxis type instead")]
        public static readonly DependencyProperty AxisModeProperty = DependencyProperty.Register("AxisMode", typeof(AxisMode), typeof(AxisBase), new PropertyMetadata(AxisMode.Linear));        

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
        /// Defines the AutoAlignVisibleRange DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AutoAlignVisibleRangeProperty = DependencyProperty.Register("AutoAlignVisibleRange", typeof(bool), typeof(AxisBase), new PropertyMetadata(false, InvalidateParent));

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
        public static readonly DependencyProperty TickProviderProperty = DependencyProperty.Register("TickProvider", typeof(ITickProvider), typeof(AxisBase), new PropertyMetadata(default(ITickProvider), OnTickProviderChanged));

        /// <summary>
        /// Defines the MinimalZoomConstrain DependencyProperty
        /// </summary>
        public static readonly DependencyProperty MinimalZoomConstrainProperty = DependencyProperty.Register("MinimalZoomConstrain", typeof (IComparable), typeof (AxisBase), new PropertyMetadata(default(IComparable)));

        /// <summary>
        /// Defines the Orientation DependencyProperty
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(AxisBase), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

        /// <summary>
        /// Defines the AxisAlignment DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AxisAlignmentProperty = DependencyProperty.Register("AxisAlignment", typeof(AxisAlignment), typeof(AxisBase), new PropertyMetadata(AxisAlignment.Default, OnAlignmentChanged));

        /// <summary>
        /// Defines the Id DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register("Id", typeof(string), typeof(AxisBase), new PropertyMetadata(DefaultAxisId, InvalidateParent));

        /// <summary>
        /// Defines the FlipCoordinates DependencyProperty
        /// </summary>
        public static readonly DependencyProperty FlipCoordinatesProperty = DependencyProperty.Register("FlipCoordinates", typeof(bool), typeof(AxisBase), new PropertyMetadata(false, OnFlipCoordinatesChanged));

        /// <summary>
        /// Defines the LabelFormatter DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LabelProviderProperty = DependencyProperty.Register("LabelProvider", typeof (ILabelProvider), typeof (AxisBase), new PropertyMetadata(null, OnLabelProviderChanged));

        /// <summary>
        /// Defines the DefaultFormatter DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DefaultLabelProviderProperty = DependencyProperty.Register("DefaultLabelProvider", typeof(ILabelProvider), typeof(AxisBase), new PropertyMetadata(null));

        /// <summary>
        /// Defines the TextFormatting DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TextFormattingProperty = DependencyProperty.Register("TextFormatting", typeof(string), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

        /// <summary>
        /// Defines the CursorTextFormatting DependencyProperty
        /// </summary>
        public static readonly DependencyProperty CursorTextFormattingProperty = DependencyProperty.Register("CursorTextFormatting", typeof(string), typeof(AxisBase), new PropertyMetadata(null, InvalidateParent));

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
        /// Defines the StrokeThickness DependencyProperty
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register("StrokeThickness", typeof(double), typeof(AxisBase), new PropertyMetadata(1.0, InvalidateParent));

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

        /// <summary>
        /// Defines the Axis ScrollBar
        /// </summary>
        public static readonly DependencyProperty ScrollbarProperty = DependencyProperty.Register("Scrollbar", typeof (UltrachartScrollbar), typeof (AxisBase), new PropertyMetadata(null, OnScrollBarChanged));

        /// <summary>
        /// The IsLabelCullingEnabled DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsLabelCullingEnabledProperty = DependencyProperty.Register("IsLabelCullingEnabled", typeof(bool), typeof(AxisBase), new PropertyMetadata(true, InvalidateParent));

        /// <summary>
        /// Raised when properties are changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Event raised immediately after the <see cref="AxisBase"/> measures itself
        /// </summary>
        public event EventHandler<EventArgs> Arranged;

        /// <summary>
        /// Raised when the VisibleRange is changed
        /// </summary>
        public event EventHandler<VisibleRangeChangedEventArgs> VisibleRangeChanged;

        /// <summary>
        /// Raised when data range is changed
        /// </summary>
        public event EventHandler<EventArgs> DataRangeChanged;

        private IServiceContainer _serviceContainer;

        /// <summary>
        /// The current CoordinateCalculator for this render pass
        /// </summary>        
        protected ICoordinateCalculator<double> _currentCoordinateCalculator;

        /// <summary>
        /// The current InteractivityHelper for this render pass
        /// </summary>
        protected IAxisInteractivityHelper _currentInteractivityHelper;

        private ILabelProvider _defaultLabelProvider;

        private IUltrachartSurface _parentSurface;

        private IAxisPanel _axisPanel;
        private ModifierAxisCanvas _modifierAxisCanvas;
        
        private bool _isXAxis = true;

        private ITickLabelsPool _labelsPool;
        private TickCoordinates _tickCoords;
        private float _offset;

        private AxisAlignmentToVeticalAnchorPointConverter _axisAlignmentToVerticalAnchorPointConverter = new AxisAlignmentToVeticalAnchorPointConverter();
        private AxisAlignmentToHorizontalAnchorPointConverter _axisAlignmentToHorizontalAnchorPointConverter = new AxisAlignmentToHorizontalAnchorPointConverter();

        private bool _isAnimationChange;

        // Two last valid ranges
        private IRange _lastValidRange, _secondLastValidRange;
        private Point _fromPoint;

        /// <summary>
        /// The Default Axis Id for new Axes
        /// </summary>
        public const string DefaultAxisId = "DefaultAxisId";

        /// <summary>
        /// Defines the minimum distance to the edge of the chart to cull axis labels
        /// </summary>
        protected const int MinDistanceToBounds = 1;

        /// <summary>
        /// Gets GrowBy Min and Max which applied to VisibleRange if VisibleRange.Min == VisibleRAnge.Max
        /// </summary>
        protected const double ZeroRangeGrowBy = 0.01;

        private static readonly int[] LabelCullingDistances = { 2, 4, 8, 16, 32};

        private StackPanel _axisContainer;

        protected Line LineToStyle = new Line();

        /// <summary>
        /// Initializes a new instance of the <see cref="AxisBase"/> class.
        /// </summary>
        /// <remarks></remarks>
        protected AxisBase()
        {
            DefaultStyleKey = typeof (AxisBase);

            _secondLastValidRange = _lastValidRange = GetDefaultNonZeroRange();

            this.SetCurrentValue(TickCoordinatesProviderProperty, new DefaultTickCoordinatesProvider());

            InitializeLabelsPool();

            SizeChanged += (s, e) => InvalidateElement();

            new Ecng.Xaml.Licensing.Core.LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());
        }

        internal AxisBase(IAxisPanel axisPanel) : this()
        {
            _axisPanel = axisPanel;
        }

        private void InitializeLabelsPool()
        {
            _labelsPool = _labelsPool ??
                          (this is NumericAxis
                              ? (ITickLabelsPool)new TickLabelsPool<NumericTickLabel>(MaxAutoTicks, ApplyStyle)
                              : new TickLabelsPool<DefaultTickLabel>(MaxAutoTicks, ApplyStyle));
        }

        private DefaultTickLabel ApplyStyle(DefaultTickLabel defaultTickLabel)
        {
            // Prevents a tick label from inheriting the DataContext of its visual parent.
            // It'll be taken from the LabelProvider later
            defaultTickLabel.DataContext = null;

            defaultTickLabel.SetBinding(DefaultTickLabel.DefaultForegroundProperty, new Binding("TickTextBrush") { Source = this });
            defaultTickLabel.SetBinding(DefaultTickLabel.DefaultHorizontalAnchorPointProperty, new Binding("AxisAlignment") { Source = this, Converter = _axisAlignmentToHorizontalAnchorPointConverter });
            defaultTickLabel.SetBinding(DefaultTickLabel.DefaultVerticalAnchorPointProperty, new Binding("AxisAlignment") { Source = this, Converter = _axisAlignmentToVerticalAnchorPointConverter });

            defaultTickLabel.SetBinding(StyleProperty, new Binding("TickLabelStyle") { Source = this });

            return defaultTickLabel;
        }

        
        internal bool IsLicenseValid { get; set; }

        /// <summary>
        /// Gets whether the current axis is an X-Axis or not
        /// </summary>
        bool IAxis.IsXAxis { get { return IsXAxis; } set { IsXAxis = value; } }

        /// <summary>
        /// Gets whether the current axis is an X-Axis or not
        /// </summary>
        public bool IsXAxis
        {
            get { return _isXAxis; }
            private set
            {
                _isXAxis = value;

                // Fixes AxisAlignment issue (http://ulcsoftware.myjetbrains.com/youtrack/issue/SC-1186),
                // AxisAlignment should be bound to this in the default style.
                // Triggers AxisAlignment change.
                OnPropertyChanged("IsXAxis");
            }
        }

        /// <summary>
        /// Gets whether the current axis is horizontal or not
        /// </summary>
        public virtual bool IsHorizontalAxis
        {
            get { return Orientation == Orientation.Horizontal; }
        }

        /// <summary>
        /// Gets whether the current axis is flipped (e.g. YAxis on the bottom or top, or XAxis on the left or right)
        /// </summary>
        public bool IsAxisFlipped
        {
            get { return IsHorizontalAxis != IsXAxis; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Label Culling is enabled (when labels overlap) on this AxisPanel instance
        /// </summary>
        public bool IsLabelCullingEnabled
        {
            get { return (bool)GetValue(IsLabelCullingEnabledProperty); }
            set { SetValue(IsLabelCullingEnabledProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether current Axis should placed in the center of chart or not
        /// </summary>
        public bool IsCenterAxis
        {
            get { return (bool)GetValue(IsCenterAxisProperty); }
            set { SetValue(IsCenterAxisProperty, value); }
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
        /// Gets or sets whether to auto-align the visible range to the data when it is set. Note that this property only applies to the X-Axis.
        /// The default value is False. Whenever the <see cref="VisibleRange"/> is set on the X-Axis, the Min and Max values will be aligned to data values in the <see cref="IDataSeries.XValues"/>
        /// </summary>
        /// <value><c>true</c> if [auto align visible range]; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        public bool AutoAlignVisibleRange
        {
            get { return (bool)GetValue(AutoAlignVisibleRangeProperty); }
            set { SetValue(AutoAlignVisibleRangeProperty, value); }
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
        /// Gets or sets the width of the element.
        /// </summary>
        /// <returns>The width of the element, in device-independent units (1/96th inch per unit). The default value is <see cref="F:System.Double.NaN"/>. This value must be equal to or greater than 0.0. See Remarks for upper bound information.</returns>
        /// <remarks></remarks>
        double IDrawable.Width { get { return ActualWidth; } set { }}

        /// <summary>
        /// Gets or sets the suggested height of the element.
        /// </summary>
        /// <returns>The height of the element, in device-independent units (1/96th inch per unit). The default value is <see cref="F:System.Double.NaN"/>. This value must be equal to or greater than 0.0. See Remarks for upper bound information.</returns>
        /// <remarks></remarks>
        double IDrawable.Height { get { return ActualHeight; } set { }}

        /// <summary>
        /// Gets or sets the ParentSurface that this Axis is associated with
        /// </summary>
        /// <value>The parent surface.</value>
        /// <remarks></remarks>
        public IUltrachartSurface ParentSurface
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
        /// Gets or sets the Text Formatting String for Labels on this cursor
        /// </summary>
        public string CursorTextFormatting
        {
            get { return (string)GetValue(CursorTextFormattingProperty); }
            set { SetValue(CursorTextFormattingProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="ILabelProvider"/> instance, which may be used to programmatically override the formatting of text and cursor labels. 
        /// For examples, see the <see cref="DefaultLabelProvider"/> and <see cref="TradeChartAxisLabelProvider"/>
        /// </summary>
        public ILabelProvider LabelProvider
        {
            get { return (ILabelProvider) GetValue(LabelProviderProperty); }
            set { SetValue(LabelProviderProperty, value); }
        }

        /// <summary>
        /// Gets the default <see cref="ILabelProvider"/> instance.
        /// </summary>
        public ILabelProvider DefaultLabelProvider
        {
            get { return (ILabelProvider)GetValue(DefaultLabelProviderProperty); }
            protected set { SetValue(DefaultLabelProviderProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="AxisMode"/>, e.g. Linear or Logarithmic, that this Axis operates in
        /// </summary>
        [Obsolete("We're sorry! AxisBase.AxisMode is obsolete. To create a chart with Logarithmic axis, please the LogarithmicNumericAxis type instead")]
        public AxisMode AxisMode
        {
            get { throw new Exception("We're sorry! AxisBase.AxisMode is obsolete. To create a chart with Logarithmic axis, please the LogarithmicNumericAxis type instead"); }
            set { throw new Exception("We're sorry! AxisBase.AxisMode is obsolete. To create a chart with Logarithmic axis, please the LogarithmicNumericAxis type instead"); }
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
        /// Gets or sets the tick text brush applied to text labels
        /// </summary>
        /// <value>The tick text brush</value>
        /// <remarks></remarks>
        public Brush TickTextBrush
        {
            get { return (Brush)GetValue(TickTextBrushProperty); }
            set { SetValue(TickTextBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Major Line Stroke for this axis
        /// </summary>
        /// <value>The major line stroke.</value>
        /// <remarks></remarks>
        [Obsolete("MajorLineStroke is obsolete, please use MajorTickLineStyle instead", true)]
        public Brush MajorLineStroke
        {
            get { return null; } set { throw new Exception("MajorLineStroke is obsolete, please use MajorTickLineStyle instead"); }
        }

        /// <summary>
        /// Gets or sets the Minoe Line Stroke for this axis
        /// </summary>
        /// <value>The minor line stroke.</value>
        /// <remarks></remarks>
        [Obsolete("MinorLineStroke is obsolete, please use MajorTickLineStyle instead", true)]
        public Brush MinorLineStroke
        {
            get { return null; }
            set { throw new Exception("MinorLineStroke is obsolete, please use MajorTickLineStyle instead"); }
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
        /// Gets or sets the Axis Orientation, e.g. Horizontal (XAxis) or Vertical (YAxis)
        /// </summary>
        /// <value>The orientation.</value>
        /// <remarks></remarks>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="AxisAlignment"/> for this Axis. Default is Right.
        /// </summary>
        public AxisAlignment AxisAlignment
        {
            get { return (AxisAlignment)GetValue(AxisAlignmentProperty); }
            set { SetValue(AxisAlignmentProperty, value); }
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
        /// Gets or sets the stroke thickness.
        /// </summary>
        /// <value>The stroke thickness.</value>
        /// <remarks></remarks>
        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
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
        /// Gets or Sets Axis ScrollBar
        /// </summary>
        public UltrachartScrollbar Scrollbar
        {
            get { return (UltrachartScrollbar)GetValue(ScrollbarProperty); }
            set { SetValue(ScrollbarProperty, value); }
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
        /// Gets the ModifierAxisCanvas, which may be used to overlay markers on the canvas
        /// </summary>
        public IAnnotationCanvas ModifierAxisCanvas { get { return _modifierAxisCanvas; } }

        /// <summary>
        /// Get the <see cref="GridLinesPanel"/> instance off the parent <see cref="UltrachartSurface"/>
        /// </summary>
        [Obsolete("GridLinesPanel no longer draws gridlines. These are now added to the RenderSurfaceBase instance instead for performance. Use AxisBase.RenderSurface instead.")]
        protected IGridLinesPanel GridLinesPanel { get { return ParentSurface != null ? ParentSurface.GridLinesPanel : null; } }

        /// <summary>
        /// Get the <see cref="RenderSurface"/> instance off the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected IRenderSurface RenderSurface { get { return ParentSurface != null ? ParentSurface.RenderSurface : null; } }

        /// <summary>
        /// Gets a value indicating whether this instance is a category axis.
        /// </summary>
        public virtual bool IsCategoryAxis
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a logarithmic axis.
        /// </summary>
        public virtual bool IsLogarithmicAxis
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a polar axis.
        /// </summary>
        public virtual bool IsPolarAxis
        {
            get { return false; }
        }

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
                //this.SetCurrentValue(VisibleRangeProperty, value);
                this.SetValue(VisibleRangeProperty, value);
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

        /// <summary>
        /// Gets the DataRange (full extents of the data) of the Axis.
        /// </summary>
        /// <value>The data range</value>
        /// <remarks>Note: The performance implications of calling this is axis will perform a full recalculation on each get. 
        /// It is recommended to get and cache if this property is needed more than once</remarks>
        public IRange DataRange
        {
            get { return CalculateDataRange(); }
        }

        // Canvases, used internally
        internal IAxisPanel AxisPanel { get { return _axisPanel; } }

        internal StackPanel AxisContainer { get { return _axisContainer; } }

        internal ITickLabelsPool TickLabelsPool { get { return _labelsPool; } }
        /// <summary>
        /// Gets the current data-point size in pixels
        /// </summary>
        public virtual double CurrentDatapointPixelSize { get { return double.NaN; }}
        
        /// <summary>
        /// Gets the integer indices of the X-Data array that are currently in range.
        /// </summary>
        /// <returns>
        /// The indices to the X-Data that are currently in range
        /// </returns>
        /// <example>If the input X-data is 0...100 in steps of 1, the VisibleRange is 10, 30 then the PointRange will be 10, 30</example>
        [Obsolete("AxisBase.GetPointRange is obsolete, please call DataSeries.GetIndicesRange(VisibleRange) instead", true)]
        public IntegerRange GetPointRange()
        {
            throw new NotSupportedException("AxisBase.GetPointRange is obsolete, please call DataSeries.GetIndicesRange(VisibleRange) instead");
        }

        /// <summary>
        /// Returns an undefined <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>        
        public abstract IRange GetUndefinedRange();

        /// <summary>
        /// Returns an default non zero <see cref="IRange" />, called internally by Ultrachart to reset the VisibleRange of an axis to an undefined state
        /// </summary>
        /// <returns></returns>
        public abstract IRange GetDefaultNonZeroRange();

        /// <summary>
        /// Gets the aligned VisibleRange of the axis, with optional ZoomToFit flag.
        /// If ZoomToFit is true, it will return the DataRange plus any GrowBy applied to the axis
        /// </summary>
        /// <param name="renderPassInfo">Struct containing data for the current render pass</param>
        /// <returns>The VisibleRange of the axis</returns>
        /// <remarks></remarks>
        public abstract IRange CalculateYRange(RenderPassInfo renderPassInfo);

        /// <summary>
        /// Calculates data range of current axis
        /// </summary>
        /// <returns></returns>
        protected virtual IRange CalculateDataRange()
        {
            return ParentSurface != null && !ParentSurface.RenderableSeries.IsNullOrEmpty()
                ? (IsXAxis ? GetXDataRange() : GetYDataRange())
                : null;
        }

        private IRange GetXDataRange()
        {
            IRange maximumRange = null;

            foreach (
                var rSeries in
                    ParentSurface.RenderableSeries.Where(x => x.XAxisId == Id && x.IsVisible && x.DataSeries?.HasValues == true))
            {
                var xRange = rSeries.GetXRange();

                if (xRange != null && xRange.IsDefined)
                {
                    var doubleRange = xRange.AsDoubleRange();
                    maximumRange = maximumRange == null ? doubleRange : doubleRange.Union(maximumRange);
                }
            }

            return maximumRange;
        }

        private IRange GetYDataRange()
        {
            IRange maximumRange = null;

            foreach (
                var rSeries in
                    ParentSurface.RenderableSeries.Where(x => x.YAxisId == Id && x.IsVisible && x.DataSeries != null))
            {
                var yRange = rSeries.DataSeries.YRange;

                if (yRange != null && yRange.IsDefined)
                {
                    var doubleRange = yRange.AsDoubleRange();
                    maximumRange = maximumRange == null ? doubleRange : doubleRange.Union(maximumRange);
                }
            }

            return maximumRange;
        }

        /// <summary>
        /// Gets the Maximum Range of the axis, which is equal to the DataRange including any GrowBy factor applied
        /// </summary>
        /// <returns></returns>
        public virtual IRange GetMaximumRange()
        {
            IRange maximumRange = new DoubleRange(double.NaN, double.NaN); 

            if (ParentSurface != null && !ParentSurface.RenderableSeries.IsNullOrEmpty())
            {
                if (IsXAxis)
                {
                    maximumRange = GetXDataRange() ?? maximumRange;

                    if (maximumRange.IsZero)
                        maximumRange = CoerceZeroRange(maximumRange);

                    if (GrowBy != null)
                    {
                        var logBase = IsLogarithmicAxis ? ((ILogarithmicAxis) this).LogarithmicBase : 0;
                        maximumRange = maximumRange.GrowBy(GrowBy.Min, GrowBy.Max, IsLogarithmicAxis, logBase);
                    }

                    if (VisibleRangeLimit != null)
                    {
                        maximumRange.ClipTo(VisibleRangeLimit.AsDoubleRange(), VisibleRangeLimitMode);
                    }
                }
                else
                {
                    maximumRange = GetWindowedYRange(null);
                }
            }

            var currentVisibleRange = VisibleRange != null && VisibleRange.IsDefined
                              ? VisibleRange
                              : GetDefaultNonZeroRange();

            return (maximumRange != null && maximumRange.IsDefined) ? maximumRange : currentVisibleRange.AsDoubleRange();
        }

        /// <summary>
        /// Coerce <seealso cref="IRange"/> if current range is zero range
        /// </summary>
        /// <param name="maximumRange">Current maximum range</param>
        protected virtual IRange CoerceZeroRange(IRange maximumRange)
        {
            return maximumRange.GrowBy(ZeroRangeGrowBy, ZeroRangeGrowBy);
        }

        /// <summary>
        /// Returns the max range only for that axis (by the data-series on it), based on <paramref name="xRanges"/>
        /// "windowed" = "displayed in current viewport"
        /// uses GrowBy()
        /// </summary>
        /// <param name="xRanges">Calculates the max range based on corresponding x ranges</param>
        /// <returns></returns>
        public IRange GetWindowedYRange(IDictionary<string, IRange> xRanges)
        {
            IRange maxRange = new DoubleRange(double.NaN, double.NaN);

            if (ParentSurface != null && !ParentSurface.RenderableSeries.IsNullOrEmpty())
            {
                foreach (var rSeries in ParentSurface.RenderableSeries.Where(x => 
                    // omit invisible & empty rSeries
                    x.YAxisId == Id && x.DataSeries != null && x.IsVisible && x.DataSeries.HasValues))
                {
                    IRange xVisibleRange;

                    // get corresponding X range, if doesn't exist in xRanges,
                    // take current VisibleRange
                    if (xRanges != null && xRanges.ContainsKey(rSeries.XAxisId))
                    {
                        xVisibleRange = xRanges[rSeries.XAxisId];
                    }
                    else
                    {
                        var xAxis = rSeries.XAxis ?? ParentSurface.XAxes.GetAxisById(rSeries.XAxisId, true);
                        xVisibleRange = xAxis.VisibleRange;
                    }

                    // if X range is valid, get Y indicies range
                    if (xVisibleRange != null && xVisibleRange.IsDefined)
                    {
                        var range = rSeries.GetYRange(xVisibleRange, IsLogarithmicAxis).AsDoubleRange();
                        if (range.IsDefined)
                        {
                            // merge all Y ranges to get maximal
                            maxRange = range.Union(maxRange);
                        }
                    }
                }

                if (maxRange.IsZero)
                    maxRange = CoerceZeroRange(maxRange);

                if (GrowBy != null)
                {
                    var logBase = IsLogarithmicAxis ? ((ILogarithmicAxis)this).LogarithmicBase : 0;
                    maxRange = (maxRange != null ? maxRange.GrowBy(GrowBy.Min, GrowBy.Max, IsLogarithmicAxis, logBase) : null);
                }

                if (VisibleRangeLimit != null)
                {
                    maxRange.ClipTo(VisibleRangeLimit.AsDoubleRange(),VisibleRangeLimitMode);
                }
            }

            return (maxRange != null && maxRange.IsDefined) ? maxRange : VisibleRange != null ? VisibleRange.AsDoubleRange() : null;
        } 
         
        /// <summary>
        /// Scrolls current <see cref="VisibleRange" /> by the specified number of pixels
        /// </summary>
        /// <param name="pixelsToScroll">Scroll N pixels from the current visible range</param>
        /// <param name="clipMode">Defines how scrolling behaves when you reach the edge of the Axis extents.
        /// e.g. ClipMode.ClipAtExtents prevents panning outside of the Axis, ClipMode.None allows panning outside</param>
        public void Scroll(double pixelsToScroll, ClipMode clipMode)
        {
            Scroll(pixelsToScroll, clipMode, TimeSpan.Zero);
        }

        /// <summary>
        /// Scrolls current <see cref="VisibleRange" /> by the specified number of pixels with the specified animation duration
        /// </summary>
        /// <param name="pixelsToScroll">Scroll N pixels from the current visible range</param>
        /// <param name="clipMode">Defines how scrolling behaves when you reach the edge of the Axis extents.
        /// e.g. ClipMode.ClipAtExtents prevents panning outside of the Axis, ClipMode.None allows panning outside</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        public void Scroll(double pixelsToScroll, ClipMode clipMode, TimeSpan duration)
        {
            var interactivityHelper = GetCurrentInteractivityHelper();
            if (interactivityHelper == null) return;

            var translatedRange = interactivityHelper.Scroll(VisibleRange, pixelsToScroll);

            var clippedRange = translatedRange;
            if (clipMode != ClipMode.None)
            {
                var maximumRange = GetMaximumRange();
                clippedRange = interactivityHelper.ClipRange(translatedRange, maximumRange, clipMode);
            }
            
            var newRange = RangeFactory.NewWithMinMax(VisibleRange, clippedRange.Min, clippedRange.Max);

            TryApplyVisibleRangeLimit(newRange);

            this.TrySetOrAnimateVisibleRange(newRange, duration);
        }

        protected void TryApplyVisibleRangeLimit(IRange newRange)
        {
            if (VisibleRangeLimit != null)
            {
                newRange.ClipTo(VisibleRangeLimit, VisibleRangeLimitMode);
            }
        }

        /// <summary>
        /// Translates current <see cref="VisibleRange" /> by the specified number of datapoints
        /// </summary>
        /// <param name="pointAmount">Amount of data points that the start visible range is scrolled by</param>
        /// <remarks>
        /// For XAxis only,  is suitable for <see cref="CategoryDateTimeAxis" />, <see cref="DateTimeAxis" /> and <see cref="NumericAxis" />
        /// where data is regularly spaced
        /// </remarks>
        public void ScrollByDataPoints(int pointAmount)
        {
            ScrollByDataPoints(pointAmount, TimeSpan.Zero);
        }

        /// <summary>
        /// Translates current <see cref="VisibleRange" /> by the specified number of datapoints with the specified animation duration
        /// </summary>
        /// <param name="pointAmount">Amount of points that the start visible range is scrolled by</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        /// <exception cref="System.InvalidOperationException">ScrollXRange is only valid on the X Axis</exception>
        /// <remarks>
        /// For XAxis only,  is suitable for <see cref="CategoryDateTimeAxis" />, <see cref="DateTimeAxis" /> and <see cref="NumericAxis" />
        /// where data is regularly spaced
        /// </remarks>
        public virtual void ScrollByDataPoints(int pointAmount, TimeSpan duration)
        {
            throw new InvalidOperationException("ScrollByDataPoints is only valid CategoryDateTimeAxis");            
        }

        /// <summary>
        /// Performs zoom on current <see cref="IAxis" />, using <paramref name="fromCoord" /> as a coordinate of new range start and
        /// <paramref name="toCoord" /> as a coordinate of new range end
        /// </summary>
        /// <param name="fromCoord">The coordinate of new range start in pixels</param>
        /// <param name="toCoord">The coordinate of new range end in pixels</param>
        public void Zoom(double fromCoord, double toCoord)
        {
            Zoom(fromCoord, toCoord, TimeSpan.Zero);
        }

        /// <summary>
        /// Performs zoom on current <see cref="IAxis" />, using <paramref name="fromCoord" /> as a coordinate of new range start and
        /// <paramref name="toCoord" /> as a coordinate of new range end
        /// </summary>
        /// <param name="fromCoord">The coordinate of new range start in pixels</param>
        /// <param name="toCoord">The coordinate of new range end in pixels</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        public void Zoom(double fromCoord, double toCoord, TimeSpan duration)
        {
            var interactivityHelper = GetCurrentInteractivityHelper();

            var newRange = interactivityHelper.Zoom(VisibleRange, fromCoord, toCoord);

            TryApplyVisibleRangeLimit(newRange);

            this.TrySetOrAnimateVisibleRange(newRange, duration);
        }

        /// <summary>
        /// Performs zoom on current <see cref="IAxis" />, using <paramref name="minFraction" /> as a multiplier of range start and
        /// <paramref name="maxFraction" /> as a multiplier of range end
        /// </summary>
        /// <param name="minFraction">The multiplier of range start</param>
        /// <param name="maxFraction">The multiplier of range end</param>
        public void ZoomBy(double minFraction, double maxFraction)
        {
            ZoomBy(minFraction, maxFraction, TimeSpan.Zero);
        }

        /// <summary>
        /// Performs zoom on current <see cref="IAxis" />, using <paramref name="minFraction" /> as a multiplier of range start and
        /// <paramref name="maxFraction" /> as a multiplier of range end
        /// </summary>
        /// <param name="minFraction">The multiplier of range start</param>
        /// <param name="maxFraction">The multiplier of range end</param>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        public void ZoomBy(double minFraction, double maxFraction, TimeSpan duration)
        {
            var interactivityHelper = GetCurrentInteractivityHelper();

            if (interactivityHelper == null)
                return;

            var newRange = interactivityHelper.ZoomBy(VisibleRange, minFraction, maxFraction);

            TryApplyVisibleRangeLimit(newRange);

            this.TrySetOrAnimateVisibleRange(newRange, duration);
        }

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels
        /// </summary>
        /// <param name="startVisibleRange">The start visible range</param>
        /// <param name="pixelsToScroll">Scroll N pixels from the start visible range</param>
        [Obsolete("AxisBase.ScrollTo is obsolete, please call AxisBase.Scroll(pixelsToScroll) instead")]
        public virtual void ScrollTo(IRange startVisibleRange, double pixelsToScroll)
        {
            ScrollToWithLimit(startVisibleRange, pixelsToScroll, null);
        }

        /// <summary>
        /// From the starting <see cref="IRange"/>, scrolls to a new range by the specified number of pixels, with the specified range limit
        /// </summary>
        /// <param name="startVisibleRange">The start visible range</param>
        /// <param name="pixelsToScroll">Scroll N pixels from the start visible range</param>
        /// <param name="rangeLimit">The range limit.</param>
        public virtual void ScrollToWithLimit(IRange startVisibleRange, double pixelsToScroll, IRange rangeLimit)
        {
            var interactivityHelper = GetCurrentInteractivityHelper();

            var translatedRange = interactivityHelper.Scroll(VisibleRange, pixelsToScroll);

            IRange newRange;
            if(rangeLimit == null)
            {
                newRange = translatedRange;
            }
            else
            {
                var doubleRange = translatedRange.AsDoubleRange();
                newRange = RangeFactory.NewWithMinMax(VisibleRange, doubleRange.Min, doubleRange.Max,
                                                      rangeLimit);
            }

            if (IsValidRange(newRange))
            {
                VisibleRange = newRange;
            }
        }

        /// <summary>
        /// Checks whether <paramref name="range"/> is valid visible range for this axis
        /// </summary>
        /// <param name="range"></param>
        public virtual bool IsValidRange(IRange range)
        {
            var isValid = IsOfValidType(range) &&
                          range.IsDefined &&
                          range.Min.CompareTo(range.Max) <= 0;

            return isValid;
        }

        /// <summary>
        /// Checks whether <paramref name="range"/> is not Null and is of valid type for this axis
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public abstract bool IsOfValidType(IRange range);
          
        /// <summary>
        /// Returns an XmlSchema that describes the XML representation of the object that is produced by the WriteXml method and consumed by the ReadXml method
        /// </summary>
        /// <remarks>
        /// This method is reserved by <see cref="System.Xml.Serialization.IXmlSerializable"/> and should not be used
        /// </remarks>
        /// <returns></returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates <see cref="AxisBase"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == GetType().Name)
            {
                AxisSerializationHelper.Instance.DeserializeProperties(this, reader);
            }
        }

        /// <summary>
        /// Converts <see cref="AxisBase"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            AxisSerializationHelper.Instance.SerializeProperties(this, writer);
        }


        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public abstract IAxis Clone();

        /// <summary>
        /// Asserts the type passed in is supported by the current axis implementation
        /// </summary>
        /// <param name="dataType"></param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public virtual void AssertDataType(Type dataType)
        {
            var supportedTypes = GetSupportedTypes();
            if (!supportedTypes.Contains(dataType))
            {
                throw new InvalidOperationException(
                    string.Format("{0} does not support the type {1}. Supported types include {2}",
                                  GetType().Name, dataType, string.Join(", ", supportedTypes.Select(x => x.Name).ToArray())));
            }
        }

        /// <summary>
        /// Returns a list of types which current axis is designed to work with
        /// </summary>
        /// <returns></returns>
        protected abstract List<Type> GetSupportedTypes();

        /// <summary>
        /// Called to check if the axis properties are valid for rendering. Will throw an exception if not
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if any property is invalid for drawing</exception>
        public void ValidateAxis()
        {
            if (!AutoTicks &&
                ((MajorDelta == null || MajorDelta.Equals(MajorDeltaProperty.GetMetadata(typeof(AxisBase)).DefaultValue)) ||
                 (MinorDelta == null || MinorDelta.Equals(MinorDeltaProperty.GetMetadata(typeof(AxisBase)).DefaultValue))))
            {
                throw new InvalidOperationException("The MinDelta, MaxDelta properties have to be set if AutoTicks == False.");
            }
        }

        /// <summary>
        /// Sets the cursor for this Axis
        /// </summary>
        /// <param name="cursor">The Cursor instance</param>
        /// <remarks></remarks>
        public void SetMouseCursor(Cursor cursor)
        {
            this.SetCurrentValue(CursorProperty, cursor);
        }
        
        /// <summary>
        /// Clears axis labels, ticks off this axis
        /// </summary>
        public void Clear()
        {
            if (AxisPanel != null)
            {
                AxisPanel.ClearLabels();
            }
        }

        /// <summary>
        /// Returns the current <see cref="IAxisInteractivityHelper"/>, valid for the current render pass, which may be used to 
        /// interact with the axis (Scroll, Zoom, Pan). 
        /// </summary>
        /// <seealso cref="IAxisInteractivityHelper"/>
        /// <returns></returns>
        public IAxisInteractivityHelper GetCurrentInteractivityHelper()
        {
            return _currentInteractivityHelper;
        }

        /// <summary>
        /// Gets the current <see cref="ICoordinateCalculator{T}"/> for this Axis, based on current Visible Range and axis type
        /// </summary>
        /// <returns></returns>
        public virtual ICoordinateCalculator<double> GetCurrentCoordinateCalculator()
        {
            var factory = Services == null ? new CoordinateCalculatorFactory() : Services.GetService<ICoordinateCalculatorFactory>();

            _currentCoordinateCalculator = factory.New(GetAxisParams());
            _currentInteractivityHelper = new AxisInteractivityHelper(_currentCoordinateCalculator);

            return _currentCoordinateCalculator;
        }

        /// <summary>
        /// Called internally immediately before a render pass begins
        /// </summary>
        public virtual void OnBeginRenderPass(RenderPassInfo renderPassInfo = default(RenderPassInfo), IPointSeries firstPointSeries = null)
        {
            GetCurrentCoordinateCalculator();
        }

        /// <summary>
        /// Gets an <see cref="AxisParams"/> struct with info about the current axis setup
        /// </summary>
        /// <returns></returns>
        public virtual AxisParams GetAxisParams()
        {
            var renderSurface = RenderSurface;
            var visibleRange = (VisibleRange ?? new DoubleRange(double.NaN, double.NaN)).AsDoubleRange();            

            var axisSize = (int)(IsHorizontalAxis ? ActualWidth : ActualHeight);
            
            if (Math.Abs(axisSize) < double.Epsilon && renderSurface != null)
            {
                axisSize = (int)(IsHorizontalAxis ? renderSurface.ActualWidth : renderSurface.ActualHeight);
            }

            var axisParams = new AxisParams
                             {
                                 FlipCoordinates = FlipCoordinates,
                                 IsXAxis = IsXAxis,
                                 IsHorizontal = IsHorizontalAxis,
                                 VisibleMax = visibleRange.Max.ToDouble(),
                                 VisibleMin = visibleRange.Min.ToDouble(),
                                 Offset = GetAxisOffset(),
                                 Size = axisSize,
                                 DataPointPixelSize = double.NaN,
                             };

            var baseDataSeries = GetBaseDataSeries();

            if (baseDataSeries != null)
            {
                axisParams.BaseXValues = baseDataSeries.XValues;
                axisParams.IsBaseXValuesSorted = baseDataSeries.IsSorted;
            }

            return axisParams;
        }

        /// <summary>
        /// Returns the offset of the axis relative to the <see cref="RenderSurface"/>.
        /// Is used for cases where axes are vertically or horizontally stacked.
        /// </summary>
        public virtual double GetAxisOffset()
        {
            var offset = 0d;

            var renderSurface = RenderSurface != null;
            if (renderSurface)
            {
                var relativeBounds = ElementExtensions.GetBoundsRelativeTo(this, RenderSurface);

                offset = relativeBounds == Rect.Empty ? 0d : (float)(IsHorizontalAxis ? relativeBounds.X : relativeBounds.Y);
            }

            return offset;
        }

        private IDataSeries GetBaseDataSeries()
        {
            IDataSeries baseDataSeries = null;

            if (ParentSurface != null && ParentSurface.RenderableSeries != null)
            {
                var firstSeries = IsXAxis
                    ? ParentSurface.RenderableSeries.FirstOrDefault(x => x.XAxisId == Id && x.DataSeries?.HasValues == true)
                    : ParentSurface.RenderableSeries.FirstOrDefault(x => x.YAxisId == Id && x.DataSeries?.HasValues == true);

                if (firstSeries != null && firstSeries.DataSeries != null)
                {
                    baseDataSeries = firstSeries.DataSeries;
                }
            }

            return baseDataSeries;
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
        /// Asynchronously requests that the element redraws itself plus children.
        /// Will be ignored if the element is ISuspendable and currently IsSuspended (within a SuspendUpdates/ResumeUpdates call)
        /// </summary>
        /// <remarks></remarks>
        public void InvalidateElement()
        {
            InvalidateMeasure();
            InvalidateArrange();

            if (ParentSurface != null)
            {
                ParentSurface.InvalidateElement();
            }
        }
        
        /// <summary>
        /// Performs a hit test on the Axis, returning the Data Value at the specific x or y pixel coordinate. This operation is the opposite of <see cref="AxisBase.GetCoordinate"/>
        /// </summary>
        /// <remarks>If the Axis is an XAxis, the coordinate passed in is an X-pixel. If the axis is a Y Axis, the coordinate is a Y-pixel</remarks>
        /// <param name="atPoint">The pixel coordinate on this Axis corresponding to the input DataValue</param>
        /// <returns>An <see cref="AxisInfo"/> struct containing the datavalue and formatted data value at this coordinate</returns>
        public virtual AxisInfo HitTest(Point atPoint)
        {
            var coordCalculator = GetCurrentCoordinateCalculator();
            if (coordCalculator == null)
                return default(AxisInfo);

            var dataValue = GetDataValue(IsHorizontalAxis ? atPoint.X : atPoint.Y);

            return HitTest(dataValue);
        }

        /// <summary>
        /// Performs a HitTest operation on the <see cref="AxisBase" />. The supplied <paramref name="dataValue" /> is used to convert to <see cref="AxisInfo" /> struct, which contains information about the axis, as well as formatted values
        /// </summary>
        /// <param name="dataValue">The data value.</param>
        /// <returns>The <see cref="AxisInfo"/> result</returns>
        public virtual AxisInfo HitTest(IComparable dataValue)
        {
            string formattedValue = FormatText(dataValue);
            string cursorFormattedValue = FormatCursorText(dataValue);

            return new AxisInfo
            {
                AxisId = Id,
                DataValue = dataValue,
                AxisAlignment = AxisAlignment,
                AxisFormattedDataValue = formattedValue,
                CursorFormattedDataValue = cursorFormattedValue,
                AxisTitle = AxisTitle,
                IsHorizontal = IsHorizontalAxis,
                IsXAxis = IsXAxis
            };
        }

        /// <summary>
        /// String formats the text, using the <see cref="TextFormatting"/> property as a formatting string
        /// </summary>
        /// <param name="value">The data value to format</param>
        /// <param name="format">A composite format string</param>
        /// <returns>The string formatted data value</returns>
        [Obsolete("The FormatText method which takes a format string is obsolete. Please use the method overload with one argument instead.", true)]
        public virtual string FormatText(IComparable value, string format)
        {
            throw new NotSupportedException("The FormatText method which takes a format string is obsolete. Please use the method overload with one argument instead.");
        }

        /// <summary>
        /// String formats the text, using the <see cref="TextFormatting"/> property as a formatting string
        /// </summary>
        /// <param name="value">The data value to format</param>
        /// <returns>The string formatted data value</returns>
        public virtual string FormatText(IComparable value)
        {
            return LabelProvider != null ? LabelProvider.FormatLabel(value) : value.ToString();
        }

        /// <summary>
        /// String formats text for the cursor, using the <see cref="CursorTextFormatting"/> property as a formatting string
        /// </summary>
        /// <param name="value">The data value to format</param>
        /// <returns>The string formatted data value</returns>
        public virtual string FormatCursorText(IComparable value)
        {
            return LabelProvider != null ? LabelProvider.FormatCursorLabel(value) : value.ToString();
        }

        /// <summary>
        /// Transforms a pixel coordinate into a data value for this axis. 
        /// </summary>
        /// <param name="pixelCoordinate"></param>
        /// <returns></returns>
        public virtual IComparable GetDataValue(double pixelCoordinate)
        {
            if (_currentCoordinateCalculator == null)
                return double.NaN;

            return _currentCoordinateCalculator.GetDataValue(pixelCoordinate);
        }

        /// <summary>
        /// Given the Data Value, returns the x or y pixel coordinate at that value on the Axis. This operation is the opposite of <see cref="AxisBase.HitTest(Point)"/>
        /// </summary>
        /// <remarks>If the Axis is an XAxis, the coordinate returned is an X-pixel. If the axis is a Y Axis, the coordinate returned is a Y-pixel</remarks>
        /// <param name="value">The DataValue as input</param>
        /// <returns>The pixel coordinate on this Axis corresponding to the input DataValue</returns>
        /// <example>
        /// Given an axis with a VisibleRange of 1..10 and height of 100, a value of 7 passed in to GetCoordinate would return 70 pixels
        ///   </example>
        /// <remarks></remarks>
        public virtual double GetCoordinate(IComparable value)
        {
            if (_currentCoordinateCalculator == null)
                return double.NaN;

            return _currentCoordinateCalculator.GetCoordinate(value.ToDouble());
        }

        /// <summary>
        /// Returns true if the Point is within the bounds of the current HitTestable element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>true if the Point is within the bounds</returns>
        /// <remarks></remarks>
        public bool IsPointWithinBounds(Point point)
        {
            var tPoint = ParentSurface.RootGrid.TranslatePoint(point, this); 

            return ElementExtensions.IsPointWithinBounds(this, tPoint);
        }

        /// <summary>
        /// Translates the point relative to the other hittestable element
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            return ElementExtensions.TranslatePoint(this, point, relativeTo);
        }

        /// <summary>
        /// Gets the bounds of the current HitTestable element relative to another HitTestable element
        /// </summary>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            return ElementExtensions.GetBoundsRelativeTo(this, relativeTo);
        }

        /// <summary>
        /// Called when the instance is drawn
        /// </summary>
        /// <param name="renderContext">The <see cref="IRenderContext2D"/> used for drawing</param>
        /// <param name="renderPassData">Contains arguments and parameters for this render pass</param>
        /// <seealso cref="IDrawable"/>
        /// <seealso cref="IRenderContext2D"/>
        public void OnDraw(IRenderContext2D renderContext, IRenderPassData renderPassData)
        {
            if (IsValidForDrawing())
            {
                // Prevent nested calls to InvalidateElement
                using (var suspender = SuspendUpdates())
                {
                    suspender.ResumeTargetOnDispose = false;

                    var stopWatch = Stopwatch.StartNew();

                    if (LabelProvider != null)
                    {
                        LabelProvider.OnBeginAxisDraw();
                    }

                    _tickCoords = CalculateTicks();

                    DrawGridLines(renderContext, _tickCoords);

                    if (IsShown())
                    {
                        OnDrawAxis(_tickCoords);
                    }

                    stopWatch.Stop();

                    UltrachartDebugLogger.Instance.WriteLine("Drawn {0}: Width={1}, Height={2} in {3}ms", GetType().Name,
                        ActualWidth, ActualHeight, stopWatch.ElapsedMilliseconds);
                }
            }
        }

        bool IsValidForDrawing()
        {
            return !IsSuspended && IsVisibleRangeValid() && IsLicenseValid;
        }

        bool IsShown()
        {
            // Requires a check for ParentSurface.Visibility here
            // in order to prevent the AxisPanel to call DrawTickLabels(..) during layout pass
            var scs = ParentSurface as UltrachartSurface;

            return scs != null && scs.IsVisible() && this.IsVisible() && HasAxisPanel();
        }

        /// <summary>
        /// Checks if the VisibleRange is valid, e.g. is not null, is not NaN, the difference between Max and Min is positive
        /// </summary>
        /// <returns></returns>
        private bool IsVisibleRangeValid()
        {
            bool isVisibleRangeValid = IsValidRange(VisibleRange) && !VisibleRange.IsZero;

            if (!isVisibleRangeValid)
            {
                UltrachartDebugLogger.Instance.WriteLine("{0} is not a valid VisibleRange for {1}",
                                                       VisibleRange, GetType());
            }

            return isVisibleRangeValid;
        }

        /// <summary>
        /// Overridden by derived types, called internal to calculate MinorTicks, MajorTicks before Axis drawing
        /// </summary>
        /// <remarks></remarks>
        protected virtual TickCoordinates CalculateTicks()
        {
            CalculateDelta();

            Guard.NotNull(TickProvider, "TickProvider");

            var axisParams = (IAxisParams) this;

            var majorTicks = TickProvider.GetMajorTicks(axisParams);
            var minorTicks = TickProvider.GetMinorTicks(axisParams);

            return TickCoordinatesProvider.GetTickCoordinates(minorTicks, majorTicks);
        }

        /// <summary>
        /// Calcuates the delta's for use in this render pass
        /// </summary>
        protected abstract void CalculateDelta();

        /// <summary>
        /// Returns an instance of an <see cref="IDeltaCalculator"/> which is used to compute the data-values of <see cref="MajorDelta"/>, <see cref="MinorDelta"/>. 
        /// Overridden by derived types to allow calculations specific to that axis type.
        /// </summary>
        /// <returns>An <see cref="IDeltaCalculator"/> instance</returns>
        protected abstract IDeltaCalculator GetDeltaCalculator();

        /// <summary>
        /// Calculates max auto ticks amount, which is >= 1
        /// </summary>
        /// <returns></returns>
        protected virtual uint GetMaxAutoTicks()
        {
            return (uint)Math.Max(1, MaxAutoTicks);
        }

        /// <summary>
        /// Called internal to draw gridlines before Axis drawing
        /// </summary>
        /// <remarks></remarks>
        protected virtual void DrawGridLines(IRenderContext2D renderContext, TickCoordinates tickCoords)
        {
            if (renderContext == null)
                return;

            // Draw minor grid lines
            if (DrawMinorGridLines && tickCoords.MinorTickCoordinates.Length > 0)
            {
                renderContext.Layers[RenderLayer.AxisMinorGridlines].Enqueue(
                    () => DrawGridLine(renderContext, MinorGridLineStyle, tickCoords.MinorTickCoordinates));
            }

            if (tickCoords.MajorTickCoordinates.Length > 0)
            {
                // Draw major bands (fill areas between axis major gridlines)
                if (DrawMajorBands)
                {
                    renderContext.Layers[RenderLayer.AxisBands].Enqueue(
                        () => DrawBand(renderContext, tickCoords.MajorTicks, tickCoords.MajorTickCoordinates));
                }

                // Draw major grid lines
                if (DrawMajorGridLines)
                {
                    renderContext.Layers[RenderLayer.AxisMajorGridlines].Enqueue(
                        () => DrawGridLine(renderContext, MajorGridLineStyle, tickCoords.MajorTickCoordinates));
                }
            }
        }

        private void DrawBand(IRenderContext2D renderContext, double[] ticks, float[] ticksCoords)
        {
            var renderSurface = RenderSurface;
            if (renderSurface == null) return;

            var direction = IsHorizontalAxis ? XyDirection.XDirection : XyDirection.YDirection;

            using (var bandBrush = renderContext.CreateBrush(AxisBandsFill))
            {
                var offset = (float)GetAxisOffset();

                var min = IsHorizontalAxis ? 0f : offset + (float)ActualHeight;
                var max = IsHorizontalAxis ? (float)renderSurface.ActualWidth : offset;

                // Case where the axis is flipped, swap the min/max coords so we draw bands correctly
                if (FlipCoordinates ^ IsAxisFlipped)
                {
                    NumberUtil.Swap(ref min, ref max);
                }

                // Even/Odd calculation helps preserve order of bands as you scroll
                var firstTickIndex = GetMajorTickIndex(ticks[0]);

                var isEven = firstTickIndex % 2 == 0;

                for (int i = 0; i < ticksCoords.Length; i++)
                {
                    if (isEven)
                    {
                        // Draw the first band to bottom of the chart
                        var coord0 = i == 0 ? min : ticksCoords[i - 1];
                        var coord1 = ticksCoords[i];

                        DrawBand(renderContext, bandBrush, direction, coord0, coord1);
                    }

                    isEven = !isEven;
                }

                // Draw the last band to the edge of the screen
                if (isEven)
                {
                    DrawBand(renderContext, bandBrush, direction, max,
                             ticksCoords.Last());
                }
            }
        }

        private void DrawBand(IRenderContext2D renderContext, IBrush2D bandBrush, XyDirection direction, float coord0, float coord1)
        {
            var renderSurface = RenderSurface;
            if (renderSurface == null) return;

            var topLeft = direction == XyDirection.YDirection ? new Point(0, coord0) : new Point(coord0, 0);
            var bottomRight = direction == XyDirection.YDirection
                ? new Point(renderSurface.ActualWidth, coord1)
                : new Point(coord1, renderSurface.ActualHeight);

            renderContext.FillRectangle(bandBrush, topLeft, bottomRight, 0d);
        }

        protected virtual void DrawGridLine(IRenderContext2D renderContext, Style gridLineStyle, IEnumerable<float> coordsToDraw)
        {
            var direction = IsHorizontalAxis ? XyDirection.XDirection : XyDirection.YDirection;

            LineToStyle.Style = gridLineStyle;
            ThemeManager.SetTheme(LineToStyle, ThemeManager.GetTheme(this));

            using (var linePen = renderContext.GetStyledPen(LineToStyle))
            {
                if (linePen == null || linePen.Color.A == 0) return;

                foreach (var coord in coordsToDraw)
                {
                    DrawGridLine(renderContext, linePen, direction, coord);
                }
            }
        }

        /// <summary>
        /// Draws a single grid line on the <see cref="RenderSurface"/>, using the specified Style (TargetType <see cref="Line"/>), <see cref="XyDirection"/> and integer coordinate.
        /// </summary>
        /// <remarks>If direction is <see cref="XyDirection.XDirection"/>, the coodinate is an X-coordinate, else it is a Y-coordinate</remarks>
        /// <param name="renderContext">The <see cref="IRenderContext2D"/> instance to draw to</param>
        /// <param name="linePen">The pen (TargetType <see cref="IPen2D"/>) to apply to the grid line</param>
        /// <param name="direction">The X or Y direction to draw the  </param>
        /// <param name="atPoint">The integer coordinate to draw at. If direction is <see cref="XyDirection.XDirection"/>, the coodinate is an X-coordinate, else it is a Y-coordinate</param>
        protected void DrawGridLine(IRenderContext2D renderContext, IPen2D linePen, XyDirection direction, float atPoint)
        {
            var renderSurface = RenderSurface;
            if (renderSurface == null) return;

            var strokeThickness = linePen.StrokeThickness;
            // Add strokeThickness to cut off line's round ends
            var pt1 = direction == XyDirection.YDirection
                ? new Point(-strokeThickness, atPoint)
                : new Point(atPoint, -strokeThickness);
            var pt2 = direction == XyDirection.YDirection
                ? new Point(renderSurface.ActualWidth + strokeThickness, atPoint)
                : new Point(atPoint, renderSurface.ActualHeight + strokeThickness);

            renderContext.DrawLine(linePen, pt1, pt2);
        }

        /// <summary>
        /// Called when the axis should redraw itself. 
        /// </summary>
        /// <param name="tickCoords"> </param>
        protected virtual void OnDrawAxis(TickCoordinates tickCoords)
        {
            _offset = (float)GetOffsetForLabels();

            AxisPanel.DrawTicks(tickCoords, _offset);

            if (!DrawLabels)
            {
                Clear();
            }

            AxisPanel.Invalidate();
        }

        /// <summary>
        /// Returns an offset for the axis
        /// </summary>
        /// <returns></returns>
        protected virtual double GetOffsetForLabels()
        {
            var offset = IsHorizontalAxis ? BorderThickness.Left : BorderThickness.Top;
            offset += GetAxisOffset();

            return offset;
        }

        /// <summary>
        /// Draws the ticks and gridlines during a render pass
        /// </summary>
        /// <param name="canvas">The canvas to draw labels on.</param>
        /// <param name="tickCoords">The tick coords containing all coordinates for ticks and gridlines.</param>
        /// <param name="offset"></param>
        protected virtual void DrawTickLabels(AxisCanvas canvas, TickCoordinates tickCoords, float offset)
        {
            var values = tickCoords.MajorTicks;
            var coords = tickCoords.MajorTickCoordinates;

            if (values == null || coords == null) return;

            var toRemove = canvas.Children.Count - coords.Length;

            if (toRemove > 0)
            {
                using (canvas.SuspendUpdates())
                {
                    for (int i = toRemove - 1; i >= 0; --i)
                    {
                        var index = coords.Length + i;

                        RemoveTickLabel(canvas, index);
                    }
                }
            }

            for (int i = 0; i < coords.Length; i++)
            {
                var tick = values[i];

                var tickLabel = i < canvas.Children.Count
                    ? (DefaultTickLabel)canvas.Children[i]
                    : _labelsPool.Get(ApplyStyle);

                var dataValue = ConvertTickToDataValue(tick);
                UpdateAxisLabel(tickLabel, dataValue);

                // labels with higher priority have more chances not to be culled during layout
                tickLabel.CullingPriority = CalculateLabelCullingPriority(tick);

                var position = GetLabelPosition(offset, coords[i]);
                tickLabel.Position = position;

                canvas.SafeAddChild(tickLabel);
            }
        }

        protected virtual Point GetLabelPosition(float offset, float coords)
        {
            var coordToDraw = coords - offset;
            var xCoord = IsHorizontalAxis ? coordToDraw : 0d;
            var position = new Point(xCoord, coordToDraw - xCoord);
            return position;
        }

        protected void RemoveTickLabel(AxisCanvas canvas, int index)
        {
            var label = (DefaultTickLabel) canvas.Children[index];
            canvas.Children.Remove(label);

            _labelsPool.Put(label);
        }

        /// <summary>
        /// When overridden in a derived class, converts a tick value to a data value. For instance, this may be overridden in the
        /// <see cref="CategoryDateTimeAxis"/> to convert between indices and DateTimes
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual IComparable ConvertTickToDataValue(IComparable value)
        {
            return value;
        }

        private void UpdateAxisLabel(DefaultTickLabel label, IComparable value)
        {
            var lblProvider = LabelProvider;
            if (lblProvider == null) return;

            label.DataContext = label.DataContext == null
                ? lblProvider.CreateDataContext(value)
                : lblProvider.UpdateDataContext((ITickLabelViewModel)label.DataContext, value);
        }

        private int CalculateLabelCullingPriority(double tick)
        {
            var tickNum = GetMajorTickIndex(tick);

            var cullingPriority = LabelCullingDistances.Count(i => tickNum%i == 0);

            return cullingPriority;
        }

        /// <summary>
        /// Returns major tick index e.g value 0 has index #0, 0 + MajorDelta - #1, 0 + 2*MajorDelta - #2 etc...
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        private decimal GetMajorTickIndex(double tick)
        {
            var categoryCalc = _currentCoordinateCalculator as ICategoryCoordinateCalculator;
            if (categoryCalc != null)
            {
                tick = categoryCalc.TransformDataToIndex(tick);
            }
            
            var step = MajorDelta.ToDouble();

            if (IsLogarithmicAxis)
            {
                var axis = this as ILogarithmicAxis;
                tick = Math.Log(tick, axis.LogarithmicBase);
            }

            var index = tick / step;

            // Prevents int overflow
            if (index >= int.MaxValue)
            {
                index = index / int.MaxValue;
                index -= (int)index;

                index = index * int.MaxValue;
            }

            var tickMajorIndex = (int)index.RoundOff();

            return tickMajorIndex;
        }
        
        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="System.Windows.FrameworkElement.OnApplyTemplate()"/>.
        /// </summary>
        /// <remarks></remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _axisContainer = GetAndAssertTemplateChild<StackPanel>("PART_AxisContainer");
            _axisPanel = GetAndAssertTemplateChild<IAxisPanel>("PART_AxisCanvas");

            ((AxisPanel)_axisPanel).AddLabels = canvas =>
            {
                if (IsValidForDrawing() && IsShown())
                {
                    DrawTickLabels(canvas, _tickCoords, _offset);
                }
            };

            _modifierAxisCanvas = GetAndAssertTemplateChild<ModifierAxisCanvas>("PART_ModifierAxisCanvas");
            _modifierAxisCanvas.ParentAxis = this;

#if SILVERLIGHT
            // Fix for SC-1677
            // caused by changing AxisAlignment before adding elements to stack
            Ecng.Xaml.Charting.Common.AxisLayoutHelper.UpdateItemsOrder(AxisContainer);            
#endif

            // Fix for VisibleRange DependencyProperty Precdence issue (Create Multi Pane Stock Charts example). 
            if (VisibleRange == null)
                this.SetCurrentValue(VisibleRangeProperty, GetUndefinedRange());      
        }

        protected T GetAndAssertTemplateChild<T>(string childName) where T : class
        {
            var templateChild = GetTemplateChild(childName) as T;
            if (templateChild == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Unable to Apply the Control Template. {0} is missing or of the wrong type",
                    childName));
            }
            return templateChild;
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

                InvalidateParentSurface();

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

        /// <summary>
        /// Raises the VisibleRangeChanged event
        /// </summary>
        /// <param name="args">The <see cref="VisibleRangeChangedEventArgs"/> containing event data</param>
        protected virtual void OnVisibleRangeChanged(VisibleRangeChangedEventArgs args)
        {
            if (ParentSurface != null)
            {
                var viewportManager = ParentSurface.ViewportManager;
                if (viewportManager != null)
                {
                    viewportManager.OnVisibleRangeChanged(this);
                }
            }

            var handler = VisibleRangeChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        internal static void NotifyDataRangeChanged(IAxis target)
        {
            var axis = target as AxisBase;
            if (axis != null)
            {
                axis.OnDataRangeChanged();
                axis.OnPropertyChanged("DataRange");
            }
        }

        private void OnDataRangeChanged()
        {
            var handler = DataRangeChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private bool IsVisibleRangeMinimalConstrainValid()
        {
            return MinimalZoomConstrain == null || VisibleRange.Diff.ToDouble() >= MinimalZoomConstrain.ToDouble();
        }

        /// <summary>
        /// When overridden in derived classes, changes value of the VisibleRange according to axis requirements
        /// before it is applied
        /// </summary>
        protected virtual void CoerceVisibleRange() { }

        private static void OnVisibleRangeLimitDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBase)d);

            var oldRange = e.OldValue as IRange;
            var newRange = e.NewValue as IRange;

            if (oldRange != null)
            {
                oldRange.PropertyChanged -= axis.OnMaxMinVisibleRangeLimitPropertiesChanged;
            }

            if (newRange != null && axis.VisibleRange != null)
            {
                newRange.PropertyChanged += axis.OnMaxMinVisibleRangeLimitPropertiesChanged;

                var clone = (IRange) axis.VisibleRange.Clone();
                clone = clone.ClipTo(newRange, axis.VisibleRangeLimitMode);

                axis.SetCurrentValue(VisibleRangeProperty, clone);
            }
        }

        private void OnMaxMinVisibleRangeLimitPropertiesChanged(object sender, PropertyChangedEventArgs e)
        {
            TryApplyVisibleRangeLimit(VisibleRange);
            
            //Update the binding 
            var expr = GetBindingExpression(VisibleRangeLimitProperty);

            if (expr != null && expr.ParentBinding.UpdateSourceTrigger != UpdateSourceTrigger.Explicit)
            {
                expr.UpdateSource();
#if !SILVERLIGHT
                expr.UpdateTarget();
#endif
            }
        }

        private static void OnAnimatedVisibleRangeDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBase)d);

            if (e.NewValue != null)
            {
                axis.AnimateVisibleRangeTo((IRange) e.NewValue, TimeSpan.FromMilliseconds(500));
            }
        }

        /// <summary>
        /// Animates the visible range of the axis to the destination VisibleRange, over the specified Duration. 
        /// Also see <see cref="AnimatedVisibleRange"/> property which has a default duration of 500ms
        /// </summary>
        /// <param name="to">The end range</param>
        /// <param name="duration">The duration of the animation.</param>
        public void AnimateVisibleRangeTo(IRange to, TimeSpan duration)
        {
            Guard.NotNull(to, "to");

            if (!HasValidVisibleRange)
            {
                VisibleRange = to;
                return;
            }

            _fromPoint = TransformRangeToPoint(VisibleRange);
            var toPoint = TransformRangeToPoint(to);

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

        private Point TransformRangeToPoint(IRange range)
        {
            var doubleRange = range.AsDoubleRange();

            var min = doubleRange.Min;
            var max = doubleRange.Max;

            if (IsLogarithmicAxis)
            {
                var logBase = ((ILogarithmicAxis) this).LogarithmicBase;

                min = Math.Log(min, logBase);
                max = Math.Log(max, logBase);
            }

            return new Point(min, max);
        }

        private static void OnVisibleRangePointDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var p = (Point)e.NewValue;
            var axis = (AxisBase)d;

            // Debug.WriteLine("New Range: Min {0}, Max {1}", p.X, p.Y);
            axis.Dispatcher.BeginInvokeAlways(() =>
                                                    {
                                                        if (axis.VisibleRange != null)
                                                        {
                                                            var min = axis._fromPoint.X + p.X;
                                                            var max = axis._fromPoint.Y + p.Y;

                                                            if (axis.IsLogarithmicAxis)
                                                            {
                                                                var logBase =
                                                                    ((ILogarithmicAxis) axis).LogarithmicBase;

                                                                min = Math.Pow(logBase, min);
                                                                max = Math.Pow(logBase, max);
                                                            }

                                                            axis._isAnimationChange = true;

                                                            axis.VisibleRange =
                                                                RangeFactory.NewRange(axis.VisibleRange.GetType(), min, max);
                                                        }
                                                    });
        }


        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;
            if (axis != null)
            {
                axis.OnPropertyChanged("IsHorizontalAxis");
            }
        }

        private static void OnAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (AxisBase) d;
            var parentSurface = axis.ParentSurface;

            if (parentSurface != null && !axis.IsSuspended)
            {
                parentSurface.OnAxisAlignmentChanged(axis, (AxisAlignment)e.OldValue);
            }

            InvalidateParent(d, e);
        }

        private static void OnIsCenterAxisDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = (AxisBase)d;
            var parentSurface = axis.ParentSurface;

            if (parentSurface != null && !axis.IsSuspended)
            {
                parentSurface.OnIsCenterAxisChanged(axis);
            }

            InvalidateParent(d, e);
        }

        /// <summary>
        /// Provides a DependencyProperty callback which invalidates the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        protected static void InvalidateParent(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = ((AxisBase)d);

            if (axis.HasAxisPanel())
            {
                axis.InvalidateParentSurface();
            }
        }

        private static void OnScrollBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;
            var axisScrollBar = e.NewValue as UltrachartScrollbar;
            if (axisScrollBar != null)
            {
                axisScrollBar.Axis = axis;
            }
        }

        private bool HasAxisPanel()
        {
            return AxisPanel != null;
        }

        private void InvalidateParentSurface()
        {
            if (Services == null || ParentSurface == null || ParentSurface.IsSuspended)
            {
                return;
            }

            Services.GetService<IEventAggregator>().Publish(new InvalidateUltrachartMessage(this));
        }

        private static void OnLabelProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;

            var labelProvider = e.NewValue as ILabelProvider;
            if (labelProvider != null)
            {
                labelProvider.Init(axis);
            }

            InvalidateParent(d, e);
        }

        private static void OnTickProviderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;

            var tickProvider = e.NewValue as ITickProvider;
            if (tickProvider != null)
            {
                tickProvider.Init(axis);
            }

            tickProvider = e.OldValue as ITickProvider;
            if (tickProvider != null)
            {
                tickProvider.Init(null);
            }

            InvalidateParent(d, e);
        }

        private static void OnIsPrimaryAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axis = d as AxisBase;

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
    }
}