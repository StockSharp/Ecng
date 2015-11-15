using System.Windows;
using System.Windows.Controls;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines the base interface for annotation canvas
    /// </summary>
    public interface IAnnotationCanvas : IHitTestable
    {
        /// <summary>
        /// Gets children elements of current annotation canvas
        /// </summary>
        UIElementCollection Children { get; }
    }

    /// <summary>
    /// A canvas which is used to place annotations on chart
    /// </summary>
    public class AnnotationSurface : Canvas, IAnnotationCanvas
    {
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            return this.TranslatePoint(point, relativeTo);
        }

        public bool IsPointWithinBounds(Point point)
        {
            return this.IsPointWithinBounds(point);
        }

        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            return this.GetBoundsRelativeTo(relativeTo);
        }
    }
}
