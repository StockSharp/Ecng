// *************************************************************************************
// ULTRACHART� � Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartSurface.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Common.Messaging;
using Ecng.Xaml.Charting.Licensing;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Numerics.PointResamplers;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Rendering.HighSpeedRasterizer;
using Ecng.Xaml.Charting.Services;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Threading;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;
using Ecng.Xaml.Licensing.Core;
using TinyMessenger;
using LicenseManager = Ecng.Xaml.Licensing.Core.LicenseManager;
namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Provides a high performance chart surface with a single <see cref="Ecng.Xaml.Charting.Rendering.Common.RenderSurfaceBase"/> viewport 
    /// for rendering multiple <see cref="IRenderableSeries"/>, multiple X and Y <see cref="IAxis"/> instances, 
    /// with <see cref="IDataSeries"/> bindings, mulitple <see cref="ChartModifierBase"/> derived behaviour modifiers and 
    /// multiple <see cref="AnnotationBase"/> UIElement Annotations
    /// </summary>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="AnnotationBase"/>
    /// <seealso cref="ChartModifierBase"/>
    /// <seealso cref="RenderSurfaceBase"/>
    /// <seealso cref="AxisBase"/>
    [UltrachartLicenseProvider(typeof(UltrachartSurfaceLicenseProvider))]
    [TemplatePart(Name = "PART_GridLinesArea", Type = typeof(GridLinesPanel))]
    [TemplatePart(Name = "PART_LeftAxisArea", Type = typeof(AxisArea))]
    [TemplatePart(Name = "PART_TopAxisArea", Type = typeof(AxisArea))]
    [TemplatePart(Name = "PART_RightAxisArea", Type = typeof(AxisArea))]
    [TemplatePart(Name = "PART_BottomAxisArea", Type = typeof(AxisArea))]    
    [TemplatePart(Name = "PART_AnnotationsOverlaySurface", Type = typeof(AnnotationSurface))]
    [TemplatePart(Name = "PART_AnnotationsUnderlaySurface", Type = typeof(AnnotationSurface))]
    [TemplatePart(Name = "PART_ChartAdornerLayer", Type = typeof(Canvas))]
    public class UltrachartSurface : UltrachartSurfaceBase, IUltrachartSurface, IXmlSerializable
    {           
        /// <summary>
        /// Defines the ClipUnderlayAnnotations DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ClipUnderlayAnnotationsProperty = DependencyProperty.Register("ClipUnderlayAnnotations", typeof(bool), typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the ClipOverlayAnnotations DepedencyProperty
        /// </summary>
        public static readonly DependencyProperty ClipOverlayAnnotationsProperty = DependencyProperty.Register("ClipOverlayAnnotations", typeof(bool), typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the ZoomExtentsCommand DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ZoomExtentsCommandProperty = DependencyProperty.Register("ZoomExtentsCommand", typeof(ICommand), typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the ZoomExtentsCommand DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AnimateZoomExtentsCommandProperty = DependencyProperty.Register("AnimateZoomExtentsCommand", typeof(ICommand), typeof(UltrachartSurface), new PropertyMetadata(null));        
        /// <summary>
        /// Defines the XAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty XAxisProperty = DependencyProperty.Register("XAxis", typeof(IAxis), typeof(UltrachartSurface), new PropertyMetadata(null, OnXAxisChanged));
        /// <summary>
        /// Defines the YAxis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty YAxisProperty = DependencyProperty.Register("YAxis", typeof(IAxis), typeof(UltrachartSurface), new PropertyMetadata(null, OnYAxisChanged));
        /// <summary>
        /// Defines the YAxes DependencyProperty
        /// </summary>
        public static readonly DependencyProperty YAxesProperty = DependencyProperty.Register("YAxes", typeof(AxisCollection), typeof(UltrachartSurface), new PropertyMetadata(null, OnYAxesDependencyPropertyChanged));
        /// <summary>
        /// Defines the YAxes DependencyProperty
        /// </summary>
        public static readonly DependencyProperty XAxesProperty = DependencyProperty.Register("XAxes",
                                                                                              typeof(AxisCollection),
                                                                                              typeof(UltrachartSurface),
                                                                                              new PropertyMetadata(
                                                                                                  null,
                                                                                                  OnXAxesDependencyPropertyChanged));
        /// <summary>
        /// Defines the Annotations DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AnnotationsProperty = DependencyProperty.Register("Annotations",
                                                                                                    typeof(
                                                                                                        AnnotationCollection
                                                                                                        ),
                                                                                                    typeof(
                                                                                                        UltrachartSurface),
                                                                                                    new PropertyMetadata
                                                                                                        (OnAnnotationsDependencyPropertyChanged));
        /// <summary>
        /// Defines the AutoRangeOnStartup DependencyProperty
        /// </summary>
        [Obsolete("We're Sorry! The AutoRangeOnStartup property has been deprecated in Ultrachart", true)]
        public static readonly DependencyProperty AutoRangeOnStartupProperty =
            DependencyProperty.Register("AutoRangeOnStartup", typeof(bool), typeof(UltrachartSurface),
                                        new PropertyMetadata(true));
        /// <summary>
        /// Defines the ChartModifier DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ChartModifierProperty = DependencyProperty.Register("ChartModifier",
                                                                                                      typeof(
                                                                                                          IChartModifier
                                                                                                          ),
                                                                                                      typeof(
                                                                                                          UltrachartSurface
                                                                                                          ),
                                                                                                      new PropertyMetadata
                                                                                                          (null,
                                                                                                           OnChartModifierChanged));
        /// <summary>
        /// Defines the LeftAxisPanelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LeftAxesPanelTemplateProperty =
            DependencyProperty.Register("LeftAxesPanelTemplate", typeof(ItemsPanelTemplate), typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the RightAxisPanelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty RightAxesPanelTemplateProperty =
            DependencyProperty.Register("RightAxesPanelTemplate", typeof(ItemsPanelTemplate), typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the RightAxisPanelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TopAxesPanelTemplateProperty =
            DependencyProperty.Register("TopAxesPanelTemplate", typeof(ItemsPanelTemplate), typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the RightAxisPanelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty BottomAxesPanelTemplateProperty =
            DependencyProperty.Register("BottomAxesPanelTemplate", typeof(ItemsPanelTemplate), typeof(UltrachartSurface), new PropertyMetadata(null));
        public static readonly DependencyProperty CenterXAxesPanelTemplateProperty =
            DependencyProperty.Register("CenterXAxesPanelTemplate", typeof(ItemsPanelTemplate), typeof(UltrachartSurface), new PropertyMetadata(default(ItemsPanelTemplate)));
        public static readonly DependencyProperty CenterYAxesPanelTemplateProperty =
            DependencyProperty.Register("CenterYAxesPanelTemplate", typeof (ItemsPanelTemplate), typeof (UltrachartSurface), new PropertyMetadata(default(ItemsPanelTemplate)));
        /// <summary>
        /// Defines the GridLinesPanelStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty GridLinesPanelStyleProperty =
            DependencyProperty.Register("GridLinesPanelStyle", typeof(Style), typeof(UltrachartSurface),
                                        new PropertyMetadata(null, OnChildStyleChanged));
        /// <summary>
        /// Defines the RenderSurfaceStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty RenderSurfaceStyleProperty =
            DependencyProperty.Register("RenderSurfaceStyle", typeof(Style), typeof(UltrachartSurface),
                                        new PropertyMetadata(null, OnChildStyleChanged));
        /// <summary>
        /// Defines the RenderableSeries DependencyProperty
        /// </summary>
        public static readonly DependencyProperty RenderableSeriesProperty =
            DependencyProperty.Register("RenderableSeries", typeof(ObservableCollection<IRenderableSeries>),
                                        typeof(UltrachartSurface),
                                        new PropertyMetadata(null, OnRenderableSeriesDependencyPropertyChanged));
        /// <summary>
        /// Defines the SelectedRenderableSeries DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectedRenderableSeriesProperty =
            DependencyProperty.Register("SelectedRenderableSeries", typeof(ObservableCollection<IRenderableSeries>),
                                        typeof(UltrachartSurface), new PropertyMetadata(null));
        /// <summary>
        /// Defines the ViewportManager DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ViewportManagerProperty =
            DependencyProperty.Register("ViewportManager", typeof(IViewportManager), typeof(UltrachartSurface),
                                        new PropertyMetadata(null, OnViewportManagerChanged));
        /// <summary>
        /// Defines the SeriesSource DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SeriesSourceProperty = DependencyProperty.Register("SeriesSource", typeof(ObservableCollection<IChartSeriesViewModel>),typeof(UltrachartSurface),new PropertyMetadata(null,OnSeriesSourceDependencyPropertyChanged));
        /// <summary>
        /// Defines the IsPolarChart DependencyProperty
        /// </summary>
        public static readonly DependencyProperty IsPolarChartProperty = DependencyProperty.Register("IsPolarChart", typeof (bool), typeof (UltrachartSurface), new PropertyMetadata(default(bool), OnIsPolarChartDependencyPropertyChanged));
        private GridLinesPanel _gridLinesPanel;
        private AxisArea _bottomAxisArea;
        private AxisArea _topAxisArea;
        private AxisArea _rightAxisArea;
        private AxisArea _leftAxisArea;
        private AxisArea _centerXAxisArea;
        private AxisArea _centerYAxisArea;
        private IEventAggregator _eventAggregator;
        private IUltrachartRenderer _ultraChartRenderer;
        private bool _isRendering;
        private AnnotationSurface _overlayAnnotationCanvas;
        private AnnotationSurface _underlayAnnotationCanvas;
        private Canvas _adornerLayerCanvas;
        private IRenderSurface2D _renderSurface;
        private readonly object _syncRoot = new object();
        private readonly HashSet<IDataSeries> _dsToNotify = new HashSet<IDataSeries>();
        private RenderableSeriesCollection _renderableSeries;
        /// <summary>
        /// Event raised when alignment of any axis changed
        /// </summary>
        public event EventHandler<AxisAlignmentChangedEventArgs> AxisAlignmentChanged;
        /// <summary>
        /// Event raised when XAxes DependnecyProperty is changed
        /// </summary>
        public event EventHandler XAxesCollectionNewCollectionAssigned;
        /// <summary>
        /// Event raised when YAxes DependnecyProperty is changed
        /// </summary>
        public event EventHandler YAxesCollectionNewCollectionAssigned;
        /// <summary>
        /// Event raised when Annotations DependencyProperty is changed
        /// </summary>
        public event EventHandler AnnotationsCollectionNewCollectionAssigned;
        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartSurface"/> class.
        /// </summary>
        /// <remarks></remarks>
        public UltrachartSurface()
        {
            DefaultStyleKey = typeof(UltrachartSurface);
            _renderableSeries = new RenderableSeriesCollection(this);
            SelectedRenderableSeries = new ObservableCollection<IRenderableSeries>();
            this.SetCurrentValue(RenderableSeriesProperty, new ObservableCollection<IRenderableSeries>());
            this.SetCurrentValue(YAxesProperty, new AxisCollection());
            this.SetCurrentValue(XAxesProperty, new AxisCollection());
            this.SetCurrentValue(AnnotationsProperty, new AnnotationCollection());
            this.SetCurrentValue(ViewportManagerProperty, new DefaultViewportManager());
            // Default implementation of Render Surface is the WriteableBitmapExRenderSurface
            this.SetCurrentValue(RenderSurfaceProperty, new HighSpeedRenderSurface());
            
            ZoomExtentsCommand = new ActionCommand(ZoomExtents);
            AnimateZoomExtentsCommand = new ActionCommand(() => AnimateZoomExtents(TimeSpan.FromMilliseconds(500)));
        }
        internal IDispatcherFacade DispatcherFacade
        {
            get { return Services.GetService<IDispatcherFacade>(); }
        }
        /// <summary>
        /// Gets the number of license days remaining
        /// </summary>
        public int LicenseDaysRemaining { get; internal set; }
        /// <summary>
        /// Gets or sets the template that defines the panel which controls the layout of left-aligned axes
        /// </summary>
        public ItemsPanelTemplate LeftAxesPanelTemplate
        {
            get { return (ItemsPanelTemplate)GetValue(LeftAxesPanelTemplateProperty); }
            set { SetValue(LeftAxesPanelTemplateProperty, value); }
        }
        /// <summary>
        /// Gets or sets the template that defines the panel which controls the layout of right-aligned axes
        /// </summary>
        public ItemsPanelTemplate RightAxesPanelTemplate
        {
            get { return (ItemsPanelTemplate)GetValue(RightAxesPanelTemplateProperty); }
            set { SetValue(RightAxesPanelTemplateProperty, value); }
        }
        /// <summary>
        /// Gets or sets the template that defines the panel which controls the layout of bottom-aligned axes
        /// </summary>
        public ItemsPanelTemplate BottomAxesPanelTemplate
        {
            get { return (ItemsPanelTemplate)GetValue(BottomAxesPanelTemplateProperty); }
            set { SetValue(BottomAxesPanelTemplateProperty, value); }
        }
        /// <summary>
        /// Gets or sets the template that defines the panel which controls the layout of top-aligned axes
        /// </summary>
        public ItemsPanelTemplate TopAxesPanelTemplate
        {
            get { return (ItemsPanelTemplate)GetValue(TopAxesPanelTemplateProperty); }
            set { SetValue(TopAxesPanelTemplateProperty, value); }
        }
        public ItemsPanelTemplate CenterXAxesPanelTemplate
        {
            get { return (ItemsPanelTemplate) GetValue(CenterXAxesPanelTemplateProperty); }
            set { SetValue(CenterXAxesPanelTemplateProperty, value); }
        }
        public ItemsPanelTemplate CenterYAxesPanelTemplate
        {
            get { return (ItemsPanelTemplate)GetValue(CenterYAxesPanelTemplateProperty); }
            set { SetValue(CenterYAxesPanelTemplateProperty, value); }
        }
        /// <summary>
        /// Gets or sets whether annotations over the chart should clip to bounds or not. Default value is True
        /// </summary>
        public bool ClipOverlayAnnotations
        {
            get { return (bool)GetValue(ClipOverlayAnnotationsProperty); }
            set { SetValue(ClipOverlayAnnotationsProperty, value); }
        }
        /// <summary>
        /// Gets or sets whether annotations under the chart should clip to bounds or not. Default value is true
        /// </summary>
        public bool ClipUnderlayAnnotations
        {
            get { return (bool)GetValue(ClipUnderlayAnnotationsProperty); }
            set { SetValue(ClipUnderlayAnnotationsProperty, value); }
        }
        /// <summary>
        /// Gets the collection of RenderableSeries that this UltrachartSurface draws.
        /// </summary>
        /// <value>The renderable series.</value>
        /// <remarks></remarks>
        public ObservableCollection<IRenderableSeries> RenderableSeries
        {
            get { return (ObservableCollection<IRenderableSeries>)GetValue(RenderableSeriesProperty); }
            set { SetValue(RenderableSeriesProperty, value); }
        }
        /// <summary>
        /// Gets the collection of RenderableSeries that are selected.
        /// </summary>
        /// <value>The renderable series.</value>
        /// <remarks></remarks>
        public ObservableCollection<IRenderableSeries> SelectedRenderableSeries
        {
            get { return (ObservableCollection<IRenderableSeries>)GetValue(SelectedRenderableSeriesProperty); }
            private set { SetValue(SelectedRenderableSeriesProperty, value); }
        }
        /// <summary>
        /// Gets or sets a value indicating whether Ultrachart will attempt to perform a one-time AutoRange on startup
        /// </summary>
        [Obsolete("We're Sorry! The AutoRangeOnStartup property has been deprecated in Ultrachart", true)]
        public bool AutoRangeOnStartup
        {
            get { return (bool)GetValue(AutoRangeOnStartupProperty); }
            set { SetValue(AutoRangeOnStartupProperty, value); }
        }
        /// <summary>
        /// Gets or sets the zoom extents command, which when invoked, causes the UltrachartSurface to zoom to extents
        /// </summary>
        /// <value>The zoom extents command.</value>
        /// <remarks></remarks>
        public ICommand ZoomExtentsCommand
        {
            get { return (ICommand)GetValue(ZoomExtentsCommandProperty); }
            set { SetValue(ZoomExtentsCommandProperty, value); }
        }
        /// <summary>
        /// Gets or sets the Animate zoom extents command, which when invoked, causes the UltrachartSurface to zoom to extents using animation
        /// </summary>
        /// <value>The zoom extents command.</value>
        /// <remarks></remarks>
        public ICommand AnimateZoomExtentsCommand
        {
            get { return (ICommand)GetValue(AnimateZoomExtentsCommandProperty); }
            set { SetValue(AnimateZoomExtentsCommandProperty, value); }
        }
        /// <summary>
        /// Gets or sets the primary XAxis on the UltrachartSurface (default side=Bottom)
        /// </summary>
        /// <value>The X axis.</value>
        /// <remarks></remarks>
        public IAxis XAxis
        {
            get { return (IAxis)GetValue(XAxisProperty); }
            set { SetValue(XAxisProperty, value); }
        }
        /// <summary>
        /// Gets or sets the primary YAxis on the UltrachartSurface (default side=Right)
        /// </summary>
        public IAxis YAxis
        {
            get { return (IAxis)GetValue(YAxisProperty); }
            set { SetValue(YAxisProperty, value); }
        }
        /// <summary>
        /// Gets the collection of Y-Axis <see cref="IAxis"/> that this UltrachartSurface measures against
        /// </summary>
        public AxisCollection YAxes
        {
            get { return (AxisCollection)GetValue(YAxesProperty); }
            set { SetValue(YAxesProperty, value); }
        }
        /// <summary>
        /// Gets the collection of X-Axis <see cref="IAxis"/> that this UltrachartSurface measures against
        /// </summary>
        public AxisCollection XAxes
        {
            get { return (AxisCollection)GetValue(XAxesProperty); }
            set { SetValue(XAxesProperty, value); }
        }
        /// <summary>
        /// Gets the <see cref="AnnotationCollection"/> which provides renderable annotations over the <see cref="UltrachartSurface"/>
        /// </summary>
        public AnnotationCollection Annotations
        {
            get { return (AnnotationCollection)GetValue(AnnotationsProperty); }
            set { SetValue(AnnotationsProperty, value); }
        }
        /// <summary>
        /// Gets or sets the ViewportManager instance on the chart, which handles behavior of the viewport on render
        /// </summary>
        /// <value>The renderable series.</value>
        /// <remarks></remarks>
        public IViewportManager ViewportManager
        {
            get { return (IViewportManager)GetValue(ViewportManagerProperty); }
            set { SetValue(ViewportManagerProperty, value); }
        }
        /// <summary>
        /// Gets the Annotation Canvas over the chart
        /// </summary>
        public IAnnotationCanvas AnnotationOverlaySurface
        {
            get { return _overlayAnnotationCanvas; }
        }
        /// <summary>
        /// Gets the Annotation Canvas under the chart
        /// </summary>
        public IAnnotationCanvas AnnotationUnderlaySurface
        {
            get { return _underlayAnnotationCanvas; }
        }
        /// <summary>
        /// Gets the Adorner Layer over the chart
        /// </summary>
        public Canvas AdornerLayerCanvas
        {
            get { return _adornerLayerCanvas; }
        }
        /// <summary>
        /// Gets or sets the current ChartModifier, which alters the behaviour of the chart
        /// </summary>
        /// <value>The chart modifier.</value>
        /// <remarks></remarks>
        public IChartModifier ChartModifier
        {
            get { return (IChartModifier)GetValue(ChartModifierProperty); }
            set { SetValue(ChartModifierProperty, value); }
        }
        /// <summary>
        /// Gets the GridLinesPanel where gridlines are drawn
        /// </summary>
        /// <remarks></remarks>
        public IGridLinesPanel GridLinesPanel
        {
            get { return _gridLinesPanel; }
        }
        /// <summary>
        /// Gets or sets the GridLinesPanel style.
        /// </summary>
        /// <value>The grid lines panel style.</value>
        /// <remarks></remarks>
        public Style GridLinesPanelStyle
        {
            get { return (Style)GetValue(GridLinesPanelStyleProperty); }
            set { SetValue(GridLinesPanelStyleProperty, value); }
        }
        /// <summary>
        /// Gets or sets the RenderSurface style.
        /// </summary>
        /// <value>The render surface style.</value>
        /// <remarks></remarks>
        public Style RenderSurfaceStyle
        {
            get { return (Style)GetValue(RenderSurfaceStyleProperty); }
            set { SetValue(RenderSurfaceStyleProperty, value); }
        }
        /// <summary>
        /// The SeriesSource property allows data-binding to a collection of <see cref="IChartSeriesViewModel"/> instances, 
        /// for pairing of <see cref="DataSeries{TX,TY}"/> with <see cref="IRenderableSeries"/>
        /// </summary>
        public ObservableCollection<IChartSeriesViewModel> SeriesSource
        {
            get { return (ObservableCollection<IChartSeriesViewModel>)GetValue(SeriesSourceProperty); }
            set { SetValue(SeriesSourceProperty, value); }
        }
        /// <summary>
        /// Gets whether this <see cref="UltrachartSurface"/> is a polar chart or not
        /// </summary>
        public bool IsPolarChart
        {
            get { return (bool)GetValue(IsPolarChartProperty); }
            private set { SetValue(IsPolarChartProperty, value); }
        }
        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes (such as a rebuilding layout pass) call <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>. In simplest terms, this means the method is called just before a UI element displays in an application. For more information, see Remarks.
        /// </summary>
        /// <remarks></remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DetachChildren();            
            if (_topAxisArea != null) _topAxisArea.Items.Clear();
            if (_bottomAxisArea != null) _bottomAxisArea.Items.Clear();
            if (_rightAxisArea != null) _rightAxisArea.Items.Clear();
            if (_leftAxisArea != null) _leftAxisArea.Items.Clear();
            if (_centerXAxisArea != null) _centerXAxisArea.Items.Clear();
            if (_centerYAxisArea != null) _centerYAxisArea.Items.Clear();
            
            _gridLinesPanel = GetAndAssertTemplateChild<GridLinesPanel>("PART_GridLinesArea");
            _bottomAxisArea = GetAndAssertTemplateChild<AxisArea>("PART_BottomAxisArea");
            _topAxisArea = GetAndAssertTemplateChild<AxisArea>("PART_TopAxisArea");
            _rightAxisArea = GetAndAssertTemplateChild<AxisArea>("PART_RightAxisArea");
            _leftAxisArea = GetAndAssertTemplateChild<AxisArea>("PART_LeftAxisArea");
            _centerXAxisArea = GetAndAssertTemplateChild<AxisArea>("PART_CenterXAxisArea");
            _centerYAxisArea = GetAndAssertTemplateChild<AxisArea>("PART_CenterYAxisArea");
            _overlayAnnotationCanvas = GetAndAssertTemplateChild<AnnotationSurface>("PART_AnnotationsOverlaySurface");
            _underlayAnnotationCanvas = GetAndAssertTemplateChild<AnnotationSurface>("PART_AnnotationsUnderlaySurface");
            _adornerLayerCanvas = GetAndAssertTemplateChild<Canvas>("PART_ChartAdornerLayer");
            ((ServiceContainer)Services).RegisterService<IChartModifierSurface>(ModifierSurface);
            if (GridLinesPanelStyle != null)
                _gridLinesPanel.Style = GridLinesPanelStyle;
            _gridLinesPanel.EventAggregator = _eventAggregator;
            // Case where user has set XAxes or YAxes on UltrachartSurface before template applied
            // add YAxes to left or right axis areas, XAxis to top and bottom areas
            YAxes.ForEachDo(PlaceAxis);
            XAxes.ForEachDo(PlaceAxis);
            AttachChildren();
            new LicenseManager().Validate(this, new UltrachartLicenseProviderFactory());            
            InvalidateElement();
        }
        /// <summary>
        /// Raises the <see cref="UltrachartSurfaceBase.Rendered"/> event, fired at the end of a render pass immediately before presentation to the screen 
        /// </summary>
        public override void OnUltrachartRendered()
        {
            if (ViewportManager != null)
            {
                ViewportManager.OnParentSurfaceRendered(this);
            }
            base.OnUltrachartRendered();
            NotifyAxes();
        }
        private void NotifyAxes()
        {
            lock (_dsToNotify)
            {
                var axesToNotify = new HashSet<IAxis>();
                // Original code
                //                foreach (var ds in _dsToNotify)
                //                {
                //                    RenderableSeries.Where(r => ReferenceEquals(r.DataSeries, ds))
                //                        .ForEachDo(series =>
                //                        {
                //                            axesToNotify.Add(series.YAxis);
                //                            axesToNotify.Add(series.XAxis);
                //                        });
                //                }
                // Attempt #2
                foreach (var rSeries in RenderableSeries)
                {
                    if (_dsToNotify.Contains(rSeries.DataSeries))
                    {
                        axesToNotify.Add(rSeries.YAxis);
                        axesToNotify.Add(rSeries.XAxis);
                    }
                }
                axesToNotify.ForEachDo(AxisBase.NotifyDataRangeChanged);
                
                _dsToNotify.Clear();
            }
        }
        protected override void Dispose(bool disposing)
        {
            lock (_dsToNotify)
            {
                _dsToNotify.Clear();
            }
            base.Dispose(disposing);
        }
        /// <summary>
        /// Called to remeasure a control.
        /// </summary>
        /// <param name="constraint">The maximum size that the method can return.</param>
        /// <returns>
        /// The size of the control, up to the maximum specified by <paramref name="constraint" />.
        /// </returns>
        protected override Size MeasureOverride(Size constraint)
        {
            // Case when AxisAlignment is set in an implicit style
            YAxes.ForEachDo(PlaceAxis);
            XAxes.ForEachDo(PlaceAxis);
            return base.MeasureOverride(constraint);
        }
        private void PlaceAxis(IAxis axis)
        {
            var actualPlacement = GetActualPlacementOf(axis);
            var actualAlignment = actualPlacement.Item1;
            var actualIsCenterAxis = actualPlacement.Item2;
            if (actualAlignment != axis.AxisAlignment || actualIsCenterAxis != axis.IsCenterAxis)
            {
                ChangeAxisContainer(axis, actualAlignment, actualIsCenterAxis);
            }
        }
        private Tuple<AxisAlignment, bool> GetActualPlacementOf(IAxis axis)
        {
            var actualAlignment = AxisAlignment.Default;
            var actualIsCenterAxis = false;
            if (_leftAxisArea.Items.Contains(axis))
            {
                actualAlignment = AxisAlignment.Left;
            }
            else if (_rightAxisArea.Items.Contains(axis))
            {
                actualAlignment = AxisAlignment.Right;
            }
            else if (_topAxisArea.Items.Contains(axis))
            {
                actualAlignment = AxisAlignment.Top;
            }
            else if (_bottomAxisArea.Items.Contains(axis))
            {
                actualAlignment = AxisAlignment.Bottom;
            }
            else if (_centerXAxisArea.Items.Contains(axis) || _centerYAxisArea.Items.Contains(axis))
            {
                actualAlignment = axis.AxisAlignment;
                actualIsCenterAxis = true;
            }
            return new Tuple<AxisAlignment, bool>(actualAlignment, actualIsCenterAxis);
        }
        private void DetachChildren()
        {
            UnsubscribeFromMouseEvents();
            if (ChartModifier != null)
            {
                DetachModifier(ChartModifier);
            }
            if (Annotations != null)
            {
                Annotations.UnsubscribeSurfaceEvents(this);
            }
        }
        private void AttachChildren()
        {
            if (ChartModifier != null)
            {
                AttachModifier(ChartModifier);
            }
            if (Annotations != null)
            {
                Annotations.SubscribeSurfaceEvents(this);
            }
        }
        /// <summary>
        /// Zooms the chart to the extents of the data, plus any X or Y Grow By fraction set on the X and Y Axes
        /// </summary>
        public virtual void ZoomExtents()
        {
            ZoomExtents(TimeSpan.Zero);
        }
        /// <summary>
        /// Zooms to extents with the specified animation duration
        /// </summary>
        /// <param name="duration">The duration of animation when zooming to extents</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void AnimateZoomExtents(TimeSpan duration)
        {
            ZoomExtents(duration);
        }
        private void ZoomExtents(TimeSpan duration)
        {
            if (XAxes.IsNullOrEmpty() || YAxes.IsNullOrEmpty())
            {
                if (DebugWhyDoesntUltrachartRender) {
                    var message = " UltrachartSurface didn't render, " + RendererErrorCodes.BecauseXAxesOrYAxesIsNull;
                    Console.WriteLine(message);
                    UltrachartDebugLogger.Instance.WriteLine(message);
                }
                return;
            }
            UltrachartDebugLogger.Instance.WriteLine("ZoomExtents() called");
            using (SuspendUpdates())
            {
                //Update the VisibleRange of X Axes, simulating (but not setting) AutoRange
                var xRanges = ZoomExtentsXInternal(duration);
                //Now ZoomExtents on Y for N Y-axes
                ZoomExtentsY(xRanges, duration);
            }
        }
        private IDictionary<string, IRange> ZoomExtentsXInternal(TimeSpan duration)
        {
            var xRanges = new Dictionary<string, IRange>();
            foreach (var xAxis in XAxes)
            {
                var maxRange = xAxis.GetMaximumRange();
                xAxis.TrySetOrAnimateVisibleRange(maxRange, duration);
                xRanges.Add(xAxis.Id, maxRange);
            }
            return xRanges;
        }
        private void ZoomExtentsY(IDictionary<string, IRange> xRanges, TimeSpan duration)
        {
            foreach (var yAxis in YAxes)
            {
                ZoomExtentsOnYAxis(yAxis, xRanges, duration);
            }
        }
        private void ZoomExtentsOnYAxis(IAxis axis, IDictionary<string, IRange> xRanges, TimeSpan duration)
        {
            var maxYRange = axis.GetWindowedYRange(xRanges);
            axis.TrySetOrAnimateVisibleRange(maxYRange, duration);
        }
        /// <summary>
        /// Zooms the chart to the extents of the data in the Y-Direction, accounting for the current data in view in the X-direction
        /// </summary>
        public void ZoomExtentsY()
        {
            UltrachartDebugLogger.Instance.WriteLine("ZoomExtentsY() called");
            using (SuspendUpdates())
            {
                ZoomExtentsY(null, TimeSpan.Zero);
            }
        }
        /// <summary>
        /// Zooms the chart to the extents of the data in the Y-Direction, accounting for the current data in view in the X-direction
        /// </summary>
        /// <param name="duration"></param>
        public void AnimateZoomExtentsY(TimeSpan duration)
        {
            using (SuspendUpdates())
            {
                ZoomExtentsY(null, duration);
            }
        }
        /// <summary>
        /// Zooms the chart to the extents of the data in the X-Direction
        /// </summary>
        public void ZoomExtentsX()
        {
            ZoomExtentsXInternal(TimeSpan.Zero);
        }
        /// <summary>
        /// Zooms the chart to the extents of the data in the X-Direction, using animation with the specified duration
        /// </summary>
        /// <param name="duration">The duration of the animation</param>
        public void AnimateZoomExtentsX(TimeSpan duration)
        {
            ZoomExtentsXInternal(duration);
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
                InvalidateElement();
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
        /// Removes all DataSeries from the Ultrachart
        /// </summary>
        /// <remarks></remarks>
        [Obsolete("We're Sorry but IUltrachartSurface.Clear() has been deprecated. Please call IUltrachartSurface.RenderableSeries.Clear() to clear the chart", true)]
        public void ClearSeries()
        {
        }
        internal class VersionFinder : Credentials
        {
        }
        /// <summary>
        /// Returns version and license info as a formatted string
        /// </summary>
        public static string VersionAndLicenseInfo
        {
            get
            {
                var assemblyName = new AssemblyName(typeof(UltrachartSurface).Assembly.FullName);
                var version = assemblyName.Version;
                string licenseString = string.Format("Ultrachart v{0}", version);
                return licenseString;
            }
        }

        internal static string LicenseKey {get; private set;}

        /// <summary>
        /// Manually applies a license key, in case automatic discovery fails
        /// </summary>
        /// <param name="key">The license key string</param>
        public static void SetLicenseKey(string key)
        {
            LicenseKey = key;
        }
        // By performing the render-loop inside CompositionTarget.Rendering, we decouple the drawing from the calling code, 
        // preventing rendering the same thing multiple times when a property changes
        private void OnRenderSurfaceDraw(object sender, DrawEventArgs e)
        {
            if (!IsLoaded)
                return;
            // Ignore Immediate rendering, this is taken care of in this.InvalidateElement(); 
            if (RenderPriority == RenderPriority.Immediate || _isRendering)
                return;
            _isRendering = true;
            Action render = () =>
            {
                // Lock around dataset allows the user to call using(DataSet.SuspendUpdates()) {} 
                // to effectively lock dataseries access & rendering, removing flickering in cases where
                // multiple updates, removes or clears are performed in a batch
                object locker = this.SyncRoot;
                Monitor.Enter(locker);
                DoDrawingLoop();                    
                Monitor.Exit(locker);
                _isRendering = false;
                
            };
            if (RenderPriority == RenderPriority.Normal)
            {
                // At normal render priority, cause a re-draw as soon as CompositionTarget.Rendering event fires
                render();
            }
            else if (RenderPriority == RenderPriority.Low)
            {
                // At low render priority, we need to put the render event lower than input (so mouse events take precedence). 
                // However, due to .NET4.5 quirks, all drawing must be synced to the CompositionTarget.Rendering event, 
                // or you get flicker (SC-388), so for this we use CompositionSyncedDelegate (which self-disposes)
                Services.GetService<IDispatcherFacade>()
                    .BeginInvoke(() => { new CompositionSyncedDelegate(render); }, DispatcherPriority.Input);
            }
        }
        protected override void DoDrawingLoop()
        {
            if (IsDisposed || _renderSurface == null || Visibility != Visibility.Visible)
                return;
            try
            {
                using (var context = _renderSurface.GetRenderContext())
                {
                    try
                    {
                        var statusResult = _ultraChartRenderer.RenderLoop(context);
                        if (!RendererErrorCodes.Success.Equals(statusResult) && DebugWhyDoesntUltrachartRender == true) {
                            var message = " UltrachartSurface didn't render, " + statusResult;
                            Console.WriteLine(message);
                            UltrachartDebugLogger.Instance.WriteLine(message);
                        }                                       
                    }
                    catch (Exception ex)
                    {
                        // Exception in processing messages
                        OnRenderFault(ex);
                    }
                }
            }
            catch (Exception caught)
            {
                OnRenderFault(caught);
            }
        }
        /// <summary>
        /// Translates the point relative to the other hittestable element
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Obsolete. Please use UltrachartSurface.RootGrid.TranslatePoint instead", true)]
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            throw new NotImplementedException("Obsolete. Please use UltrachartSurface.RootGrid.TranslatePoint instead");
        }
        /// <summary>
        /// Returns true if the Point is within the bounds of the current HitTestable element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>true if the Point is within the bounds</returns>
        /// <remarks></remarks>
        [Obsolete("Obsolete. Please use UltrachartSurface.RootGrid.IsPointWithinBounds instead", true)]
        public bool IsPointWithinBounds(Point point)
        {
            throw new NotImplementedException("Obsolete. Please use UltrachartSurface.RootGrid.TranslatePoint instead");
        }
        /// <summary>
        /// Gets the bounds of the current HitTestable element relative to another HitTestable element
        /// </summary>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        [Obsolete("Obsolete. Please use UltrachartSurface.RootGrid.GetBoundsRelativeTo instead", true)]
        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            throw new NotImplementedException(
                "Obsolete. Please use UltrachartSurface.RootGrid.GetBoundsRelativeTo instead");
        }
        /// <summary>
        /// Attaches listeners for DataSeries.DataSeriesChanged
        /// </summary>remote
        /// 
        /// <param name="dataSeries"></param>
        public void AttachDataSeries(IDataSeries dataSeries)
        {
            if (dataSeries == null) return;
            dataSeries.ParentSurface = dataSeries.ParentSurface ?? this;
            if (IsUltrachartSurfaceLoaded)
            {
                dataSeries.DataSeriesChanged -= OnDataSeriesChanged;
                dataSeries.DataSeriesChanged += OnDataSeriesChanged;
            }
            TryAddDataSeriesForNotification(dataSeries);
        }
        /// <summary>
        /// Detaches listeners for DataSeries.DataSeriesChanged
        /// </summary>
        /// <param name="dataSeries"></param>
        public void DetachDataSeries(IDataSeries dataSeries)
        {
            if (dataSeries == null) return;
            if (ReferenceEquals(dataSeries.ParentSurface, this))
            {
                dataSeries.ParentSurface = null;
            }
            dataSeries.DataSeriesChanged -= OnDataSeriesChanged;
            TryAddDataSeriesForNotification(dataSeries);
        }
        private void OnDataSeriesChanged(object sender, DataSeriesChangedEventArgs e)
        {
            TryAddDataSeriesForNotification(sender as IDataSeries);
            OnDataSeriesUpdated(sender, e);
        }
        private void TryAddDataSeriesForNotification(IDataSeries dataSeries)
        {            
            if (dataSeries != null)
            {
                lock (_dsToNotify)
                {
                    _dsToNotify.Add(dataSeries);
                }
            }
        }
        private void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems != null)
            {
                foreach (IAxis axis in args.OldItems)
                {
                    DetachAxis(axis);
                }
            }
            if (args.NewItems != null)
            {
                foreach (IAxis axis in args.NewItems)
                {
                    AttachAxis(axis, false);
                }
            }
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearAxisAreasFrom(false);
            }
            if (Annotations != null)
            {
                Annotations.OnYAxesCollectionChanged(sender, args);
            }
            if (ChartModifier != null)
            {
                ChartModifier.OnYAxesCollectionChanged(sender, args);
            }
            InvalidateElement();
        }
        private void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (args.OldItems != null)
            {
                foreach (IAxis axis in args.OldItems)
                {
                    DetachAxis(axis);
                }
            }
            if (args.NewItems != null)
            {
                foreach (IAxis axis in args.NewItems)
                {
                    AttachAxis(axis, true);
                }
            }
            if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                ClearAxisAreasFrom(true);
            }
            if (Annotations != null)
            {
                Annotations.OnXAxesCollectionChanged(sender, args);
            }
            if (ChartModifier != null)
            {
                ChartModifier.OnXAxesCollectionChanged(sender, args);
            }
            InvalidateElement();
        }
        private void ClearAxisAreasFrom(bool isXAxes)
        {
            ClearAxisAreaFrom(_rightAxisArea, isXAxes);
            ClearAxisAreaFrom(_leftAxisArea, isXAxes);
            ClearAxisAreaFrom(_topAxisArea, isXAxes);
            ClearAxisAreaFrom(_bottomAxisArea, isXAxes);
            ClearAxisAreaFrom(_centerXAxisArea, isXAxes);
            ClearAxisAreaFrom(_centerYAxisArea, isXAxes);
        }
        private void ClearAxisAreaFrom(AxisArea area, bool isXAxes)
        {
            if (area != null)
            {
                var axes = area.Items.Cast<IAxis>().Where(axis => axis.IsXAxis == isXAxes).ToList();
                foreach (var axis in axes)
                {
                    DetachAxis(axis);
                }
            }
        }
        private void DetachAxis(IAxis axis)
        {
            RemoveFromAxisContainer(axis, axis.AxisAlignment, axis.IsCenterAxis);
            axis.ParentSurface = null;
            axis.Services = null;
        }
        private void AttachAxis(IAxis axis, bool isXAxis)
        {
            axis.ParentSurface = this;
            axis.IsXAxis = isXAxis;
            if (axis.AxisAlignment == AxisAlignment.Default)
            {
                var defaultAlignment = axis.IsXAxis ? AxisAlignment.Bottom : AxisAlignment.Right;
                ((AxisBase) axis).SetCurrentValue(AxisBase.AxisAlignmentProperty, defaultAlignment);
                return;
            }
            AddToAxisContainer(axis, axis.AxisAlignment, axis.IsCenterAxis);
        }
        private void OnRenderableSeriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _renderableSeries.Clear();
            }
            if (e.OldItems != null)
            {
                e.OldItems.Cast<IRenderableSeries>().ForEachDo(series => _renderableSeries.Remove(series));
            }
            if (e.NewItems != null)
            {
                e.NewItems.Cast<IRenderableSeries>().ForEachDo(_renderableSeries.Add);
            }
        }
        private void DetachRenderableSeries(IRenderableSeries rSeries)
        {
            if (_renderSurface != null)
            {
                rSeries.SelectionChanged -= OnSeriesSelectionChanged;
                _renderSurface.RemoveSeries(rSeries);
            }
            if (rSeries.DataSeries != null)
            {
                DetachDataSeries(rSeries.DataSeries);                
            }
            if (rSeries.IsSelected)
            {
                ((BaseRenderableSeries)rSeries).SetCurrentValue(BaseRenderableSeries.IsSelectedProperty, false);
            }
            if (SelectedRenderableSeries.Contains(rSeries)) SelectedRenderableSeries.Remove(rSeries);
            if (rSeries is IStackedColumnRenderableSeries)
            {
                var stackedSeries = (IStackedColumnRenderableSeries)rSeries;
                StackedColumnsWrapper.RemoveSeries(stackedSeries);
                if (StackedColumnsWrapper.GetStackedSeriesCount() == 0)
                {
                    StackedColumnsWrapper = null;
                }
            }       
            
            if (rSeries is IStackedMountainRenderableSeries)
            {
                var stackedSeries = (IStackedMountainRenderableSeries)rSeries;
                StackedMountainsWrapper.RemoveSeries(stackedSeries);
                if (StackedMountainsWrapper.GetStackedSeriesCount() == 0)
                {
                    StackedMountainsWrapper = null;
                }
            }
            InvalidateElement();
        }
        private void AttachRenderableSeries(IRenderableSeries rSeries)
        {
            if (_renderSurface != null)
            {
                _renderSurface.AddSeries(rSeries);
                rSeries.SelectionChanged -= OnSeriesSelectionChanged;
                rSeries.SelectionChanged += OnSeriesSelectionChanged;
                if (rSeries.IsSelected && !SelectedRenderableSeries.Contains(rSeries))
                {
                    SelectedRenderableSeries.Add(rSeries);
                }
            }
            if (rSeries.DataSeries != null)
            {
                AttachDataSeries(rSeries.DataSeries);
            }
            if (rSeries is IStackedColumnRenderableSeries)
            {
                if (StackedColumnsWrapper == null)
                {
                    StackedColumnsWrapper = new StackedColumnsWrapper();
                }
                var stackedSeries = (IStackedColumnRenderableSeries)rSeries;
                StackedColumnsWrapper.AddSeries(stackedSeries);
            }
            if (rSeries is IStackedMountainRenderableSeries)
            {
                if (StackedMountainsWrapper == null)
                {
                    StackedMountainsWrapper = new StackedMountainsWrapper();
                }
                var stackedSeries = (IStackedMountainRenderableSeries)rSeries;
                StackedMountainsWrapper.AddSeries(stackedSeries);
            }
            InvalidateElement();
        }
        private void DetachChartSeries(IChartSeriesViewModel oldItem)
        {
            oldItem.PropertyChanged -= ChartSeriesViewModelPropertyListener;
            if (oldItem.RenderSeries != null && RenderableSeries.Contains(oldItem.RenderSeries))
            {
                RenderableSeries.Remove(oldItem.RenderSeries);
            }
        }
        private void AttachChartSeries(IChartSeriesViewModel newItem)
        {
            Guard.NotNull(newItem, "newItem");
            newItem.PropertyChanged -= ChartSeriesViewModelPropertyListener;
            newItem.PropertyChanged += ChartSeriesViewModelPropertyListener;
            int index = SeriesSource.IndexOf(newItem);
            if (newItem.RenderSeries != null)
            {
                // If existing RenderableSeries is re-used,
                // just update DataSeries
                if (!RenderableSeries.Contains(newItem.RenderSeries))
                {
                    RenderableSeries.Insert(index, newItem.RenderSeries);
                }
                newItem.RenderSeries.DataSeries = newItem.DataSeries;
            }
        }
        private void OnSeriesSelectionChanged(object sender, EventArgs e)
        {
            var series = sender as IRenderableSeries;
            if (series == null) return;
            if (series.IsSelected)
            {
                SelectedRenderableSeries.Add(series);
            }
            else
            {
                SelectedRenderableSeries.Remove(series);
            }
        }
        private static int loadedCount ;
        private static char globalId = 'a';
        private string id = (globalId++).ToString();
        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase" /> is loaded. Perform initialization operations here.
        /// </summary>
        protected override void OnUltrachartSurfaceLoaded()
        {
            if (IsUltrachartSurfaceLoaded || IsDisposed) return;
            base.OnUltrachartSurfaceLoaded();
            var lc = Interlocked.Increment(ref loadedCount);
            Debug.WriteLine("UltrachartSurface {0} Loaded: {1}", id, lc);
            if (SeriesSource != null)
            {
                SeriesSource.CollectionChanged -= OnChartSeriesCollectionChanged;
                SeriesSource.CollectionChanged += OnChartSeriesCollectionChanged;
                foreach (var chartSeriesViewModel in SeriesSource)
                {
                    AttachChartSeries(chartSeriesViewModel);
                }
            }
            RenderableSeries.ForEachDo(AttachRenderableSeries);
            XAxes.ForEachDo(axis => AttachAxis(axis, true));
            YAxes.ForEachDo(axis => AttachAxis(axis, false));
            
            AttachChildren();
        }
        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase" /> is Unloaded and removed from the visual tree. Perform cleanup operations here
        /// </summary>
        protected override void OnUltrachartSurfaceUnloaded()
        {
            if (!IsUltrachartSurfaceLoaded) return;
            base.OnUltrachartSurfaceUnloaded();
            var lc = Interlocked.Decrement(ref loadedCount);
            Debug.WriteLine("UltrachartSurface {0} Unloaded: {1}", id, lc);
            if (SeriesSource != null)
            {
                foreach (var chartSeriesViewModel in SeriesSource)
                {
                    DetachChartSeries(chartSeriesViewModel);
                }
                SeriesSource.CollectionChanged -= OnChartSeriesCollectionChanged;
            }
            RenderableSeries.ForEachDo(DetachRenderableSeries);
            XAxes.ForEachDo(DetachAxis);
            YAxes.ForEachDo(DetachAxis);
            DetachChildren();
        }
        /// <summary>
        /// Preparations for a render pass, called internally, returns the viewport size
        /// </summary>
        /// <returns>The required Viewport Size</returns>
        public Size OnArrangeUltrachart()
        {
            if (RenderSurface.NeedsResizing)
            {
                //RenderSurface.RecreateSurface();
            }
            var size = new Size(RenderSurface.ActualWidth, RenderSurface.ActualHeight);
            return size;
        }
        /// <summary>
        /// Equivalent of calling YAxis.GetMaximumRange() however returns the max range only for that axis (by the data-series on it)
        /// "windowed" = "displayed in current viewport"
        /// uses GrowBy()
        /// </summary>
        /// <param name="yAxis"></param>
        /// <param name="xRange"></param>
        /// <returns></returns>
        [Obsolete("IUltrachartSurface.GetWindowedYRange is obsolete. Use IAxis.GetWindowedYRange instead", true)]
        public IRange GetWindowedYRange(IAxis yAxis, IRange xRange)
        {
            var dataSeries = GetDataSeriesFor(yAxis.Id);
            var maxRange = yAxis.GetMaximumRange();
            if (!dataSeries.Any())
            {
                return yAxis.VisibleRange == null || yAxis.VisibleRange.IsDefined ? yAxis.VisibleRange : maxRange;
            }
            //get all valid windowed ranges
            IRange[] ranges = dataSeries.Select(ds => ds.GetWindowedYRange(xRange, yAxis.IsLogarithmicAxis))
                .Where(range => range.IsDefined)
                .ToArray();
            var windowedRange = ranges.FirstOrDefault();
            if (windowedRange != null)
            {
                //calculate the maximal range
                for (int i = 1; i < ranges.Length; i++)
                {
                    windowedRange = windowedRange.Union(ranges[i]);
                }
                //apply the GrowBy
                if (yAxis.GrowBy != null && yAxis.GrowBy.IsDefined)
                    windowedRange.GrowBy(yAxis.GrowBy.Min, yAxis.GrowBy.Max);
            }
            return windowedRange ?? maxRange;
        }
        /// <summary>
        /// Called internally by Ultrachart when <see cref="IAxis.IsCenterAxis"/> changes. Allows the <see cref="UltrachartSurface"/> to place the axis in the center of chart
        /// </summary>
        /// <param name="axis"></param>
        public void OnIsCenterAxisChanged(IAxis axis)
        {
            ChangeAxisContainer(axis, axis.AxisAlignment, !axis.IsCenterAxis);
        }
        /// <summary>
        /// Called internally by Ultrachart when <see cref="IAxis.AxisAlignment" /> changes. Allows the <see cref="UltrachartSurface" /> to reposition the axis, args.g. at the top, left, bottom, right
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="oldValue"> </param>
        public void OnAxisAlignmentChanged(IAxis axis, AxisAlignment oldValue)
        {
            var newValue = axis.AxisAlignment;
            
            ChangeAxisContainer(axis, oldValue, axis.IsCenterAxis);
            OnAxisAlignmentChanged(new AxisAlignmentChangedEventArgs(axis.Id, oldValue, newValue));
        }
        private void ChangeAxisContainer(IAxis axis, AxisAlignment oldAlignment, bool oldIsCenterAxis)
        {
            // #SC-2470: need to save values before detaching 
            // after removing axis from container binding restores default property value
            var newAxisAlignment = axis.AxisAlignment;
            var newIsCenterAxis = axis.IsCenterAxis;
            // Prevents AxisAlignmentChanged event from being fired when an implicit style is applied.
            // Adding an axis to the AxisArea causes implicit style to be applied, removing causes it to be rejected.
            using (axis.SuspendUpdates())
            {
                // Remove axis from parent container
                RemoveFromAxisContainer(axis, oldAlignment, oldIsCenterAxis);
                // Add into new parent container
                AddToAxisContainer(axis, newAxisAlignment, newIsCenterAxis);
            }
        }
        private void RemoveFromAxisContainer(IAxis axis, AxisAlignment alignment, bool isCenterAxis)
        {
            var container = GetContainerFor(axis, alignment, isCenterAxis);
            // checking if axis is attached to chart
            if (container != null && axis.ParentSurface != null) 
            {
                container.SafeRemoveItem(axis);
            }
            InvalidateIsPolarChartProperty();
        }
        private void InvalidateIsPolarChartProperty()
        {
            if (_centerXAxisArea != null && _centerYAxisArea != null)
            {
                var isPolarChart = _centerXAxisArea.Items.OfType<IAxis>().Any(x => x.IsPolarAxis) ||
                                   _centerYAxisArea.Items.OfType<IAxis>().Any(x => x.IsPolarAxis);
                IsPolarChart = isPolarChart;
            }            
        }
        private AxisArea GetContainerFor(IAxis axis, AxisAlignment axisAlignment, bool isCenterAxis)
        {
            if (isCenterAxis)
                return axis.IsXAxis ? _centerXAxisArea : _centerYAxisArea;
            AxisArea container = null;
            switch (axisAlignment)
            {
                case AxisAlignment.Left:
                    container = _leftAxisArea;
                    break;
                case AxisAlignment.Right:
                    container = _rightAxisArea;
                    break;
                case AxisAlignment.Top:
                    container = _topAxisArea;
                    break;
                case AxisAlignment.Bottom:
                    container = _bottomAxisArea;
                    break;
                case AxisAlignment.Default:
                    container = axis.IsXAxis ? _bottomAxisArea : _rightAxisArea;
                    break;
            }
            return container;
        }
        private void AddToAxisContainer(IAxis axis, AxisAlignment alignment, bool isCenterAxis)
        {
            var container = GetContainerFor(axis, alignment, isCenterAxis);
            if (container != null && !container.Items.Contains(axis))
            {
                var addInReverseOrder = !ReferenceEquals(container, _centerXAxisArea) && !ReferenceEquals(container, _centerYAxisArea) &&
                                        (alignment == AxisAlignment.Left || alignment == AxisAlignment.Top);
                if (addInReverseOrder)
                {
                    container.Items.Insert(0, axis);
                }
                else
                {
                    container.Items.Add(axis);
                }
            }
            InvalidateIsPolarChartProperty();
        }
        private void OnAxisAlignmentChanged(AxisAlignmentChangedEventArgs args)
        {
            var handler = AxisAlignmentChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }
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
        /// Generates <see cref="UltrachartSurface"/> from its XML representation.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == GetType().Name)
            {
                UltrachartSurfaceSerializationHelper.Instance.DeserializeProperties(this, reader);
            }
        }
        /// <summary>
        /// Converts <see cref="UltrachartSurface"/> into its XML representation.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void WriteXml(XmlWriter writer)
        {
            UltrachartSurfaceSerializationHelper.Instance.SerializeProperties(this, writer);
        }
        /// <summary>
        /// Export snapshot of current <see cref="UltrachartSurface"/> to <see cref="BitmapSource"/>
        /// </summary>
        /// <returns></returns>
        public BitmapSource ExportToBitmapSource()
        {
            var renderPriority = RenderPriority;
            try
            {
                PrepareSurface(Width, Height);
                return BitmapPrintingHelper.ExportToBitmapSource(this);
            }
            finally
            {
                RenderPriority = renderPriority;
            }
        }
        private void PrepareSurface(double width, double height)
        {
            if (!IsLoaded)
            {
                // Throws in SL
#if !SILVERLIGHT
                // Prepare surface for rendering in memory
                var desiredSize = new Size(width, height);
                RenderPriority = RenderPriority.Immediate;
                ApplyTemplate();
                OnLoad();
                // Fix for SC-2600: need to simulate load to place annotations
                if (Annotations != null)
                {
                    Annotations
                        .OfType<FrameworkElement>()
                        .ForEachDo(annotation => annotation.RaiseEvent(new RoutedEventArgs(LoadedEvent)));
                }
                // Applying template and building control visual tree
                Measure(desiredSize);
                Arrange(new Rect(new Point(0, 0), desiredSize));
                UpdateLayout();
                // Invalidate triggers creation of images inside surface axes and rendersurface
                InvalidateElement();
                UpdateLayout();
                //Need invalidate one more time to consider sizes of created images during layout
                InvalidateElement();
                UpdateLayout();
#endif
            }
        }
