// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// CursorModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The CursorModifier provides a cross-hairs (cursor) plus tooltip with X,Y data values under the mouse as the mouse moves. 
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class CursorModifier : TooltipModifierBase
    {
        /// <summary>
        /// Defined IncludeSeries Attached Property
        /// </summary>
        public static readonly DependencyProperty IncludeSeriesProperty = DependencyProperty.RegisterAttached("IncludeSeries", typeof(bool), typeof(CursorModifier), new PropertyMetadata(true));

        /// <summary>
        /// Gets the include Series or not
        /// </summary>
        public static bool GetIncludeSeries(DependencyObject obj)
        {
            return (bool)obj.GetValue(IncludeSeriesProperty);
        }

        /// <summary>
        /// Sets the include Series or not
        /// </summary>
        public static void SetIncludeSeries(DependencyObject obj, bool value)
        {
            obj.SetValue(IncludeSeriesProperty, value);
        }

        /// <summary>
        /// Defines the ShowTooltip DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ShowTooltipProperty = DependencyProperty.Register("ShowTooltip", typeof(bool), typeof(CursorModifier), new PropertyMetadata(false, null));

        private Line _lineX;
        private Line _lineY;
        private Ellipse _cursorPoint;
        private TemplatableControl _cursorLabelCache;

        private ObservableCollection<AxisInfo> _axisInfo = new ObservableCollection<AxisInfo>();
        private AxisInfo _xAxisInfo;
        private AxisInfo _yAxisInfo;

        private const double CursorXyOffset = 6;

        /// <summary>
        /// Provides a collection of <see cref="AxisInfo"/> structs, which may be data-bound to in the UI defined by the <see cref="TooltipModifierBase.AxisLabelTemplate"/> Control template
        /// </summary>
        public ObservableCollection<AxisInfo> AxisInfo
        {
            get { return _axisInfo; }
            set
            {
                _axisInfo = value;
                OnPropertyChanged("AxisInfo");
            }
        }

        /// <summary>
        /// Provides an <see cref="AxisInfo"/> object, which may be data-bound to
        /// </summary>
        public AxisInfo XAxisInfo
        {
            get { return _xAxisInfo; }
            set
            {
                _xAxisInfo = value;
                OnPropertyChanged("XAxisInfo");
            }
        }

        /// <summary>
        /// Provides an <see cref="AxisInfo"/> object, which may be data-bound to
        /// </summary>
        public AxisInfo YAxisInfo
        {
            get { return _yAxisInfo; }
            set
            {
                _yAxisInfo = value;
                OnPropertyChanged("YAxisInfo");
            }
        }

        /// <summary>
        /// Gets or sets value, indicates whether show cursor tooltip or not
        /// </summary>
        /// <remarks></remarks>
        public bool ShowTooltip
        {
            get { return (bool)GetValue(ShowTooltipProperty); }
            set { SetValue(ShowTooltipProperty, value); }
        }

        /// <summary>
        /// Gets or sets delay for showing of tooltips in miliseconds
        /// </summary>
        public double HoverDelay
        {
            get { return (double)GetValue(HoverDelayProperty); }
            set { SetValue(HoverDelayProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CursorModifier"/> class.
        /// </summary>
        /// <remarks></remarks>
        public CursorModifier()
        {
            DefaultStyleKey = typeof(CursorModifier);

            this.SetCurrentValue(SeriesDataProperty, new ChartDataObject());

            _delayActionHelper = new DelayActionHelper {Interval = HoverDelay};
        }

        /// <summary>
        /// Called when the Chart Modifier is attached to the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnAttached()
        {
            base.OnAttached();

            CurrentPoint = new Point(double.NaN, double.NaN);

            ClearAll();
        }

        /// <summary>
        /// Called immediately before the Chart Modifier is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public override void OnDetached()
        {
            base.OnDetached();

            ClearAll();
        }

        /// <summary>
        /// Called when the mouse enters the parent <see cref="UltrachartSurface"/>
        /// </summary>
        protected override void OnParentSurfaceMouseEnter()
        {
            ClearAll();
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Master <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleMasterMouseEvent(Point mousePoint)
        {
            //UltrachartDebugLogger.Instance.WriteLine("CursorModifier Master MouseMove: {0}, {1}", mousePoint.X, mousePoint.Y);

            ShowCrosshairCursor(mousePoint);

            if (ShowTooltip || ShowAxisLabels)
            {
                GetSeriesData(mousePoint);
                GetAxesData(mousePoint);
            }

            if (ShowTooltip)
            {
                if (SeriesData.SeriesInfo.IsNullOrEmpty() || !HasToShowTooltip())
                {
                    ClearCursorOverlay();
                }
                else if (ShowTooltipOn == ShowTooltipOptions.MouseHover)
                {
                    ClearCursorOverlay();
                    _delayActionHelper.Start(() => PlaceTooltip(mousePoint));
                }
                else
                {
                    PlaceTooltip(mousePoint);
                }
            }

            if (ShowAxisLabels)
            {
                UpdateAxesLabels(mousePoint);
            }
        }

        private void PlaceTooltip(Point mousePoint)
        {
            var firstSeries = SeriesData.SeriesInfo != null ? SeriesData.SeriesInfo.FirstOrDefault() : null;
            var point = firstSeries != null ? firstSeries.XyCoordinate : mousePoint;
            if (IsEnabledAt(point))
            {
                UpdateCursorOverlay(point);
            }
            else
            {
                ClearCursorOverlay();
            }
        }

        private void UpdateAxesLabels(Point mousePoint)
        {
            if (!IsLabelsCacheActual())
            {
                RecreateLabels();
            }

            UpdateAxesOverlay(mousePoint);
        }

        private void GetSeriesData(Point currentPoint)
        {
            SeriesData.UpdateSeriesInfo(GetSeriesInfoAt(currentPoint));
        }

        /// <summary>
        /// Enumerates RenderableSeries on the parent <see cref="ChartModifierBase.ParentSurface"/> and gets <see cref="SeriesInfo"/> objects in given point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        protected override IEnumerable<SeriesInfo> GetSeriesInfoAt(Point point)
        {
            return GetSeriesInfoAt(renderSeries => renderSeries.VerticalSliceHitTest(point, UseInterpolation));
        }

        /// <summary>
        /// Performs a hit-test on all axes and aggregates data into the <see cref="AxisInfo"/> collection
        /// </summary>
        /// <param name="mousePoint"></param>
        protected virtual void GetAxesData(Point mousePoint)
        {
            var yHitTestInfos = YAxes.Select(a => HitTestAxis(a, mousePoint));
            YAxisInfo = yHitTestInfos.FirstOrDefault();

            var xHitTestInfos = XAxes.Select(a => HitTestAxis(a, mousePoint));
            XAxisInfo = xHitTestInfos.FirstOrDefault();

            var allInfo = new ObservableCollection<AxisInfo>();
            allInfo.AddRange(xHitTestInfos);
            allInfo.AddRange(yHitTestInfos);

            AxisInfo = allInfo;
        }

        private void ShowCrosshairCursor(Point mousePoint)
        {
            var modifierRect = ModifierSurface.GetBoundsRelativeTo(RootGrid);

            if (_lineX == null || _lineY == null)
            {
                _lineX = new Line { Style = LineOverlayStyle, IsHitTestVisible = false };
                _lineY = new Line { Style = LineOverlayStyle, IsHitTestVisible = false };
                if (_cursorPoint == null)
                {
                    _cursorPoint = new Ellipse { Fill = _lineY.Stroke, Width = 5, Height = 5 };
                }
                ModifierSurface.Children.Add(_lineX);
                ModifierSurface.Children.Add(_lineY);
            }

            _lineX.X1 = 0;
            _lineX.X2 = modifierRect.Width - 1;
            _lineX.Y1 = mousePoint.Y;
            _lineX.Y2 = mousePoint.Y;

            _lineY.X1 = mousePoint.X;
            _lineY.X2 = mousePoint.X;
            _lineY.Y1 = 0;
            _lineY.Y2 = modifierRect.Height - 1;
        }

        /// <summary>
        /// When overriden in a derived class, called to handle the Slave <see cref="ChartModifierBase" /> MouseMove event
        /// </summary>
        /// <param name="mousePoint">The current Mouse-point</param>
        protected override void HandleSlaveMouseEvent(Point mousePoint)
        {
            UltrachartDebugLogger.Instance.WriteLine("CursorModifier Slave MouseMove: {0}, {1}", mousePoint.X, mousePoint.Y);

            var modifierRect = ModifierSurface.GetBoundsRelativeTo(RootGrid);

            if (_lineY == null)
            {
                _lineY = new Line { Style = LineOverlayStyle, IsHitTestVisible = false };

                ModifierSurface.Children.Add(_lineY);
            }

            _lineY.X1 = mousePoint.X;
            _lineY.X2 = mousePoint.X;
            _lineY.Y1 = 0;
            _lineY.Y2 = modifierRect.Height - 1;
        }

        
        /// <summary>
        /// When overriden in a derived class, this method should clear all markers and tooltips from the <see cref="UltrachartSurface.ModifierSurface" />
        /// </summary>
        protected override void ClearAll()
        {
            if (ModifierSurface != null)
            {
                if (_lineX != null && ModifierSurface.Children.Contains(_lineX))
                {
                    ModifierSurface.Children.Remove(_lineX);
                    _lineX = null;
                }
                if (_lineY != null && ModifierSurface.Children.Contains(_lineY))
                {
                    ModifierSurface.Children.Remove(_lineY);
                    _lineY = null;
                }
            }

            ClearOverlay();
            CurrentPoint = new Point(double.NaN, double.NaN);

            _delayActionHelper.Stop();
        }

        private void ClearOverlay()
        {
            ClearCursorOverlay();
            ClearAxesOverlay();
        }

        private void ClearCursorOverlay()
        {
            if (_cursorLabelCache == null || ModifierSurface == null)
                return;

            if (ModifierSurface.Children.Contains(_cursorLabelCache))
            {
                ModifierSurface.Children.Remove(_cursorLabelCache);
            }

            if (ModifierSurface.Children.Contains(_cursorPoint))
            {
                ModifierSurface.Children.Remove(_cursorPoint);
            }
        }
        
        private void UpdateCursorOverlay(Point mousePoint)
        {
            if (_cursorLabelCache == null || !ShowTooltip)
                return;

            PlaceOverlay(_cursorLabelCache, mousePoint);
            Canvas.SetLeft(_cursorPoint, mousePoint.X - _cursorPoint.ActualWidth * 0.5);
            Canvas.SetTop(_cursorPoint, mousePoint.Y - _cursorPoint.ActualHeight * 0.5);

            if (!ModifierSurface.Children.Contains(_cursorLabelCache))
                ModifierSurface.Children.Add(_cursorLabelCache);
            if (!ModifierSurface.Children.Contains(_cursorPoint))
                ModifierSurface.Children.Add(_cursorPoint);

#if SILVERLIGHT
            // Fix for SC-2104: Need to update cursor label layout to apply template from tooltip selector
            _cursorLabelCache.MeasureArrange();
#endif
        }

        private void PlaceOverlay(FrameworkElement overlay, Point mousePoint)
        {
            var modifierRect = new Rect(0, 0, ModifierSurface.ActualWidth, ModifierSurface.ActualHeight);

            var x = mousePoint.X + CursorXyOffset;
            var y = mousePoint.Y + CursorXyOffset;

            var overlayRect = new Rect(x, y, overlay.ActualWidth, overlay.ActualHeight);

            if (modifierRect.Right < overlayRect.Right)
            {
                x = mousePoint.X - overlayRect.Width - CursorXyOffset;
            }

            if (modifierRect.Bottom < overlayRect.Bottom)
            {
                var diff = overlayRect.Bottom - modifierRect.Bottom;
                y = y - diff;
                // This check needed to prevent cutting tooltip at the top (if tooltip placed at the lower bound)
                y = y < 0 ? 0 : y;
            }

            Canvas.SetLeft(overlay, x);
            Canvas.SetTop(overlay, y < 0 ? 0 : y);
        }


        /// <summary>
        /// When overriden in a derived class, applies new template to tooltip label, <seealso cref="TooltipModifierBase.TooltipLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.TooltipLabelTemplate" />
        /// </summary>
        protected override void OnTooltipLabelTemplateChanged()
        {
            var controlTemplate = TooltipLabelTemplate;

            _cursorLabelCache = CreateFromTemplate(controlTemplate, TooltipLabelTemplateSelector, this);
        }

        /// <summary>
        /// When overriden in a derived class, applies new template to axis labels, <seealso cref="TooltipModifierBase.AxisLabelTemplateSelector" /> and <seealso cref="TooltipModifierBase.AxisLabelTemplate" />
        /// </summary>
        protected override void OnAxisLabelTemplateChanged()
        {
            RecreateLabels();
        }
    }
}