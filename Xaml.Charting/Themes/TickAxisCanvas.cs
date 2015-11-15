using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Themes
{
    public class TickAxisCanvas : AxisCanvas
    {
        /// <summary>
        /// Arranges all children in the correct position.
        /// </summary>
        /// <param name="arrangeSize">The size to arrange element's within.
        /// </param>
        /// <returns>The size that element's were arranged in.</returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var placedLabels = new List<Rect>();

            // perform culling tickLabels 
            foreach (var label in Children.OfType<TickLabel>().OrderByDescending(label => label.CullingPriority))
            {
                var arrangedRect = GetArrangedRect(arrangeSize, label);

                // check if current label intersects with one of already placed labels
                var intersects = false;
                placedLabels.ForEachDo(rect => intersects |= arrangedRect.IntersectsWith(rect));

                if (intersects)
                {
                    label.Visibility = Visibility.Collapsed;
                }
                else
                {
                    placedLabels.Add(arrangedRect);
                    label.Arrange(arrangedRect);
                }
            }

            return arrangeSize;
        }
    }
}
