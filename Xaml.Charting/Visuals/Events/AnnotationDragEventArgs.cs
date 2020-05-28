using System;

namespace Ecng.Xaml.Charting
{
    public class AnnotationDragEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationDragEventArgs" /> class.
        /// </summary>
        public AnnotationDragEventArgs(bool isPrimary, bool isResize)
        {
            IsPrimary = isPrimary;
            IsResize = isResize;
        }

        /// <summary>
        /// whether this is related to annotation user is dragging with his mouse.
        /// otherwise, this event is for annotation in multiselect group.
        /// </summary>
        public bool IsPrimary { get; }

        /// <summary>
        /// whether this is adorner resize operation.
        /// </summary>
        public bool IsResize { get; }
    }
}
