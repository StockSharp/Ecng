using System;
using System.Windows;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="MouseWheelZoomModifier"/> provides zooming (or shrinking) of the <see cref="UltrachartSurface"/> on mouse wheel scroll
    /// Add to a <see cref="UltrachartSurface"/> and set IsEnabled to true to enable this behaviour
    /// </summary>
    public class TimeframeSegmentWheelModifier : RelativeZoomModifierBase
    {
        /// <summary>
        /// Defines the ActionType DependencyProperty
        /// </summary>
        public static readonly DependencyProperty ActionTypeProperty = DependencyProperty.Register("ActionType", typeof(ActionType), typeof(TimeframeSegmentWheelModifier), new PropertyMetadata(ActionType.Pan,
            (sender, args) => {
                var mouseWheelZoomModifier = sender as TimeframeSegmentWheelModifier;

                var action = (ActionType)args.NewValue;

                if(mouseWheelZoomModifier != null) {
                    mouseWheelZoomModifier._performAction = (action == ActionType.Pan
                                                                ? new Action<Point, double>(mouseWheelZoomModifier.PerformPan)
                                                                : new Action<Point, double>(mouseWheelZoomModifier.PerformZoom));
                }
            }));

        private Action<Point, double> _performAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseWheelZoomModifier"/> class.
        /// </summary>
        /// <remarks></remarks>
        public TimeframeSegmentWheelModifier()
        {
            GrowFactor = 0.05;

            //use pan action by default
            _performAction = PerformPan;
        }

        /// <summary>
        /// Gets or sets the <see cref="ActionType"/> to perform on mouse-wheel interaction
        /// </summary>
        public ActionType ActionType
        {
            get { return (ActionType)GetValue(ActionTypeProperty); }
            set { SetValue(ActionTypeProperty, value); }
        }

        private void PerformZoom(Point point, double value)
        {
            PerformZoom(point, value, value);
        }

        /// <summary>
        /// Called when the Mouse Wheel is scrolled on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation</param>
        /// <remarks></remarks>
        public override void OnModifierMouseWheel(ModifierMouseArgs e)
        {
            base.OnModifierMouseWheel(e);

            const double mouseWheelDeltaCoef = 120;

            using (ParentSurface.SuspendUpdates())
            {
                double value = -e.Delta / mouseWheelDeltaCoef;

                var currDirection = XyDirection;
                var currAction = ActionType;

                switch(e.Modifier) {
                    case MouseModifier.None:
                        ActionType = ActionType.Pan;
                        XyDirection = XyDirection.YDirection;
                        break;
                    case MouseModifier.Alt:
                        ActionType = ActionType.Pan;
                        XyDirection = XyDirection.XDirection;
                        break;
                    case MouseModifier.Ctrl:
                        ActionType = ActionType.Zoom;
                        break;
                    default:
                        return;
                }

                e.Handled = true;

                var mousePoint = GetPointRelativeTo(e.MousePoint, ModifierSurface);
                _performAction(mousePoint, value);

                XyDirection = currDirection;
                ActionType = currAction;
            }
        }

        private void PerformPan(Point mousePoint, double value)
        {
            if (XyDirection == XyDirection.YDirection || XyDirection == XyDirection.XYDirection)
            {
                // Computation of new Y-Range
                foreach (var yAxis in YAxes)
                {
                    var size = yAxis.IsHorizontalAxis ? yAxis.Width : yAxis.Height;
                    var pixels = value*GrowFactor*size;
                    yAxis.Scroll(pixels, ClipMode.None);
                }

                UltrachartDebugLogger.Instance.WriteLine("Growing YRange: {0}", value);
            }

            if (XyDirection == XyDirection.XDirection || XyDirection == XyDirection.XYDirection)
            {
                // Scroll to new X-Axis range, based on start point (pixel), current point and the initial visible range
                foreach (var xAxis in XAxes)
                {
                    // don't pan on axes which have a different orientation than primary X axis
                    if (xAxis.IsHorizontalAxis != XAxis?.IsHorizontalAxis)
                        break;

                    var size = xAxis.IsHorizontalAxis ? xAxis.Width : xAxis.Height;
                    var pixels = -value * GrowFactor * size;

                    xAxis.Scroll(pixels, ClipMode.None);
                }

                UltrachartDebugLogger.Instance.WriteLine("Growing XRange: {0}", (int)value);
            }
        }
    }
}
