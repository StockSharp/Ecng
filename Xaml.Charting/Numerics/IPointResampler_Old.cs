// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IPointResampler_Old.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Numerics
{
    internal interface IPointResampler_Old
    {
        /// <summary>
        /// Gets the current resolution. This must be greater than equal to 2 and the higher the number, the larger the reduced datasets
        /// </summary>
        int Resolution { get; }

        /// <summary>
        /// Gets the current Resampling Mode.
        /// </summary>
        ResamplingMode ResamplingMode { get; }

        /// <summary>
        /// Returns true if a dataset with the specified index range requires point reduction on the given viewport size
        /// </summary>
        /// <param name="pointIndices"></param>
        /// <param name="viewportWidth"></param>
        /// <returns></returns>
        bool RequiresReduction(IndexRange pointIndices, int viewportWidth);

        /// <summary>
        /// Sets a new ResamplingMode on the IPointsResampler
        /// </summary>
        /// <param name="newMode"></param>
        /// <returns></returns>
        IPointResampler_Old WithMode(ResamplingMode newMode);

        /// <summary>
        /// Reduces the input points using the current ResamplingMode and Resolution
        /// </summary>
        /// <param name="inputPoints"></param>
        /// <param name="viewportWidth"></param>
        /// <returns></returns>
        IList ReducePoints(IList inputPoints, int viewportWidth);

        /// <summary>
        /// Reduces the input points using the current ResamplingMode and Resolution
        /// </summary>
        /// <param name="inputPoints"></param>
        /// <param name="pointIndices"></param>
        /// <param name="viewportWidth"></param>
        /// <returns></returns>
        IList ReducePoints(IList inputPoints, IndexRange pointIndices, int viewportWidth);        
    }

    /// <summary>
    /// Defines the ResamplingMode used by a <see cref="BaseRenderableSeries"/>
    /// </summary>
    /// <remarks></remarks>
    public enum ResamplingMode
    {
        /// <summary>
        /// Do not use resampling when redrawing a series
        /// </summary>
        None,

        /// <summary>
        /// Assumes Evenly-spaced data (TimeSeries). Resample by taking the min-max of oversampled data. This results in the most visually accurate resampling, with the most performant rendering
        /// </summary>
        MinMax, 

        /// <summary>
        /// Assumes Evenly-spaced data (TimeSeries). Resample by taking the median point of oversampled data
        /// </summary>
        Mid,

        /// <summary>
        /// Assumes Evenly-spaced data (TimeSeries). Resample by taking the maximum point of oversampled data
        /// </summary>
        Max,

        /// <summary>
        /// Assumes Evenly-spaced data (TimeSeries). Resample by taking the minimum point of oversampled data
        /// </summary>
        Min, 

        /// <summary>
        /// Assumes Evenly-spaced data (TimeSeries). Resample by taking the minimum data-set to accurately represent the original points without incurring aliasing or other artifacts
        /// </summary>
        Nyquist,

		/// <summary>
		/// Groups close points in 2D space
		/// </summary>
		Cluster2D,

        /// <summary>
        /// Does not assume Evenly-spaced data (TimeSeries). Resample by taking the min-max of oversampled data. This results in the most visually accurate resampling, with the most performant rendering
        /// </summary>
        MinMaxWithUnevenSpacing, 
        
        /// <summary>
        /// Auto-detect the most suitable resampling algorithm (Fastest, plus most accurate) for the type of data appended
        /// </summary>
        Auto,
    }
}