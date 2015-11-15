using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    public class AxisTitle:ContentControl
    {
        // Using a DependencyProperty as the backing store for Orientation.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register("Orientation", typeof(Orientation), typeof(AxisTitle), new PropertyMetadata(default(Orientation)));

        public AxisTitle()
        {
            DefaultStyleKey = typeof(AxisTitle);
        }

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
    }
}
