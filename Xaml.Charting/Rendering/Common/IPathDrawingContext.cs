using System;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Returns an <see cref="IPathDrawingContext"/> to draw a polyline, or collection of PointMarkers 
    /// </summary>
    /// <example>
    /// <code>
    /// var lineContext = renderContext.BeginLine(pen, 0, 0);
    /// lineContext.LineTo(1, 2);
    /// lineContext.LineTo(3, 4);
    /// lineContext.Dispose();
    /// </code>
    /// </example>
    public interface IPathDrawingContext : IDisposable
    {
        /// <summary>
        /// Starts the context at the specified X,Y coordinate with a specified Pen
        /// </summary>
        /// <param name="color">The pen or brush for the drawing operation</param>
        /// <param name="x">The x-coordinate in pixels</param>
        /// <param name="y">The y-coordinate in pixels</param>
        /// <returns>The <see cref="IPathDrawingContext"/> instance, to allow fluent API</returns>
        IPathDrawingContext Begin(IPathColor color, double x, double y);

        /// <summary>
        /// Moves the Context to the specified X,Y coordinate. 
        /// </summary>
        /// <param name="x">The x-coordinate in pixels</param>
        /// <param name="y">The y-coordinate in pixels</param>
        /// <returns>The <see cref="IPathDrawingContext"/> instance, to allow fluent API</returns>
        IPathDrawingContext MoveTo(double x, double y);

        /// <summary>
        /// Ends the segment, flushing to render target
        /// </summary>
        void End();
    }
}