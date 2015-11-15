using Ecng.Xaml.Charting.Visuals.PointMarkers;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Creates <see cref="IPathDrawingContext"/> instances, e.g. to draw lines, points, polygon outlines depending on the final implementation. 
    /// 
    /// The <seealso cref="IRenderContext2D"/> which has method BeginLine to draw lines, but other types such as <see cref="IPointMarker"/> implement <see cref="IPathContextFactory"/>
    /// implement it to draw point-markers
    /// </summary>
    /// <seealso cref="IRenderContext2D"/>
    public interface IPathContextFactory
    {
        /// <summary>
        /// Begins drawing at the specified X,Y pixel coordinate, with the specified color
        /// </summary>
        IPathDrawingContext Begin(IPathColor color, double startX, double startY);	 
    }
}