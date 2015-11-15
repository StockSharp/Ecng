using System;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting.StrategyManager
{
    /// <summary>
    /// Defines the interface for methods which allows to place annotation
    /// </summary>
    public interface IAnnotationPlacementStrategy
    {
        /// <summary>
        /// Places annotation with specific annotation coordinates
        /// </summary>
        /// <param name="coordinates"></param>
        void PlaceAnnotation(AnnotationCoordinates coordinates);

        /// <summary>
        /// Gets base points for annotation instance
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        Point[] GetBasePoints(AnnotationCoordinates coordinates);

        /// <summary>
        /// Sets base point for annotation
        /// </summary>
        /// <param name="newPoint"></param>
        /// <param name="index"></param>
        void SetBasePoint(Point newPoint, int index);
       
        /// <summary>
        /// Checks whether coordinates are within canvas bounds
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="annotationCanvas"></param>
        /// <returns></returns>
        bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas annotationCanvas);

        /// <summary>
        ///  Moves the annotation to a specific horizontal and vertical offset
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="horizontalOffset"></param>
        /// <param name="verticalOffset"></param>
        /// <param name="annotationCanvas"></param>
        void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizontalOffset, double verticalOffset, IAnnotationCanvas annotationCanvas);
    }

    internal abstract class AnnotationPlacementStrategyBase<T> : IAnnotationPlacementStrategy where T : AnnotationBase
    {
        private readonly T _annotation;

        protected AnnotationPlacementStrategyBase(T annotation)
        {
            _annotation = annotation;
        }

        public T Annotation
        {
            get { return _annotation; }
        }

        public abstract void PlaceAnnotation(AnnotationCoordinates coordinates);

        public abstract Point[] GetBasePoints(AnnotationCoordinates coordinates);

        public abstract void SetBasePoint(Point newPoint, int index);

        public abstract bool IsInBounds(AnnotationCoordinates coordinates, IAnnotationCanvas annotationCanvas);

        public abstract void MoveAnnotationTo(AnnotationCoordinates coordinates, double horizontalOffset, double verticalOffset, IAnnotationCanvas annotationCanvas);
    }
}
