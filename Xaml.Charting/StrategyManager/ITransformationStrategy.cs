using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting.Common.Messaging;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.StrategyManager
{
    /// <summary>
    /// Defines interface for pixel transformation strategy
    /// </summary>
    public interface ITransformationStrategy
    {
        /// <summary>
        /// Peform transform of point according to current strategy
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        Point Transform(Point point);
        /// <summary>
        /// Perform reverse transform of point according to current strategy
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        Point ReverseTransform(Point point);
        /// <summary>
        /// Current viewport size
        /// </summary>
        Size ViewportSize { get; }
    }

    internal abstract class TransformationStrategyBase:ITransformationStrategy
    {
        public Size ViewportSize { get; private set; }

        protected TransformationStrategyBase(Size viewportSize)
        {
            ViewportSize = viewportSize;
        }

        public abstract Point Transform(Point point);

        public abstract Point ReverseTransform(Point point);
    }

    internal class CartesianTransformationStrategy : TransformationStrategyBase
    {
        public CartesianTransformationStrategy(Size viewportSize): base(viewportSize)
        {
        }

        public override Point Transform(Point point)
        {
            return point;
        }

        public override Point ReverseTransform(Point point)
        {
            return point;
        }
    }

    internal class PolarTransformationStrategy : TransformationStrategyBase
    {
        private readonly PolarCartesianTransformationHelper _helper;

        public PolarTransformationStrategy(Size viewportSize): base(viewportSize)
        {
            _helper = new PolarCartesianTransformationHelper(viewportSize.Width, viewportSize.Height);
        }

        public override Point Transform(Point point)
        {
            return _helper.ToPolar(point.X, point.Y);
        }

        public override Point ReverseTransform(Point point)
        {
            return _helper.ToCartesian(point.X, point.Y);
        }
    }
}