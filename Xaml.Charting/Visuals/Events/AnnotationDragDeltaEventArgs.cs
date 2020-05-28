using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Event Args used by the <see cref="AnnotationBase.DragDelta"/> event
    /// </summary>
    public class AnnotationDragDeltaEventArgs : AnnotationDragEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationDragDeltaEventArgs" /> class.
        /// </summary>
        public AnnotationDragDeltaEventArgs(bool isPrimary, bool isResize, double horizontalOffset, double verticalOffset) : base(isPrimary, isResize)
        {
            HorizontalOffset = horizontalOffset;
            VerticalOffset = verticalOffset;
        }

        /// <summary>
        /// Gets / Sets HorizontalOffset property
        /// </summary>
        public double HorizontalOffset { get; }
        /// <summary>
        /// Gets / Sets VerticalOffset property
        /// </summary>
        public double VerticalOffset { get; }
    }
}