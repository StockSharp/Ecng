// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderPassInfo.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Windows;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.StrategyManager;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines data for the current render pass
    /// </summary>
    public struct RenderPassInfo
    {
        /// <summary>
        /// Gets or sets the current Viewport Size
        /// </summary>
        public Size ViewportSize;

        /// <summary>
        /// Gets or sets an array of RenderableSeries to draw
        /// </summary>
        public IRenderableSeries[] RenderableSeries;

        /// <summary>
        /// Gets or sets an array of <see cref="IPointSeries"/> which provide data
        /// </summary>
        public IPointSeries[] PointSeries;

        /// <summary>
        /// Gets or sets an array of <see cref="IDataSeries"/> which source data
        /// </summary>
        public IDataSeries[] DataSeries;

        /// <summary>
        /// Gets or sets an array of <see cref="IntegerRange"/> which provide indices to the source data-series in view
        /// </summary>
        public IndexRange[] IndicesRanges;

        /// <summary>
        /// Gets or sets a keyed dictionary of XAxis CoordinateCalculators
        /// </summary>
        public IDictionary<string, ICoordinateCalculator<double>> XCoordinateCalculators;

        /// <summary>
        /// Gets or sets a keyed dictionary of YAxis CoordinateCalculators
        /// </summary>
        public IDictionary<string, ICoordinateCalculator<double>> YCoordinateCalculators;

        /// <summary>
        /// Gets or sets the current pixel transformation strategy
        /// </summary>
        public ITransformationStrategy TransformationStrategy;

        public List<string> Warnings { get; set; } 
    }
}