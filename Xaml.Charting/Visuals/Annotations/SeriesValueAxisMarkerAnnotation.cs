using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.ChartModifiers;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Used by <see cref="SeriesValueModifier"/> to create AxisMarkers which are bound to the series latest Y value in a viewport.
    /// </summary>
    public class SeriesValueAxisMarkerAnnotation: AxisMarkerAnnotation
    {
        /// <summary>
        /// Initializes a new <see cref="SeriesValueAxisMarkerAnnotation"/> instance.
        /// </summary>
        public SeriesValueAxisMarkerAnnotation()
        {
            DefaultStyleKey = typeof(SeriesValueAxisMarkerAnnotation);
        }
    }
}
