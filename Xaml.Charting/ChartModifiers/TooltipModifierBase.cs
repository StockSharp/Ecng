// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TooltipModifierBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Themes;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Specifies actions that cause roolover tooltip to be shown
    /// </summary>
    public enum ShowTooltipOptions
    {
        /// <summary>
        /// Show tooltip when mouse is over point
        /// </summary>
        MouseOver,

        /// <summary>
        /// Show tooltip when mouse hovers over the surface
        /// </summary>
        MouseHover,

        /// <summary>
        /// Show tooltip always
        /// </summary>
        Always,

        /// <summary>
        /// Show tooltip when mouse left button is pressed
        /// </summary>
        MouseLeftButtonDown,

        /// <summary>
        /// Show tooltip when mouse right button is pressed
        /// </summary>
        MouseRightButtonDown,

        /// <summary>
        /// Show tooltip when mouse middle button is pressed
        /// </summary>
        MouseMiddleButtonDown
    }

    /// <summary>
    /// The <see cref="TooltipModifierBase"/> is part of the ChartModifier API, which factors out handling of Axis and Chart Label templates, and provides a mouse-over templated tooltip to bind to a custom DataContext, provided by the output of the Hit-Test operation on a <see cref="IRenderableSeries"/>
    /// </summary>
    /// <seealso cref="ChartModifierBase"/>
    /// <seealso cref="RolloverModifier"/>
    /// <seealso cref="CursorModifier"/>
    /// <seealso cref="TooltipModifier"/>
    public abstract class TooltipModifierBase : InspectSeriesModifierBase
    {
        /// <summary>
        /// Defines the ShowTooltipOn DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowTooltipOnProperty = DependencyProperty.Register("ShowTooltipOn", typeof(ShowTooltipOptions), typeof(TooltipModifierBase), new PropertyMetadata(default(ShowTooltipOptions)));

        /// <summary>
        /// Defines the LineOverlayStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty LineOverlayStyleProperty = DependencyProperty.Register("LineOverlayStyle", typeof(Style), typeof(InspectSeriesModifierBase), new PropertyMetadata(null));

        /// <summary>
        /// Defines the AxisLabelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AxisLabelTemplateProperty = DependencyProperty.Register("AxisLabelTemplate", typeof(ControlTemplate), typeof(TooltipModifierBase), new PropertyMetadata(OnAxisLabelTemplatePropertyChanged));

        /// <summary>
        /// Defines the ShowAxisLabels DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowAxisLabelsProperty = DependencyProperty.Register("ShowAxisLabels", typeof(bool), typeof(TooltipModifierBase), new PropertyMetadata(true, null));

        /// <summary>
        /// Defines the LabelTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TooltipLabelTemplateProperty = DependencyProperty.Register("TooltipLabelTemplate", typeof(ControlTemplate), typeof(TooltipModifierBase), new PropertyMetadata(OnTooltipLabelTemplatePropertyChanged));

        /// <summary>
        /// Defines the AxisLabelTemplateSelector DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AxisLabelTemplateSelectorProperty = DependencyProperty.Register("AxisLabelTemplateSelector", typeof(IDataTemplateSelector), typeof(TooltipModifierBase), new PropertyMetadata(OnAxisLabelTemplatePropertyChanged));

        /// <summary>
        /// Defines the LabelTemplateSelector DependencyProperty
        /// </summary>
        public static readonly DependencyProperty TooltipLabelTemplateSelectorProperty = DependencyProperty.Register("TooltipLabelTemplateSelector", typeof(IDataTemplateSelector), typeof(TooltipModifierBase), new PropertyMetadata(OnTooltipLabelTemplatePropertyChanged));

        /// <summary>
        /// Defines the DefaultAxisLabelTemplateSelector DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DefaultAxisLabelTemplateSelectorProperty = DependencyProperty.Register("DefaultAxisLabelTemplateSelector", typeof (IDataTemplateSelector), typeof (TooltipModifierBase), new PropertyMetadata(default(IDataTemplateSelector)));

        /// <summary>
        /// Defines the DefaultAxisLabelTemplateSelectorStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DefaultAxisLabelTemplateSelectorStyleProperty = DependencyProperty.Register("DefaultAxisLabelTemplateSelectorStyle", typeof(Style), typeof(TooltipModifierBase), new PropertyMetadata(default(Style),OnDefaultAxisLabelTemplateSelectorStyleChanged));

        /// <summary>
        /// Defines the DefaultTooltipLabelTemplateSelector DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DefaultTooltipLabelTemplateSelectorProperty = DependencyProperty.Register("DefaultTooltipLabelTemplateSelector", typeof(IDataTemplateSelector), typeof(TooltipModifierBase), new PropertyMetadata(default(IDataTemplateSelector)));

        /// <summary>
        /// Defines the DefaultTooltipLabelTemplateSelectorStyle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DefaultTooltipLabelTemplateSelectorStyleProperty = DependencyProperty.Register("DefaultTooltipLabelTemplateSelectorStyle", typeof (Style), typeof (TooltipModifierBase), new PropertyMetadata(default(Style), OnDefaultLabelTooltipTemplateSelectorStyleChanged));

        /// <summary>
        /// Defines HoverDelay DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HoverDelayProperty = DependencyProperty.Register("HoverDelay", typeof (double), typeof (TooltipModifierBase), new PropertyMetadata(500d, OnHoverDelayDependencyPropertyChanged));

        private IEnumerable<Tuple<IAxis, FrameworkElement>> _xAxisLabelsCache;
        private IEnumerable<Tuple<IAxis, FrameworkElement>> _yAxisLabelsCache;

        internal DelayActionHelper _delayActionHelper;

        /// <summary>
        /// Gets or sets when to show tooltips
        /// </summary>
        public ShowTooltipOptions ShowTooltipOn
        {
            get { return (ShowTooltipOptions)GetValue(ShowTooltipOnProperty); }
            set { SetValue(ShowTooltipOnProperty, value); }
        }

        /// <summary>
        /// Gets or sets the style applied to the modifier overlays (TargetType=Line)
        /// </summary>
        /// <value>The crosshairs style.</value>
        /// <remarks></remarks>
        public Style LineOverlayStyle
        {
            get { return (Style)GetValue(LineOverlayStyleProperty); }
            set { SetValue(LineOverlayStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the AxisLabelTemplate used for the labels on axes
        /// </summary>
        /// <value>The axis label template.</value>
        /// <remarks></remarks>
        public ControlTemplate AxisLabelTemplate
        {
            get { return (ControlTemplate)GetValue(AxisLabelTemplateProperty); }
            set { SetValue(AxisLabelTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the TooltipLabelTemplate used for the labels on data-points
        /// </summary>
        /// <value>The axis label template.</value>
        /// <remarks></remarks>
        public ControlTemplate TooltipLabelTemplate
        {
            get { return (ControlTemplate)GetValue(TooltipLabelTemplateProperty); }
            set { SetValue(TooltipLabelTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets value, indicates whether show labels on axes or not
        /// </summary>
        /// <remarks></remarks>
        public bool ShowAxisLabels
        {
            get { return (bool)GetValue(ShowAxisLabelsProperty); }
            set { SetValue(ShowAxisLabelsProperty, value); }
        }

        /// <summary>
        /// Gets or sets instance of <see cref="IDataTemplateSelector"/> which is used by modifier
        /// </summary>
        /// <remarks></remarks>
        public IDataTemplateSelector AxisLabelTemplateSelector
        {
            get { return (IDataTemplateSelector)GetValue(AxisLabelTemplateSelectorProperty); }
            set { SetValue(AxisLabelTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Gets or sets instance of <see cref="IDataTemplateSelector"/> which is used by modifier
        /// </summary>
        /// <remarks></remarks>
        public IDataTemplateSelector TooltipLabelTemplateSelector
        {
            get { return (IDataTemplateSelector)GetValue(TooltipLabelTemplateSelectorProperty); }
            set { SetValue(TooltipLabelTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Gets default instance of <see cref="AxisLabelTemplateSelector"/>
        /// </summary>
        public IDataTemplateSelector DefaultAxisLabelTemplateSelector
        {
            get { return (IDataTemplateSelector)GetValue(DefaultAxisLabelTemplateSelectorProperty); }
            protected set { SetValue(DefaultAxisLabelTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Gets or sets style for <see cref="DefaultAxisLabelTemplateSelector"/>
        /// </summary>
        public Style DefaultAxisLabelTemplateSelectorStyle
        {
            get { return (Style)GetValue(DefaultAxisLabelTemplateSelectorStyleProperty); }
            set { SetValue(DefaultAxisLabelTemplateSelectorStyleProperty, value); }
        }

        /// <summary>
        /// Gets default instance of <see cref="TooltipLabelTemplateSelector"/>
        /// </summary>
        public IDataTemplateSelector DefaultTooltipLabelTemplateSelector
        {
            get { return (IDataTemplateSelector)GetValue(DefaultTooltipLabelTemplateSelectorProperty); }
            protected set { SetValue(DefaultTooltipLabelTemplateSelectorProperty, value); }
        }

        /// <summary>
        /// Gets or sets style for <see cref="DefaultTooltipLabelTemplateSelector"/>
        /// </summary>
        public Style DefaultTooltipLabelTemplateSelectorStyle
        {
            get { return (Style)GetValue(DefaultTooltipLabelTemplateSelectorStyleProperty); }
            set { SetValue(DefaultTooltipLabelTemplateSelectorStyleProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TooltipModifierBase" /> class.
        /// </summary>
        protected TooltipModifierBase()
        {
            DefaultAxisLabelTemplateSelector = new AxisInfoTemplateSelector() {Style = DefaultAxisLabelTemplateSelectorStyle};
            DefaultTooltipLabelTemplateSelector = new SeriesInfoTemplateSelector(){Style = DefaultTooltipLabelTemplateSelectorStyle};
        }

#if SILVERLIGHT
        private static StackPanel _measuringPanel = new StackPanel();

        protected void MeasureVisualChild(FrameworkElement visualChild)
        {
            // We need this trick to force measuring of visual element
            _measuringPanel.SafeAddChild(visualChild);
            _measuringPanel.UpdateLayout();

            visualChild.MeasureArrange();
        }
#endif
        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);

            HandleMouseButtonEvent(e);
        }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            base.OnModifierMouseUp(e);

            HandleMouseButtonEvent(e);
        }

        /// <summary>
        /// Called when the IsEnabled property changes on this <see cref="ChartModifierBase"/> instance
        /// </summary>
        /// <remarks></remarks>
        protected override void OnIsEnabledChanged()
        {
            ClearAll();
        }

        private void HandleMouseButtonEvent(ModifierMouseArgs e)
        {
            if (ShowTooltipOn == ShowTooltipOptions.MouseLeftButtonDown ||
                ShowTooltipOn == ShowTooltipOptions.MouseMiddleButtonDown ||
                ShowTooltipOn == ShowTooltipOptions.MouseRightButtonDown)
            {
                var currentButton = e.MouseButtons;

                // need to set None value to provide correct handling of click without mouse moving
                e.MouseButtons = MouseButtons.None;
                HandleMouseEvent(e);
                e.MouseButtons = currentButton;

                e.Handled = false;
            }
        }

        /// <summary>
        /// Checks <see cref="ShowTooltipOn"/> property and returns value, indicating whether the tooltip has to be shown
        /// </summary>
        /// <returns></returns>
        protected bool HasToShowTooltip()
        {
            bool result = ShowTooltipOn == ShowTooltipOptions.Always || ShowTooltipOn == ShowTooltipOptions.MouseHover ||
                          (ShowTooltipOn == ShowTooltipOptions.MouseLeftButtonDown && IsMouseLeftButtonDown) ||
                          (ShowTooltipOn == ShowTooltipOptions.MouseMiddleButtonDown && IsMouseMiddleButtonDown) ||
                          (ShowTooltipOn == ShowTooltipOptions.MouseRightButtonDown && IsMouseRightButtonDown);

            return result;
        }

        /// <summary>
        /// When overridden in derived classes, indicates whether <see cref="HitTestInfo"/> result of hit-test should be returned from the <see cref="InspectSeriesModifierBase.GetSeriesInfoAt(Point)"/> method.
        /// </summary>
        /// <param name="hitTestInfo"></param>
        /// <returns></returns>
        protected override bool IsHitPointValid(HitTestInfo hitTestInfo)
        {
            var isValid = !hitTestInfo.IsEmpty() && (hitTestInfo.IsHit || hitTestInfo.IsWithinDataBounds);

            return isValid;
        }

        /// <summary>
        /// Called when [X axes collection changed].
        /// </summary>
        protected override void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RecreateLabelsOnXAxes();
        }

        /// <summary>
        /// Called when [Y axes collection changed].
        /// </summary>
        protected override void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RecreateLabelsOnYAxes();
        }

        protected bool IsLabelsCacheActual()
        {
            return _yAxisLabelsCache != null && _yAxisLabelsCache.Count() == YAxes.Count() &&
                   _xAxisLabelsCache != null && _xAxisLabelsCache.Count() == XAxes.Count();
        }

        /// <summary>
        /// Updates the Axes Overlays on the X and Y Axis
        /// </summary>
        protected void UpdateAxesOverlay(Point mousePoint)
        {
            foreach (var xAxis in XAxes)
            {
                var label = _xAxisLabelsCache.Where(x => x.Item1 == xAxis)
                    .Select(pair => pair.Item2)
                    .FirstOrDefault();

                UpdateAxisOverlay(mousePoint, xAxis, label);
            }

            foreach (var yAxis in YAxes)
            {
                var label = _yAxisLabelsCache.Where(x => x.Item1 == yAxis)
                    .Select(pair=>pair.Item2)
                    .FirstOrDefault();

                UpdateAxisOverlay(mousePoint, yAxis, label);
            }
        }

        private void UpdateAxisOverlay(Point mousePoint, IAxis axis, FrameworkElement axisLabel)
        {
            if (axisLabel == null || axis == null)
                return;

            var axisCanvas = axis.ModifierAxisCanvas;
            bool isPolarAxis = axis.IsPolarAxis;
            
            var strategy = Services.GetService<IStrategyManager>().GetTransformationStrategy();

            Point point = isPolarAxis
                ? strategy.Transform(mousePoint)
                : ParentSurface.ModifierSurface.TranslatePoint(mousePoint, axis);
            
            if (IsInBounds(point, axis))
            {
                axisLabel.DataContext = HitTestAxis(axis, mousePoint);

                if (isPolarAxis)
                {
                    SetPolarOffset(axisLabel, point);
                }
                else
                {
                    axis.SetHorizontalOffset(axisLabel, point);
                    axis.SetVerticalOffset(axisLabel, point);
                }

                axisCanvas.SafeAddChild(axisLabel);
            }
            else
            {
                axisCanvas.SafeRemoveChild(axisLabel);
            }
        }

        private static bool IsInBounds(Point point, IAxis axis)
        {
            double size, coordinate;
            if (axis.IsHorizontalAxis)
            {
                coordinate = point.X;
                size = axis.ActualWidth;
            }
            else
            {
                coordinate = point.Y;
                size = axis.ActualHeight;
            }

            return coordinate <= size && coordinate >= 0;
        }

        private void SetPolarOffset(FrameworkElement axisLabel, Point mousePoint)
        {
            var polarHitTestPoint = mousePoint;

            axisLabel.SetValue(AxisCanvas.BottomProperty, 0d);
            axisLabel.SetValue(AxisCanvas.CenterLeftProperty, polarHitTestPoint.X);
        }

        /// <summary>
        /// Clears Axis Overlays on the X and Y Axes 
        /// </summary>
        protected void ClearAxesOverlay()
        {
            ClearAxesOverlays(_xAxisLabelsCache);
            ClearAxesOverlays(_yAxisLabelsCache);
        }

        private void ClearAxesOverlays(IEnumerable<Tuple<IAxis, FrameworkElement>> labelsCache)
        {
            if (labelsCache != null && ParentSurface != null)
            {
                foreach (var axisLabelCache in labelsCache)
                {
                    var axis = axisLabelCache.Item1;
                    axis.ModifierAxisCanvas.SafeRemoveChild(axisLabelCache.Item2);
                }
            }
        }

        /// <summary>
        /// Recreates Labels on the X and Y Axis
        /// </summary>
        protected void RecreateLabels()
        {
            RecreateLabelsOnXAxes();
            RecreateLabelsOnYAxes();
        }

        private void RecreateLabelsOnXAxes()
        {
            ClearAxesOverlays(_xAxisLabelsCache);

            _xAxisLabelsCache = CreateLabelsFor(XAxes, AxisLabelTemplate);
        }

        private void RecreateLabelsOnYAxes()
        {
            ClearAxesOverlays(_yAxisLabelsCache);

            _yAxisLabelsCache = CreateLabelsFor(YAxes, AxisLabelTemplate);
        }

        protected IEnumerable<Tuple<IAxis, FrameworkElement>> CreateLabelsFor(IEnumerable<IAxis> axes, ControlTemplate labelTemplate)
        {
            IEnumerable<Tuple<IAxis, FrameworkElement>> axisLabelsCache = null;

            if (axes != null)
            {
                axisLabelsCache =
                    axes.Select(axis => new Tuple<IAxis, FrameworkElement>(axis, CreateFromTemplate(labelTemplate, AxisLabelTemplateSelector, null)))
                        .ToArray();
            }

            return axisLabelsCache;
        }

        /// <summary>
        /// Creates instances of <see cref="TemplatableControl"/> based on passed <paramref name="dataTemplateSelector"/>.
        /// If <paramref name="dataTemplateSelector"/> == Null, the control is created based on passed <paramref name="template"/>.
        /// </summary>
        /// <param name="template">The ControlTemplate to be applied</param>
        /// <param name="dataTemplateSelector">The object which is used to select a template for a new control</param>
        /// <param name="dataContext">The value which is used by <paramref name="dataTemplateSelector"/> to determine proper template</param>
        /// <returns></returns>
        protected TemplatableControl CreateFromTemplate(ControlTemplate template, IDataTemplateSelector dataTemplateSelector, object dataContext)
        {
            TemplatableControl instance = null;

            if (template != null)
            {
                instance = new TooltipControl()
                {
                    Template = template,
                    DataContext = dataContext,
                    Selector = dataTemplateSelector,
                };
            }

            return instance;
        }

        /// <summary>
        /// When overriden in a derived class, applies new template to tooltip label, <seealso cref="TooltipModifierBase.TooltipLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.TooltipLabelTemplate" />
        /// </summary>
        protected abstract void OnTooltipLabelTemplateChanged();

        /// <summary>
        /// When overriden in a derived class, applies new template to axis labels, <seealso cref="TooltipModifierBase.AxisLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.AxisLabelTemplate" />
        /// </summary>
        protected abstract void OnAxisLabelTemplateChanged();

        private static void OnAxisLabelTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = (TooltipModifierBase)d;

            if (modifier != null)
            {
                modifier.OnAxisLabelTemplateChanged();
            }
        }

        private static void OnTooltipLabelTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = (TooltipModifierBase)d;

            if (modifier != null)
            {
                modifier.OnTooltipLabelTemplateChanged();
            }
        }

        private static void OnDefaultAxisLabelTemplateSelectorStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = d as TooltipModifierBase;
            if (modifier != null)
            {
                ((DataTemplateSelector)modifier.DefaultAxisLabelTemplateSelector).Style = (Style)e.NewValue;
            }
        }

        private static void OnDefaultLabelTooltipTemplateSelectorStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = d as TooltipModifierBase;
            if (modifier != null)
            {
                ((DataTemplateSelector)modifier.DefaultTooltipLabelTemplateSelector).Style = (Style)e.NewValue;
            }
        }
        
        private static void OnHoverDelayDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modifier = d as TooltipModifierBase;
            if (modifier != null)
            {
                var delayHelper = modifier._delayActionHelper;
                if (delayHelper != null)
                {
                    delayHelper.Interval = (double) e.NewValue;
                }
            }
        }
    }
}