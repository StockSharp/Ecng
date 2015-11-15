using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Themes
{
    public class PolarTickLabelAxisCanvas : TickLabelAxisCanvas
    {
        public static readonly DependencyProperty MaxChildSizeProperty =
            DependencyProperty.Register("MaxChildSize", typeof (double), typeof (PolarTickLabelAxisCanvas), new PropertyMetadata(default(double)));

        
        private PolarCartesianTransformationHelper _transformationHelpser;
        private double _radius;

        public double MaxChildSize
        {
            get { return (double)GetValue(MaxChildSizeProperty); }
            set { SetValue(MaxChildSizeProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var desiredSize = base.MeasureOverride(constraint);

            MaxChildSize = GetMaxChildSize();

            return desiredSize;
        }

        private double GetMaxChildSize()
        {
            return Children.OfType<UIElement>().Select(x => x.DesiredSize.Height).MaxOrNullable() ?? 0d;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            _transformationHelpser = new PolarCartesianTransformationHelper(arrangeSize.Width, arrangeSize.Height);
            _radius = PolarUtil.CalculateViewportRadius(arrangeSize);

            return base.ArrangeOverride(arrangeSize);
        }

        protected override Rect GetArrangedRect(Size arrangeSize, UIElement element)
        {
            var arrangedRect = Rect.Empty;

            var label = element as DefaultTickLabel;
            if (label != null)
            {
                var coord = label.Position.X;
                var r = _radius - MaxChildSize/2;

                var point = _transformationHelpser.ToCartesian(coord, r);
                var x = point.X - element.DesiredSize.Width/2;
                var y = point.Y - element.DesiredSize.Height/2;

                var renderTransorm = new RotateTransform() {Angle = coord + 90, CenterX = 0.5, CenterY = 0.5};
                element.RenderTransform = renderTransorm;
                element.RenderTransformOrigin = new Point(0.5, 0.5);

                arrangedRect = new Rect(new Point(x, y), element.DesiredSize);
            }

            return arrangedRect;
        }
    }
}