using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// The RadialPanel provdes panel which rotates its children with specified angle relative to center of panel
    /// </summary>
    public class RadialPanel : Panel
    {
        /// <summary>
        /// Defines the Angle AttachedProperty
        /// </summary>
        public static readonly DependencyProperty AngleProperty = DependencyProperty.RegisterAttached("Angle", typeof(double), typeof(RadialPanel), new PropertyMetadata(default(double), InvalidateParentArrange));

        /// <summary>
        /// Defines the IsHorizontal AttachedProperty
        /// </summary>
        public static readonly DependencyProperty IsHorizontalProperty = DependencyProperty.RegisterAttached("IsHorizontal", typeof(bool), typeof(RadialPanel), new PropertyMetadata(default(bool)));

        /// <summary>
        /// Defines the OriginPoint AttachedProperty
        /// </summary>
        public static readonly DependencyProperty OriginPointProperty = DependencyProperty.RegisterAttached("OriginPoint", typeof(Point), typeof(RadialPanel), new PropertyMetadata(default(Point), InvalidateParentArrange));

        protected override Size MeasureOverride(Size availableSize)
        {
            var halfSize = GetSize(availableSize.Width, availableSize.Height); ;
            halfSize.Width /= 2;
            halfSize.Height /= 2;

            double maxSize = 0d;
            foreach (var element in Children.OfType<UIElement>().Where(x => x.IsVisible()))
            {
                element.Measure(halfSize);

                maxSize = Math.Max(maxSize, Math.Max(element.DesiredSize.Width, element.DesiredSize.Height));
            }

            var size = maxSize * 2;
            return new Size(size, size);
        }

        private Size GetSize(double width, double height)
        {
            width = Math.Max(width, 0);
            height = Math.Max(height, 0);

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            // X and Y are equal to half size of final size
            var center = new Point(finalSize.Width / 2, finalSize.Height / 2);

            foreach (var element in Children.OfType<UIElement>().Where(x => x.IsVisible()))
            {
                var isHorizontal = GetIsHorizontal(element);

                var size = isHorizontal
                    ? new Size(center.X, element.DesiredSize.Height)
                    : new Size(element.DesiredSize.Width, center.Y);

                var transformOrigin = GetOriginPoint(element);

                var location = new Point(center.X, center.Y);
                location.X -= size.Width * transformOrigin.X;
                location.Y -= size.Height * transformOrigin.Y;

                var angle = GetAngle(element);

                var renderTransorm = new RotateTransform()
                {
                    Angle = angle,
                    CenterX = transformOrigin.X,
                    CenterY = transformOrigin.Y
                };

                element.RenderTransform = renderTransorm;
                element.RenderTransformOrigin = transformOrigin;

                element.Arrange(new Rect(location, size));
            }

            return finalSize;
        }

        /// <summary>
        /// Gets the value of the Angle attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static double GetAngle(UIElement element)
        {
            return (double)element.GetValue(AngleProperty);
        }

        /// <summary>
        /// Sets the value of the Angle attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetAngle(UIElement element, double value)
        {
            element.SetValue(AngleProperty, value);
        }

        /// <summary>
        /// Sets the value of the OriginPoint attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetOriginPoint(UIElement element, Point value)
        {
            element.SetValue(OriginPointProperty, value);
        }

        /// <summary>
        /// Gets the value of the OriginPoint attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static Point GetOriginPoint(UIElement element)
        {
            return (Point)element.GetValue(OriginPointProperty);
        }

        /// <summary>
        /// Sets the value of the IsHorizontal attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static void SetIsHorizontal(UIElement element, bool value)
        {
            element.SetValue(IsHorizontalProperty, value);
        }

        /// <summary>
        /// Gets the value of the IsHorizontal attached property for a specified UIElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="value"></param>
        public static bool GetIsHorizontal(UIElement element)
        {
            return (bool)element.GetValue(IsHorizontalProperty);
        }

        private static void InvalidateParentArrange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as UIElement;
            if (element != null)
            {
                var radialPanel = VisualTreeHelper.GetParent(element) as RadialPanel;
                if (radialPanel != null)
                    radialPanel.InvalidateArrange();
            }
        }
    }
}
