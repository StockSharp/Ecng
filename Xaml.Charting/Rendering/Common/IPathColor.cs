using System;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// A base interface for <see cref="IPen2D"/> and <see cref="IBrush2D"/>. Used by the <see cref="IPathDrawingContext"/> to draw fills and lines
    /// </summary>
    public interface IPathColor : IDisposable
    {
        /// <summary>
        /// Gets the color of the pen. Supports transparency
        /// </summary>
        Color Color { get; }

        /// <summary>
        /// Used internally by the renderer, gets the integer color-code that represents the Pen color
        /// </summary>
        int ColorCode { get; }

        /// <summary>
        /// Gets a value indicating whether this pen is transparent.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is transparent; otherwise, <c>false</c>.
        /// </value>
        bool IsTransparent { get; }
    }
}