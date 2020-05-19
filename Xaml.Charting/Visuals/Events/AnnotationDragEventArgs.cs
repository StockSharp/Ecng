using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecng.Xaml.Charting
{
    public class AnnotationDragEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationDragEventArgs" /> class.
        /// </summary>
        public AnnotationDragEventArgs(bool isPrimary)
        {
            IsPrimary = isPrimary;
        }

        /// <summary>
        /// whether this is related to annotation user is dragging with his mouse.
        /// otherwise, this event is for annotation in multiselect group.
        /// </summary>
        public bool IsPrimary { get; set; }
    }
}