#if !SILVERLIGHT
        /// <summary>
        /// Saves snapshot of current <see cref="UltrachartSurface"/> to file
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="exportType">Defines format of file to export</param>
        public void ExportToFile(string fileName, ExportType exportType)
        {
            var bitmap = ExportToBitmapSource();
            BitmapPrintingHelper.SaveToFile(bitmap, fileName, exportType);
        }
#endif
        /// <summary>
        /// Outputs current <see cref="UltrachartSurface"/> to printer
        /// </summary>
        /// <param name="description">Description of printing job</param>
        public void Print(string description = null)
        {
            var renderPriority = RenderPriority;
            try
            {
                var width = Width;
                var height = Height;
#if !SILVERLIGHT
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true)
                {
                    width = dialog.PrintableAreaWidth;
                    height = dialog.PrintableAreaHeight;
#endif
                    PrepareSurface(width, height);
#if !SILVERLIGHT
                    Action printAction = () => dialog.PrintVisual(this, description);
                    Dispatcher.BeginInvoke(printAction);
                }
#else
            var document = new System.Windows.Printing.PrintDocument();
            document.PrintPage += (sender, args) => args.PageVisual = this;
            document.Print(description);
#endif
            }
            finally
            {
                RenderPriority = renderPriority;
            }
        }
        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase"/> Size changes. Perform render surface resize or redraw operations here
        /// </summary>
        protected override void OnUltrachartSurfaceSizeChanged()
        {
            UltrachartDebugLogger.Instance.WriteLine("UltrachartSurface Resized: x={0}\ty={1}", ActualWidth,
                                                   ActualHeight);
            _eventAggregator.Publish(new UltrachartResizedMessage(this));
            base.OnUltrachartSurfaceSizeChanged();
        }
        /// <summary>
        /// Called when the <see cref="UltrachartSurfaceBase" /> DataContext changes.
        /// </summary>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
        {
            if (ChartModifier != null)
            {
                ChartModifier.DataContext = e.NewValue;
            }
        }
        private static void OnViewportManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((IUltrachartSurface)d);
            var newViewportManager = e.NewValue as IViewportManager;
            var oldViewportManager = e.OldValue as IViewportManager;
            if (oldViewportManager != null)
            {
                oldViewportManager.DetachUltrachartSurface();
            }
            if (newViewportManager != null)
            {
                newViewportManager.AttachUltrachartSurface(ultraChartSurface);
            }
            ultraChartSurface.InvalidateElement();
        }
        private static void OnAnnotationsDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((UltrachartSurface)d);
            var newCollection = e.NewValue as AnnotationCollection;
            var oldCollection = e.OldValue as AnnotationCollection;
            if (oldCollection != null)
            {
                oldCollection.ParentSurface = null;
                oldCollection.CollectionChanged -= AnnotationCollectionChanged;
            }
            if (newCollection != null)
            {
                newCollection.ParentSurface = ultraChartSurface;
                newCollection.CollectionChanged += AnnotationCollectionChanged;
                ultraChartSurface.InvalidateElement();
            }
            if (ultraChartSurface.ChartModifier != null)
            {
                AnnotationCollectionChanged(ultraChartSurface, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
        private static void AnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            var annotationCollection = sender as AnnotationCollection;
            var parentSurface = annotationCollection != null ? annotationCollection.ParentSurface : sender as IUltrachartSurface;
            if (parentSurface != null && parentSurface.ChartModifier != null)
            {
                parentSurface.ChartModifier.OnAnnotationCollectionChanged(sender, args);
            }
        }
        private static void OnRenderableSeriesDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((UltrachartSurface)d);
            using (ultraChartSurface.SuspendUpdates())
            {
                ultraChartSurface._renderableSeries.Clear();
                var newCollection = e.NewValue as ObservableCollection<IRenderableSeries>;
                var oldCollection = e.OldValue as ObservableCollection<IRenderableSeries>;
                if (oldCollection != null)
                {
                    oldCollection.CollectionChanged -= ultraChartSurface.OnRenderableSeriesCollectionChanged;
                }
                if (newCollection != null)
                {
                    newCollection.CollectionChanged += ultraChartSurface.OnRenderableSeriesCollectionChanged;
                    newCollection.ForEachDo(ultraChartSurface._renderableSeries.Add);
                }
            }
        }
        private static void OnYAxesDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = (UltrachartSurface) d;
            var newCollection = e.NewValue as AxisCollection;
            var oldCollection = e.OldValue as AxisCollection;
            if (oldCollection != null)
            {
                foreach (var oldItem in oldCollection)
                {
                    ultraChartSurface.DetachAxis(oldItem);
                }
                oldCollection.CollectionChanged -= ultraChartSurface.OnYAxesCollectionChanged;
            }
            if (newCollection != null)
            {
                newCollection.CollectionChanged += ultraChartSurface.OnYAxesCollectionChanged;
                foreach (var newItem in newCollection)
                {
                    ultraChartSurface.AttachAxis(newItem, false);
                }
            }
            if (ultraChartSurface.Annotations != null)
            {
                ultraChartSurface.Annotations.OnYAxesCollectionChanged(ultraChartSurface, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            if (ultraChartSurface.ChartModifier != null)
            {
                ultraChartSurface.ChartModifier.OnYAxesCollectionChanged(ultraChartSurface, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
        private static void OnXAxesDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = (UltrachartSurface) d;
            var newCollection = e.NewValue as AxisCollection;
            var oldCollection = e.OldValue as AxisCollection;
            if (oldCollection != null)
            {
                foreach (var oldAxis in oldCollection)
                {
                    ultraChartSurface.DetachAxis(oldAxis);
                }
                oldCollection.CollectionChanged -= ultraChartSurface.OnXAxesCollectionChanged;
            }
            if (newCollection != null)
            {
                newCollection.CollectionChanged += ultraChartSurface.OnXAxesCollectionChanged;
                foreach (var newAxis in newCollection)
                {
                    ultraChartSurface.AttachAxis(newAxis, true);
                }
            }
            if (ultraChartSurface.Annotations != null)
            {
                ultraChartSurface.Annotations.OnXAxesCollectionChanged(ultraChartSurface, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
            if (ultraChartSurface.ChartModifier != null)
            {
                ultraChartSurface.ChartModifier.OnXAxesCollectionChanged(ultraChartSurface, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
        private static void OnYAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((UltrachartSurface)d);
            var oldAxis = e.OldValue as IAxis;
            var newAxis = e.NewValue as IAxis;
            if (oldAxis != null)
            {
                ultraChartSurface.YAxes.Remove(oldAxis);
            }
            if (newAxis != null)
            {
                UltrachartDebugLogger.Instance.WriteLine("Inserting Primary Y-Axis");
                ultraChartSurface.YAxes.Insert(0, newAxis);
            }
        }
        private static void OnXAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((UltrachartSurface)d);
            var oldAxis = e.OldValue as IAxis;
            if (oldAxis != null)
            {
                ultraChartSurface.XAxes.Remove(oldAxis);
            }
            var newAxis = e.NewValue as IAxis;
            if (newAxis != null)
            {
                UltrachartDebugLogger.Instance.WriteLine("Inserting Primary X-Axis");
                ultraChartSurface.XAxes.Insert(0, newAxis);
            }
        }
        private static void OnIsPolarChartDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var surface = d as UltrachartSurface;
            if (surface != null)
            {
                var coordinateSystem = (bool)e.NewValue ? CoordinateSystem.Polar : CoordinateSystem.Cartesian;
                surface.Services.GetService<IEventAggregator>().Publish(new CoordinateSystemMessage(surface, coordinateSystem));
            }
        }
        private static void OnChildStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((UltrachartSurface)d);
            var panel = ultraChartSurface.GridLinesPanel as GridLinesPanel;
            if (panel != null)
            {
                panel.Style = ultraChartSurface.GridLinesPanelStyle;
            }
            var rs = ultraChartSurface.RenderSurface as IRenderSurface2D;
            if (rs != null)
            {
                rs.Style = ultraChartSurface.RenderSurfaceStyle;
            }
            UltrachartDebugLogger.Instance.WriteLine("OnChildStyleChanged");
        }        
        protected override void OnRenderSurfaceDependencyPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnRenderSurfaceDependencyPropertyChanged(e);
            var oldSurface = e.OldValue as IRenderSurface2D;
            if (oldSurface != null)
            {
                oldSurface.Draw -= OnRenderSurfaceDraw;
                oldSurface.Services = null;
                oldSurface.ClearSeries();
                oldSurface.Dispose();
            }
            var newSurface = e.NewValue as IRenderSurface2D;
            if (newSurface != null)
            {
                newSurface.Draw += OnRenderSurfaceDraw;
                newSurface.Services = Services;
                if (RenderSurfaceStyle != null)
                {
                    newSurface.Style = RenderSurfaceStyle;
                }
                newSurface.AddSeries(RenderableSeries);
            }
            _renderSurface = newSurface;
        }
        private static void OnSeriesSourceDependencyPropertyChanged(DependencyObject d,
                                                                    DependencyPropertyChangedEventArgs e)
        {
            var surface = ((UltrachartSurface)d);
            var newCollection = e.NewValue as ObservableCollection<IChartSeriesViewModel>;
            var oldCollection = e.OldValue as ObservableCollection<IChartSeriesViewModel>;
            if (oldCollection != null)
            {
                foreach (var oldItem in oldCollection)
                {
                    surface.DetachChartSeries(oldItem);
                }
                oldCollection.CollectionChanged -= surface.OnChartSeriesCollectionChanged;
            }
            if (newCollection != null)
            {
                newCollection.CollectionChanged += surface.OnChartSeriesCollectionChanged;
                foreach (var newItem in newCollection)
                {
                    surface.AttachChartSeries(newItem);
                }
            }
        }
        private static void OnChartModifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ultraChartSurface = ((UltrachartSurface)d);
            var oldModifier = e.OldValue as IChartModifier;
            if (oldModifier != null)
            {
                ultraChartSurface.DetachModifier(oldModifier);
            }
            var newModifier = e.NewValue as IChartModifier;
            if (newModifier != null)
            {
                ultraChartSurface.AttachModifier(newModifier);
            }
            UltrachartDebugLogger.Instance.WriteLine("OnChartModifierChanged");
            ultraChartSurface.InvalidateElement();
        }
        [Obsolete("GetDataSeriesFor is obsolete. Please call RenderableSeries.DataSeries to get the DataSeries", true)]
        internal IDataSeries GetDataSeriesFor(IRenderableSeries renderableSeries)
        {
            throw new NotImplementedException(
                "GetDataSeriesFor is obsolete. Please call RenderableSeries.DataSeries to get the DataSeries");
        }
        internal IEnumerable<IDataSeries> GetDataSeriesFor(string axisName)
        {
            if (RenderableSeries == null)
                yield break;
            foreach (var renderableSeries in RenderableSeries)
            {
                if (renderableSeries.YAxisId != axisName || !renderableSeries.IsVisible)
                    continue;
                var dataSeries = renderableSeries.DataSeries;
                if (dataSeries != null)
                    yield return dataSeries;
            }
        }
        /// <summary>
        /// Called in the constructor of <see cref="UltrachartSurfaceBase" />, gives derived classes the opportunity to register services per <see cref="UltrachartSurfaceBase" /> instance
        /// </summary>
        /// <returns>
        /// The populated <see cref="IServiceContainer" />. Must not return null. Return at least an empty <see cref="ServiceContainer" />
        /// </returns>
        protected override void RegisterServices(IServiceContainer serviceContainer)
        {
            base.RegisterServices(serviceContainer);
            
            serviceContainer.RegisterService<IUltrachartRenderer>(new UltrachartRenderer(this));
            serviceContainer.RegisterService<IMouseManager>(new MouseManager());
            serviceContainer.RegisterService<ICoordinateCalculatorFactory>(new CoordinateCalculatorFactory());
            serviceContainer.RegisterService<IPointResamplerFactory>(new PointResamplerFactory());
            serviceContainer.RegisterService<IUltrachartSurface>(this);
            serviceContainer.RegisterService<IStrategyManager>(new DefaultStrategyManager(this));
            _eventAggregator = serviceContainer.GetService<IEventAggregator>();
            _eventAggregator.Subscribe<ZoomExtentsMessage>(m =>
            {
                if (m.ZoomYOnly)
                {
                    ZoomExtentsY();
                }
                else
                {
                    ZoomExtents();
                }
            }, true);
            _eventAggregator.Subscribe<InvalidateUltrachartMessage>(m => InvalidateElement(), true);
            _ultraChartRenderer = serviceContainer.GetService<IUltrachartRenderer>();
        }
        private void AttachModifier(IChartModifier chartModifier)
        {
            if (chartModifier.IsAttached)
            {
                DetachModifier(chartModifier);
            }
            UltrachartDebugLogger.Instance.WriteLine("Attaching ChartModifier {0}", chartModifier.GetType());
            AttachAsVisualChild(chartModifier);
            chartModifier.ParentSurface = this;
            chartModifier.Services = Services;
            if (RootGrid != null)
                Services.GetService<IMouseManager>().Subscribe(RootGrid, chartModifier);
            chartModifier.DataContext = DataContext;
            chartModifier.IsAttached = true;
            chartModifier.OnAttached();
        }
        private void AttachAsVisualChild(IChartModifier chartModifier)
        {
            var visualModifier = chartModifier as FrameworkElement;
            if (visualModifier != null && visualModifier.Parent == null)
            {
                visualModifier.Visibility = Visibility.Collapsed;
                RootGrid.SafeAddChild(visualModifier);
            }
        }
        private void DetachAsVisualChild(IChartModifier chartModifier)
        {
            var visualModifier = chartModifier as FrameworkElement;
            if (visualModifier != null && visualModifier.Parent == RootGrid)
            {
                RootGrid.SafeRemoveChild(visualModifier);
            }
        }
        private void DetachModifier(IChartModifier chartModifier)
        {
            if (!chartModifier.IsAttached)
                return;
            UltrachartDebugLogger.Instance.WriteLine("Dettaching ChartModifier {0}", chartModifier.GetType());
            UnsubscribeFromMouseEvents();
            chartModifier.OnDetached();
            chartModifier.ParentSurface = null;
            chartModifier.Services = null;
            chartModifier.IsAttached = false;
            DetachAsVisualChild(chartModifier);
        }
        private void UnsubscribeFromMouseEvents()
        {
            if (RootGrid != null && Services != null)
            {
                Services.GetService<IMouseManager>().Unsubscribe(RootGrid);
            }
        }
        private void ChartSeriesViewModelPropertyListener(object sender, PropertyChangedEventArgs e)
        {
            var cvm = (IChartSeriesViewModel)sender;
            int index = SeriesSource.IndexOf(cvm);
            if (index == -1)
            {
                cvm.PropertyChanged -= ChartSeriesViewModelPropertyListener;
                return;
            }
            if (e.PropertyName == "DataSeries")
            {
                Guard.NotNull(cvm.DataSeries, "IChartSeriesViewModel.DataSeries");
                if (cvm.RenderSeries != null)
                {
                    cvm.RenderSeries.DataSeries = cvm.DataSeries;
                }
            }
            if (e.PropertyName == "RenderSeries")
            {
                if (cvm.RenderSeries == null)
                {
                    RenderableSeries.RemoveAt(index);
                }
                else
                {
                    RenderableSeries[index] = cvm.RenderSeries;
                    cvm.RenderSeries.DataSeries = cvm.DataSeries;
                }
            }
        }
        private void OnChartSeriesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            using (SuspendUpdates())
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    RenderableSeries.Clear();
                }
                if (e.OldItems != null)
                {
                    e.OldItems.Cast<IChartSeriesViewModel>().ForEachDo(DetachChartSeries);
                }
                if (e.NewItems != null)
                {
                    e.NewItems.Cast<IChartSeriesViewModel>().ForEachDo(AttachChartSeries);
                }
            }
        }
        internal class RenderableSeriesCollection : ObservableCollection<IRenderableSeries>
        {
            private readonly UltrachartSurface _parentSurface;
            public RenderableSeriesCollection(UltrachartSurface parentSurface)
            {
                _parentSurface = parentSurface;
            }
            protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                using (_parentSurface.SuspendUpdates())
                {
                    if (e.OldItems != null)
                    {
                        e.OldItems.Cast<IRenderableSeries>().ForEachDo(_parentSurface.DetachRenderableSeries);
                    }
                    if (e.NewItems != null)
                    {
                        e.NewItems.Cast<IRenderableSeries>().ForEachDo(_parentSurface.AttachRenderableSeries);
                    }
                }
                base.OnCollectionChanged(e);
            }
            protected override void ClearItems()
            {
                using (_parentSurface.SuspendUpdates())
                {
                    this.ForEachDo(_parentSurface.DetachRenderableSeries);
                }
                base.ClearItems();
            }
        }
        // Area used for Unit Test access
        // 
        /// <summary>
        /// Gets the Bottom AxisArea, which contains the Axes which have <see cref="AxisBase.AxisAlignment"/> set to <see cref="AxisAlignment.Bottom"/> 
        /// </summary>
        public AxisArea AxisAreaBottom
        {
            get { return _bottomAxisArea; }
        }
        /// <summary>
        /// Gets the Right AxisArea, which contains the Axes which have <see cref="AxisBase.AxisAlignment"/> set to <see cref="AxisAlignment.Right"/> 
        /// </summary>
        public AxisArea AxisAreaRight
        {
            get { return _rightAxisArea; }
        }
        /// <summary>
        /// Gets the Top AxisArea, which contains the Axes which have <see cref="AxisBase.AxisAlignment"/> set to <see cref="AxisAlignment.Top"/> 
        /// </summary>
        public AxisArea AxisAreaTop
        {
            get { return _topAxisArea; }
        }
        /// <summary>
        /// Gets the Left AxisArea, which contains the Axes which have <see cref="AxisBase.AxisAlignment"/> set to <see cref="AxisAlignment.Left"/> 
        /// </summary>
        public AxisArea AxisAreaLeft
        {
            get { return _leftAxisArea; }
        }
        /// <summary>
        /// Gets the center AxisArea which contains XAxes with <see cref="IAxis.IsCenterAxis"/> flag equals to true
        /// </summary>
        public AxisArea CenterXAxisArea
        {
            get { return _centerXAxisArea; }
        }
        /// <summary>
        /// Gets the center AxisArea which contains YAxes with <see cref="IAxis.IsCenterAxis"/> flag equals to true
        /// </summary>
        public AxisArea CenterYAxisArea
        {
            get { return _centerYAxisArea; }
        }
        /// <summary>
        /// Gets <see cref="StackedColumnsWrapper"/> that allows user to customize drawing of <see cref="StackedColumnRenderableSeries"/>
        /// </summary>
        internal IStackedColumnsWrapper StackedColumnsWrapper { get; set; }
                   
        /// <summary>
        /// Gets <see cref="StackedMountainsWrapper"/> that allows user to customize drawing of <see cref="StackedMountainRenderableSeries"/>
        /// </summary>
        internal IStackedMountainsWrapper StackedMountainsWrapper { get; set; }
        internal RenderableSeriesCollection RenderableSeriesInternal
        {
            get { return _renderableSeries; }
        }
        /// <summary>
        /// Internal Ctor used for tests
        /// </summary>
        /// <param name="mockServices"></param>
        internal UltrachartSurface(IServiceContainer mockServices)
            : this()
        {
            Services = mockServices;
            _ultraChartRenderer = mockServices.GetService<IUltrachartRenderer>();
            _eventAggregator = mockServices.GetService<IEventAggregator>();
        }
        internal HashSet<IDataSeries> DataSeriesToNotify { get { return _dsToNotify; } } 
    }
    /// <summary>
    /// Enumeration constants to define the render priority for series rendering on the <see cref="UltrachartSurface"/>
    /// </summary>
    public enum RenderPriority
    {
        /// <summary>
        /// Renders immediately on data update, as opposed to waiting for the CompositionTarget.Rendering event
        /// </summary>
        Immediate,
        /// <summary>
        /// Ultrachart renders whenever there is new data and the CompositionTarget.Rendering event has fired.
        /// This is the default option
        /// </summary>
        Normal,
        /// <summary>
        /// Ultrachart renders whenever there is new data and the CompositionTarget.Rendering event has fired, 
        /// but with a lower priority than input (mouse) events
        /// </summary>
        Low,
        /// <summary>
        /// Never redraws automatically. You must manually call InvalidateElement() or ZoomExtents() on the UltrachartSurface in order to get it to redraw
        /// </summary>
        Manual
    }
#if !Silverlight
    /// <summary>
    /// Provides values for exporting snapshot of <see cref="UltrachartSurface"/> to file.
    /// </summary>
    public enum ExportType
    {
        /// <summary>
        /// Export <see cref="UltrachartSurface"/> in PNG format
        /// </summary>
        Png,
        /// <summary>
        /// Export <see cref="UltrachartSurface"/> in JPEG format
        /// </summary>
        Jpeg,
        /// <summary>
        /// Export <see cref="UltrachartSurface"/> in BMP format
        /// </summary>
        Bmp
    }
#endif
}
