using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a class for tick axis labels rendering.
    /// </summary>
    public class NumericTickLabel : DefaultTickLabel
    {
        /// <summary>
        /// a new instance of the <see cref="NumericTickLabel"/> class. 
        /// </summary>
        public NumericTickLabel()
        {
            DefaultStyleKey = typeof(NumericTickLabel);
        }
    }
}
