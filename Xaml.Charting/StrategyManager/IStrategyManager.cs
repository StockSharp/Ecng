using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Messaging;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting.StrategyManager
{
    /// <summary>
    /// Provides access to different strategies for polar/cartesian chart
    /// </summary>
    public interface IStrategyManager
    {
        /// <summary>
        /// Gets current transformation strategy
        /// </summary>
        /// <returns></returns>
        ITransformationStrategy GetTransformationStrategy();
    }

    internal class DefaultStrategyManager : IStrategyManager
    {
        private CoordinateSystem _chartCoordinateSystem;
        private ITransformationStrategy _transformationStrategy;

        public DefaultStrategyManager(UltrachartSurface surface)
        {
            _chartCoordinateSystem = surface.IsPolarChart ? CoordinateSystem.Polar : CoordinateSystem.Cartesian;

            UpdateStrategies(new Size(), _chartCoordinateSystem);

            var eventAgg = surface.Services.GetService<IEventAggregator>();

            eventAgg.Subscribe<RenderSurfaceResizedMessage>(OnRenderSurfaceResized, true);
            eventAgg.Subscribe<CoordinateSystemMessage>(OnCoordinateSystemChanged, true);
        }

        private void UpdateStrategies(Size viewportSize, CoordinateSystem coordinateSystem)
        {
            switch (coordinateSystem)
            {
                case CoordinateSystem.Polar:
                    _transformationStrategy = new PolarTransformationStrategy(viewportSize);
                    break;
                case CoordinateSystem.Cartesian:
                    _transformationStrategy = new CartesianTransformationStrategy(viewportSize);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Cannot update strategies for surface with CoordinateSystem.{0}", coordinateSystem));
            }
        }

        private void OnRenderSurfaceResized(RenderSurfaceResizedMessage message)
        {
            UpdateStrategies(message.ViewportSize, _chartCoordinateSystem);
        }

        private void OnCoordinateSystemChanged(CoordinateSystemMessage message)
        {
            _chartCoordinateSystem = message.CoordinateSystem;
            
            UpdateStrategies(_transformationStrategy.ViewportSize, _chartCoordinateSystem);
        }

        public ITransformationStrategy GetTransformationStrategy()
        {
            return _transformationStrategy;
        }
    }
}
