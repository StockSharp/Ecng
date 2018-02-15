// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartOverview.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows.Controls.Primitives;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.AttachedProperties;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// A slider that provides the a range
    /// </summary>
    [   TemplatePart(Name = "PART_Container", Type = typeof(Panel)),
        TemplatePart(Name = "PART_BackgroundSurface", Type = typeof(IUltrachartSurface)),
        TemplatePart(Name = "PART_Scrollbar", Type = typeof(UltrachartScrollbar)),]
    public class UltrachartOverview : Control, IInvalidatableElement
    {
        /// <summary>
        /// Provides the SeriesColor for IRenderableSeries
        /// </summary>
        public static readonly DependencyProperty SeriesColorProperty = DependencyProperty.Register("SeriesColor", typeof(Color), typeof(UltrachartOverview), new PropertyMetadata(default(Color)));

        /// <summary>
        /// Provides the AreaBrush for FastMountainRenderableSeries
        /// </summary>
        public static readonly DependencyProperty AreaBrushProperty = DependencyProperty.Register("AreaBrush", typeof(Brush), typeof(UltrachartOverview), new PropertyMetadata(default(Brush)));

        /// <summary>
        /// Provides the DataSeriesIndex for IRenderableSeries
        /// </summary>
        public static readonly DependencyProperty DataSeriesProperty = DependencyProperty.Register("DataSeries", typeof(IDataSeries), typeof(UltrachartOverview), new PropertyMetadata(null));

        /// <summary>
        /// Provides the ParentSurface which this overview control is associated with
        /// </summary>
        public static readonly DependencyProperty ParentSurfaceProperty = DependencyProperty.Register("ParentSurface", typeof (IUltrachartSurface), typeof (UltrachartOverview), new PropertyMetadata(default(IUltrachartSurface), OnParentSurfaceDependencyPropertyChanged));

        /// <summary>
        /// Defines the XAxisId DependencyProperty
        /// </summary>
        public static readonly DependencyProperty XAxisIdProperty = DependencyProperty.Register("XAxisId", typeof(string), typeof(UltrachartOverview), new PropertyMetadata(AxisBase.DefaultAxisId, OnAxisIdDependencyPropertyChanged));

        /// <summary>
        /// Selected range of the range slider
        /// </summary>
        public static readonly DependencyProperty SelectedRangeProperty = DependencyProperty.Register("SelectedRange", typeof (IRange), typeof (UltrachartOverview), new PropertyMetadata(default(IRange)));

        /// <summary>
        /// Defines the Axis DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AxisProperty = DependencyProperty.Register("Axis", typeof (IAxis), typeof (UltrachartOverview), new PropertyMetadata(default(IAxis),OnAxisDependencyPropertyChanged));

        /// <summary>
        /// Defines the ScrollbarStyle
        /// </summary>
        public static readonly DependencyProperty ScrollbarStyleProperty = DependencyProperty.Register("ScrollbarStyle", typeof (Style), typeof (UltrachartOverview), new PropertyMetadata(default(Style)));

        public static readonly DependencyProperty RenderableSeriesStyleProperty = DependencyProperty.Register("RenderableSeriesStyle", typeof (Style), typeof (UltrachartOverview), new PropertyMetadata(default(Style), OnRenderableSeriesStylePropertyChanged));        

        public static readonly DependencyProperty RenderableSeriesTypeProperty = DependencyProperty.Register(
            "RenderableSeriesType", typeof (Type), typeof (UltrachartOverview), new PropertyMetadata(default(Type), OnRenderableSeriesTypePropertyChanged));   
   


        private IUltrachartSurface _backgroundChartSurface;

        private PropertyChangeNotifier _renderableSeriesPropertyNotifier;
        private PropertyChangeNotifier _renderSeriesDataSeriesPropertyNotifier;
        private ObservableCollection<IRenderableSeries> _renderableSeries;

        private PropertyChangeNotifier _xAxesPropertyNotifier;
        private AxisCollection _axisCollection;

        private readonly RenderTimerHelper _renderTimerHelper;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UltrachartOverview()
        {
            DefaultStyleKey = typeof(UltrachartOverview);

            _renderTimerHelper = new RenderTimerHelper(InvalidateElement, new DispatcherUtil(this.Dispatcher));

            Loaded+=(sender, args) => _renderTimerHelper.OnLoaded();
            Unloaded += (sender, args) => _renderTimerHelper.OnUnlodaed();
        }

        /// <summary>
        /// Returns the <see cref="UltrachartSurface"/> instance that this Overview control hosts. 
        /// </summary>
        public IUltrachartSurface BackgroundChartSurface { get { return _backgroundChartSurface; } }

        /// <summary>
        /// Gets or sets the renderable series style to apply to the RenderableSeries behind the UltrachartOverview
        /// </summary>
        /// <value>
        public Style RenderableSeriesStyle
        {
            get { return (Style) GetValue(RenderableSeriesStyleProperty); }
            set { SetValue(RenderableSeriesStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the type of the renderable series to display behind the UltrachartOverview
        /// </summary>        
        public Type RenderableSeriesType
        {
            get { return (Type) GetValue(RenderableSeriesTypeProperty); }
            set { SetValue(RenderableSeriesTypeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the SeriesColor.
        /// </summary>
        /// <value>The color of the series.</value>
        /// <remarks></remarks>
        public Color SeriesColor
        {
            get { return (Color)GetValue(SeriesColorProperty); }
            set { SetValue(SeriesColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Area Brush for the <see cref="FastMountainRenderableSeries"/>. The mountain chart outline is specified by <see cref="BaseRenderableSeries.SeriesColor"/>
        /// </summary>
        public Brush AreaBrush
        {
            get { return (Brush)GetValue(AreaBrushProperty); }
            set { SetValue(AreaBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataSeries used to draw the background chart
        /// </summary>
        public IDataSeries DataSeries
        {
            get { return (IDataSeries)GetValue(DataSeriesProperty); }
            set { SetValue(DataSeriesProperty, value); }
        }

        /// <summary>
        /// Gets or sets which XAxis to bind the UltrachartOverview to, matching by string Id
        /// </summary>
        public string XAxisId
        {
            get { return (string)GetValue(XAxisIdProperty); }
            set { SetValue(XAxisIdProperty, value); }
        }

        /// <summary>
        /// Gets or sets the ParentSurface which this overview control is bound to
        /// </summary>
        public IUltrachartSurface ParentSurface
        {
            get { return (IUltrachartSurface)GetValue(ParentSurfaceProperty); }
            set { SetValue(ParentSurfaceProperty, value); }
        }

        /// <summary>
        /// Selected range of the range slider
        /// </summary>
        public IRange SelectedRange
        {
            get { return (IRange)GetValue(SelectedRangeProperty); }
            set { SetValue(SelectedRangeProperty, value); }
        }

        /// <summary>
        /// Gets current axis which this control is bound to
        /// </summary>
        public IAxis Axis
        {
            get { return (IAxis)GetValue(AxisProperty); }
            private set { SetValue(AxisProperty, value); }
        }

        /// <summary>
        /// Get or sets style for scrollbar which is used by this overview control
        /// </summary>
        public Style ScrollbarStyle
        {
            get { return (Style)GetValue(ScrollbarStyleProperty); }
            set { SetValue(ScrollbarStyleProperty, value); }
        }

        /// <summary>
        /// Asynchronously requests that the element redraws itself plus children.
        /// Will be ignored if the element is ISuspendable and currently IsSuspended (within a SuspendUpdates/ResumeUpdates call)
        /// </summary>
        public void InvalidateElement()
        {
            if (BackgroundChartSurface != null && Axis != null)
            {
                var dataRange = Axis.DataRange;
                if (dataRange == null) 
                    return;
                
                if (Axis.GrowBy != null)
                {
                    dataRange = dataRange.GrowBy(Axis.GrowBy.Min, Axis.GrowBy.Max);
                }

                BackgroundChartSurface.XAxis.VisibleRange = dataRange;
            }
        }
        
        /// <summary>
        /// Overide to get the visuals from the control template
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _backgroundChartSurface = this.GetTemplateChild("PART_BackgroundSurface") as IUltrachartSurface;

            SynchronizeXAxisType(Axis);
            SynchronizeRenderableSeriesType(RenderableSeriesStyle, RenderableSeriesType);

			if(this.GetTemplateChild("PART_Scrollbar") is UltrachartScrollbar bar) {
				bar.MouseWheel += (sender, args) => {
					var point = args.GetPosition(ParentSurface.RootGrid as UIElement);
					ParentSurface?.ChartModifier?.OnModifierMouseWheel(new ModifierMouseArgs(point, MouseButtons.None, MouseExtensions.GetCurrentModifier(), args.Delta, true));
				};
			}
		}

        private void SynchronizeXAxisType(IAxis xAxis)
        {
            if (BackgroundChartSurface != null && xAxis != null)
            {
                var clonedAxis = xAxis.Clone();
                
                clonedAxis.Visibility = Visibility.Collapsed;
                clonedAxis.ParentSurface = BackgroundChartSurface;
                clonedAxis.DrawMinorGridLines = false;
                clonedAxis.DrawMajorGridLines = false;

                var binding = new Binding("FlipCoordinates") {Source = xAxis, Mode = BindingMode.OneWay};
                ((AxisBase) clonedAxis).SetBinding(AxisBase.FlipCoordinatesProperty, binding);

                BackgroundChartSurface.XAxis = clonedAxis;
            }
        }

        private bool DoesSurfaceHaveThisDataSeries(IUltrachartSurface scs, IDataSeries dataSeries)
        {
            if (scs == null || scs.RenderableSeries.IsNullOrEmpty())
                return false;

            return scs.RenderableSeries.Any(x => x.DataSeries == dataSeries);
        }

        private static void OnParentSurfaceDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overview = d as UltrachartOverview;
            if (overview != null)
            {
                overview.OnParentSurfaceChanged(e);
            }
        }

        private void OnParentSurfaceChanged(DependencyPropertyChangedEventArgs e)
        {
            DisposeNotifiers();

            var scs = e.NewValue as UltrachartSurface;
            if (scs != null)
            {
                _renderableSeriesPropertyNotifier = new PropertyChangeNotifier(scs, UltrachartSurface.RenderableSeriesProperty);
                _renderableSeriesPropertyNotifier.ValueChanged += () => OnRenderableSeriesChanged((ObservableCollection<IRenderableSeries>)_renderableSeriesPropertyNotifier.Value);

                
                _xAxesPropertyNotifier = new PropertyChangeNotifier(scs, UltrachartSurface.XAxesProperty);
                _xAxesPropertyNotifier.ValueChanged += () => OnXAxesPropertyChanged((AxisCollection)_xAxesPropertyNotifier.Value);

                if (!DoesSurfaceHaveThisDataSeries(scs, DataSeries))
                {
                    this.SetCurrentValue(DataSeriesProperty, null);
                }

                OnRenderableSeriesChanged(scs.RenderableSeries);
                OnXAxesPropertyChanged(scs.XAxes);

            }
            else
            {
                DisposeNotifiers();
                this.SetCurrentValue(DataSeriesProperty, null);
                
            }
        }

        private void DisposeNotifiers()
        {
            if (_xAxesPropertyNotifier != null)
            {
                _xAxesPropertyNotifier.Dispose();
                _xAxesPropertyNotifier = null;
            }
            if (_renderableSeriesPropertyNotifier != null)
            {
                _renderableSeriesPropertyNotifier.Dispose();
                _renderSeriesDataSeriesPropertyNotifier = null;
            }
        }

        private void OnXAxesPropertyChanged(AxisCollection xAxes)
        {
            if (_axisCollection != null)
            {
                _axisCollection.CollectionChanged -= OnXAxesCollectionChanged;
            }

            _axisCollection = xAxes;

            if (_axisCollection != null)
            {
                _axisCollection.CollectionChanged += OnXAxesCollectionChanged;
            }

            OnXAxesCollectionChanged(this, null);
        }

        private void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            Axis = !_axisCollection.IsNullOrEmpty() ? _axisCollection.GetAxisById(XAxisId) : null;
        }

        private void OnRenderableSeriesChanged(ObservableCollection<IRenderableSeries> renderableSeries)
        {
            if (_renderableSeries != null)
            {
                _renderableSeries.CollectionChanged -= RenderableSeriesOnCollectionChanged;
                _renderableSeries = null;
            }

            _renderableSeries = renderableSeries;

            if (_renderableSeries != null)
            {
                _renderableSeries.CollectionChanged += RenderableSeriesOnCollectionChanged;
                RenderableSeriesOnCollectionChanged(this, null);
            }
        }

        private void RenderableSeriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (DataSeries == null && ParentSurface != null)
            {
                var renderableSeries = ParentSurface.RenderableSeries.FirstOrDefault();
                
                if (_renderSeriesDataSeriesPropertyNotifier != null)
                {
                    _renderSeriesDataSeriesPropertyNotifier.Dispose();
                    _renderSeriesDataSeriesPropertyNotifier = null;
                }

                if (renderableSeries != null)
                {
                    if (renderableSeries is BaseRenderableSeries)
                    {
                        _renderSeriesDataSeriesPropertyNotifier = new PropertyChangeNotifier(renderableSeries as BaseRenderableSeries, BaseRenderableSeries.DataSeriesProperty);
                        _renderSeriesDataSeriesPropertyNotifier.ValueChanged += () => OnDataSeriesDependencyPropertyChanged(renderableSeries.DataSeries);
                    }

                    OnDataSeriesDependencyPropertyChanged(renderableSeries.DataSeries);
                }
            }
        }

        private void OnDataSeriesDependencyPropertyChanged(IDataSeries dataSeries)
        {
            this.SetCurrentValue(DataSeriesProperty, dataSeries);
        }

        private static void OnAxisIdDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overview = d as UltrachartOverview;
            if (overview != null)
            {
                overview.OnXAxesCollectionChanged(overview,null);
            }
        }

        private static void OnAxisDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overview = d as UltrachartOverview;
            if (overview != null)
            {
                var oldAxis = e.OldValue as IAxis;
                if (oldAxis != null)
                {
                    oldAxis.DataRangeChanged -= overview.OnDataRangeChanged;
                }

                var newAxis = e.NewValue as IAxis;
                if (newAxis != null)
                {
                    newAxis.DataRangeChanged += overview.OnDataRangeChanged;

                    overview.SynchronizeXAxisType(newAxis);
                }
            }
        }

        private void OnDataRangeChanged(object sender, EventArgs e)
        {
            _renderTimerHelper.Invalidate();
        }

        private static void OnRenderableSeriesTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Type t = (Type)e.NewValue;
            if (t.IsAssignableFrom(typeof (BaseRenderableSeries)))
            {
                ((UltrachartOverview)d).SynchronizeRenderableSeriesType(((UltrachartOverview)d).RenderableSeriesStyle, t);
            }
        }

        private static void OnRenderableSeriesStylePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((UltrachartOverview)d).SynchronizeRenderableSeriesType(e.NewValue as Style, ((UltrachartOverview)d).RenderableSeriesType);
        }

        private void SynchronizeRenderableSeriesType(Style renderableSeriesStyle, Type renderableSeriesType)
        {
            if (_backgroundChartSurface != null)
            {
                if (_backgroundChartSurface.RenderableSeries.IsEmpty() ||
                    _backgroundChartSurface.RenderableSeries.First().GetType() != renderableSeriesType)
                {
                    var rs = (BaseRenderableSeries)Activator.CreateInstance(renderableSeriesType);
                    rs.SetCurrentValue(DataContextProperty, this);
                    if (renderableSeriesStyle == null || renderableSeriesStyle.TargetType.IsAssignableFrom(renderableSeriesType))
                    {
                        rs.Style = renderableSeriesStyle;
                    }                    
                    using (_backgroundChartSurface.SuspendUpdates())
                    {
                        _backgroundChartSurface.RenderableSeries.Clear();
                        _backgroundChartSurface.RenderableSeries.Add(rs);
                    }
                    return;
                }

                _backgroundChartSurface.RenderableSeries[0].Style = renderableSeriesStyle;
            }
        }
    }
}