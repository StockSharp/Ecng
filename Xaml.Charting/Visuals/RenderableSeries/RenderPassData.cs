// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderPassData.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************

using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.StrategyManager;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines the interface to <see cref="RenderPassData"/>, the data used in a single render pass by <see cref="BaseRenderableSeries"/> derived types
    /// </summary>
    public interface IRenderPassData
    {
        /// <summary>
        /// Gets the integer indices of the X-Data array that are currently in range.
        /// </summary>
        /// <returns>The indices to the X-Data that are currently in range</returns>
        /// <example>If the input X-data is 0...100 in steps of 1, the VisibleRange is 10, 30 then the PointRange will be 10, 30</example>
        /// <remarks></remarks>
        IndexRange PointRange { get; }
        /// <summary>
        /// Gets the current point series.
        /// </summary>
        IPointSeries PointSeries { get; }
        /// <summary>
        /// Gets a value, indicating whether current chart is vertical
        /// </summary>
        bool IsVerticalChart { get; }
        /// <summary>
        /// Gets the current Y coordinate calculator.
        /// </summary>
        ICoordinateCalculator<double> YCoordinateCalculator { get; }
        /// <summary>
        /// Gets the current X coordinate calculator.
        /// </summary>
        ICoordinateCalculator<double> XCoordinateCalculator { get; }

        /// <summary>
        /// Gets the current pixel transformation strategy
        /// </summary>
        ITransformationStrategy TransformationStrategy { get; }

    }

    /// <summary>
    /// Provides data used in a single render pass by <see cref="BaseRenderableSeries"/> derived types
    /// </summary>
    public class RenderPassData : IRenderPassData
    {
        private readonly IndexRange _pointRange;
        private readonly ICoordinateCalculator<double> _xCoordinateCalculator;
        private readonly ICoordinateCalculator<double> _yCoordinateCalculator;
        private readonly IPointSeries _pointSeries;
        private readonly ITransformationStrategy _transformationStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderPassData"/> class.
        /// </summary>
        /// <param name="pointRange">The integer indices to the X-data array that are currently in range.</param>
        /// <param name="xCoordinateCalculator">The current X Coordinate Calculator</param>
        /// <param name="yCoordinateCalculator">The current Y Coordinate Calculator</param>
        /// <param name="pointSeries">The resampled PointSeries to draw</param>
        /// <remarks></remarks>
        public RenderPassData(
            IndexRange pointRange, 
            ICoordinateCalculator<double> xCoordinateCalculator, 
            ICoordinateCalculator<double> yCoordinateCalculator,
            IPointSeries pointSeries,
            ITransformationStrategy transformationStrategy)
        {
            _pointRange = pointRange;
            _xCoordinateCalculator = xCoordinateCalculator;
            _yCoordinateCalculator = yCoordinateCalculator;
            _pointSeries = pointSeries;
            _transformationStrategy = transformationStrategy;
        }


        /// <summary>
        /// Gets a value, indicating whether current chart is vertical
        /// </summary>
        public bool IsVerticalChart { get { return !XCoordinateCalculator.IsHorizontalAxisCalculator; } }

        /// <summary>
        /// Gets the current Y coordinate calculator.
        /// </summary>
        public ICoordinateCalculator<double> YCoordinateCalculator
        {
            get { return _yCoordinateCalculator; }
        }

        /// <summary>
        /// Gets the current X coordinate calculator.
        /// </summary>
        public ICoordinateCalculator<double> XCoordinateCalculator
        {
            get { return _xCoordinateCalculator; }
        }

        /// <summary>
        /// Gets the current point series.
        /// </summary>
        public IPointSeries PointSeries
        {
            get { return _pointSeries; }
        }

        /// <summary>
        /// Gets the integer indices of the X-Data array that are currently in range.
        /// </summary>
        /// <returns>The indices to the X-Data that are currently in range</returns>
        ///   
        /// <example>If the input X-data is 0...100 in steps of 1, the VisibleRange is 10, 30 then the PointRange will be 10, 30</example>
        /// <remarks></remarks>
        public IndexRange PointRange
        {
            get { return _pointRange; }
        }

        /// <summary>
        /// Gets the current pixel transformation strategy
        /// </summary>
        public ITransformationStrategy TransformationStrategy
        {
            get { return _transformationStrategy; }
        }
    }    
}