using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ecng.Xaml.Charting.Model.DataSeries;

namespace Ecng.Xaml.Charting.Visuals
{
    public class UltrachartPerformanceOverlay : ContentControl
    {
        public static readonly DependencyProperty TargetSurfaceProperty =
            DependencyProperty.Register("TargetSurface", typeof(IUltrachartSurface), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(default(IUltrachartSurface), OnTargetSurfaceDependencyPropertyChanged));

        public static readonly DependencyProperty WpfFpsProperty =
            DependencyProperty.Register("WpfFps", typeof(double), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty UltrachartFpsProperty =
            DependencyProperty.Register("UltrachartFps", typeof(double), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(default(double)));

        public static readonly DependencyProperty WpfFpsSeriesProperty =
            DependencyProperty.Register("WpfFpsSeries", typeof(XyDataSeries<double, double>), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(default(XyDataSeries<double, double>)));

        public static readonly DependencyProperty UltrachartFpsSeriesProperty =
            DependencyProperty.Register("UltrachartFpsSeries", typeof(XyDataSeries<double, double>), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(default(XyDataSeries<double, double>)));

        public static readonly DependencyProperty SmoothingWindowSizeProperty =
            DependencyProperty.Register("SmoothingWindowSize", typeof(int), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(20));

        public static readonly DependencyProperty TotalPointCountProperty =
            DependencyProperty.Register("TotalPointCount", typeof(int), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ChartsVisibilityProperty = 
            DependencyProperty.Register("ChartsVisibility", typeof(Visibility), typeof(UltrachartPerformanceOverlay), new PropertyMetadata(Visibility.Visible));

        public UltrachartPerformanceOverlay()
        {
            DefaultStyleKey = typeof (UltrachartPerformanceOverlay);
        }

        public Visibility ChartsVisibility
        {
            get { return (Visibility)GetValue(ChartsVisibilityProperty); }
            set { SetValue(ChartsVisibilityProperty, value); }
        }

        public int TotalPointCount
        {
            get { return (int)GetValue(TotalPointCountProperty); }
            set { SetValue(TotalPointCountProperty, value); }
        }

        public int SmoothingWindowSize
        {
            get { return (int)GetValue(SmoothingWindowSizeProperty); }
            set { SetValue(SmoothingWindowSizeProperty, value); }
        }

        public XyDataSeries<double, double> UltrachartFpsSeries
        {
            get { return (XyDataSeries<double, double>)GetValue(UltrachartFpsSeriesProperty); }
            set { SetValue(UltrachartFpsSeriesProperty, value); }
        }

        public XyDataSeries<double, double> WpfFpsSeries
        {
            get { return (XyDataSeries<double, double>)GetValue(WpfFpsSeriesProperty); }
            set { SetValue(WpfFpsSeriesProperty, value); }
        }

        private Stopwatch _stopWatch;
        private double _lastUltrachartRenderTime;
        private double _lastWpfRenderTime;       

        public double WpfFps
        {
            get { return (double)GetValue(WpfFpsProperty); }
            set { SetValue(WpfFpsProperty, value); }
        }

        public IUltrachartSurface TargetSurface
        {
            get { return (IUltrachartSurface)GetValue(TargetSurfaceProperty); }
            set { SetValue(TargetSurfaceProperty, value); }
        }

        public double UltrachartFps
        {
            get { return (double)GetValue(UltrachartFpsProperty); }
            set { SetValue(UltrachartFpsProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        private static void OnTargetSurfaceDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fpsControl = (UltrachartPerformanceOverlay)d;
            var scs = e.NewValue as UltrachartSurface;

            if (scs == null)
                return;

            scs.Loaded -= fpsControl.OnUltrachartSurfaceLoaded;
            scs.Loaded += fpsControl.OnUltrachartSurfaceLoaded;

            scs.Unloaded -= fpsControl.OnUltrachartSurfaceUnloaded;
            scs.Unloaded += fpsControl.OnUltrachartSurfaceUnloaded;

            if (scs.IsLoaded)
            {
                fpsControl.OnUltrachartSurfaceLoaded(scs, null);
            }
        }

        private void OnUltrachartSurfaceUnloaded(object sender, RoutedEventArgs e)
        {
            var scs = sender as UltrachartSurface;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
            scs.Rendered -= OnUltrachartSurfaceRendered;
            _stopWatch.Stop();
        }

        private void OnUltrachartSurfaceLoaded(object sender, RoutedEventArgs e)
        {
            //if (ChartsVisibility == Visibility.Collapsed) return;

            var scs = sender as UltrachartSurface;
            _stopWatch = Stopwatch.StartNew();
            _lastUltrachartRenderTime = 0.0;
            _lastWpfRenderTime = 0.0;
            UltrachartFpsSeries = new XyDataSeries<double, double>() { FifoCapacity = SmoothingWindowSize };
            WpfFpsSeries = new XyDataSeries<double, double>() { FifoCapacity = SmoothingWindowSize };

            TotalPointCount = 0;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
            scs.Rendered += OnUltrachartSurfaceRendered;
        }

        private void OnUltrachartSurfaceRendered(object sender, EventArgs e)
        {
            double lastFps = 1000.0 / (_stopWatch.ElapsedMilliseconds - _lastUltrachartRenderTime);
            _lastUltrachartRenderTime = _stopWatch.ElapsedMilliseconds;
            int x = (int)(UltrachartFpsSeries.Count == 0 ? 0 : UltrachartFpsSeries.XValues.Last() + 1);
            UltrachartFpsSeries.Append(x, lastFps);

            double averageFps = UltrachartFpsSeries.YValues.Sum() / UltrachartFpsSeries.Count;

            UltrachartFps = averageFps;

            TotalPointCount = TargetSurface.RenderableSeries.Sum(r => r.DataSeries != null ? r.DataSeries.Count : 0);
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            double lastFps = 1000.0 / (_stopWatch.ElapsedMilliseconds - _lastWpfRenderTime);
            if (double.IsInfinity(lastFps)) return;

            _lastWpfRenderTime = _stopWatch.ElapsedMilliseconds;
            int x = (int)(WpfFpsSeries.Count == 0 ? 0 : WpfFpsSeries.XValues.Last() + 1);
            WpfFpsSeries.Append(x, lastFps);

            double averageFps = WpfFpsSeries.YValues.Sum() / WpfFpsSeries.Count;

            WpfFps = averageFps;
        }
    }
}
