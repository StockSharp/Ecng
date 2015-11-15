// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PointResampler_Old.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System;
using System.Collections;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Numerics
{
    internal class PointResampler_Old : IPointResampler_Old
    {
        private readonly int _resolution;
        private ResamplingMode _resamplingMode;

        /// <summary>
        /// Creates a new PointResampler instance
        /// </summary>
        /// <param name="resolution">The resolution constant, must be 2 or more. The higher resolution means larger datasets after resampling (which results in cleaner but slower rendering)</param>
        /// <param name="resamplingMode"></param>
        internal PointResampler_Old(int resolution, ResamplingMode resamplingMode)
        {
            Guard.Assert(resolution, "resolution").IsGreaterThanOrEqualTo(2);

            _resolution = resolution;
            _resamplingMode = resamplingMode;
        }

        public int Resolution { get { return _resolution; } }
        public ResamplingMode ResamplingMode { get { return _resamplingMode; } }

        public bool RequiresReduction(IndexRange pointIndices, int viewportWidth)
        {
            int setLength = pointIndices.Max - pointIndices.Min + 1;
            int resampledLength = _resolution * viewportWidth;

            return _resamplingMode != ResamplingMode.None & setLength > resampledLength;
        }

        public IPointResampler_Old WithMode(ResamplingMode newMode)
        {
            _resamplingMode = newMode;
            return this;
        }

        public IList ReducePoints(IList inputPoints, int viewportWidth)
        {
            return ReducePoints(inputPoints, new IndexRange(0, inputPoints.Count - 1), viewportWidth);
        }

        public IList ReducePoints(IList inputPoints, IndexRange pointIndices, int viewportWidth)
        {            
            if (!RequiresReduction(pointIndices, viewportWidth))
            {
                int setLength = pointIndices.Max - pointIndices.Min + 1;
                if (setLength == inputPoints.Count)
                {
                    return inputPoints;
                }
                var result = new double[setLength];
                for (int i = pointIndices.Min, j = 0; j < setLength; i++, j++)
                {
                    result[j] = Convert.ToDouble(inputPoints[i]);
                }
                return result;
            }

            if (_resamplingMode.ToString().StartsWith(ResamplingMode.MinMax.ToString()))
                return ResampledMinMax(inputPoints, pointIndices.Min, pointIndices.Max, viewportWidth);

            if (_resamplingMode == ResamplingMode.Min)
                return ResampledMin(inputPoints, pointIndices.Min, pointIndices.Max, viewportWidth);

            if (_resamplingMode == ResamplingMode.Max)
                return ResampledMax(inputPoints, pointIndices.Min, pointIndices.Max, viewportWidth);

            if (_resamplingMode == ResamplingMode.Mid)
                return ResampledMid(inputPoints, pointIndices.Min, pointIndices.Max, viewportWidth);

            throw new Exception(string.Format("Resampling Mode {0} has not been handled", _resamplingMode));   
        }        

        private IList ResampledMinMax(IList inputPoints, int startIndex, int endIndex, int viewportWidth)
        {
            int setLength = endIndex - startIndex + 1;
            int minResampledLength = _resolution * viewportWidth;
            int bucketSize = setLength/minResampledLength;
            int actualResampledLength = 2*setLength/bucketSize;

            double[] reducedPoints = new double[actualResampledLength];

            double min = double.MaxValue;
            double max = double.MinValue;

            int i = startIndex, counter = 0, rIndex = 0;
            int stopIndex = endIndex - setLength%bucketSize;
            for (; i <= stopIndex; i++)
            {
                double current = ((IComparable)inputPoints[i]).ToDouble();
                min = Math.Min(min, current);
                max = Math.Max(max, current);

                if (++counter >= bucketSize)
                {
                    reducedPoints[rIndex++] = min;
                    reducedPoints[rIndex++] = max;

                    min = double.MaxValue;
                    max = double.MinValue;
                    counter = 0;
                }
            }

            if (endIndex != stopIndex && reducedPoints.Length > rIndex)
            {
                for (; i <= endIndex; i++)
                {
                    min = Math.Min(min, (double) inputPoints[i]);
                    max = Math.Max(max, (double) inputPoints[i]);
                }

                reducedPoints[rIndex] = min;
                reducedPoints[rIndex] = max;
            }

            return reducedPoints;
        }

        private IList ResampledMax(IList inputPoints, int startIndex, int endIndex, int viewportWidth)
        {
            int setLength = endIndex - startIndex + 1;
            int minResampledLength = _resolution * viewportWidth;
            int bucketSize = setLength / minResampledLength;
            int actualResampledLength = setLength / bucketSize;

            double[] reducedPoints = new double[actualResampledLength];

            double max = double.MinValue;

            for (int i = startIndex, counter = 0, rIndex = 0; i <= endIndex; i++)
            {
                double newMax = ((IComparable)inputPoints[i]).ToDouble();
                if (newMax > max)
                    max = newMax;

                if (++counter >= bucketSize)
                {
                    reducedPoints[rIndex++] = max;

                    max = double.MinValue;
                    counter = 0;
                }
            }

            return reducedPoints;
        }

        private IList ResampledMin(IList inputPoints, int startIndex, int endIndex, int viewportWidth)
        {
            int setLength = endIndex - startIndex + 1;
            int minResampledLength = _resolution * viewportWidth;
            int bucketSize = setLength / minResampledLength;
            int actualResampledLength = setLength / bucketSize;

            double[] reducedPoints = new double[actualResampledLength];

            double min = double.MaxValue;

            for (int i = startIndex, counter = 0, rIndex = 0; i <= endIndex; i++)
            {
                double newMin = ((IComparable)inputPoints[i]).ToDouble();
                if (newMin < min)
                    min = newMin;

                if (++counter >= bucketSize)
                {
                    reducedPoints[rIndex++] = min;

                    min = double.MaxValue;
                    counter = 0;
                }
            }

            return reducedPoints;
        }

        private IList ResampledMid(IList inputPoints, int startIndex, int endIndex, int viewportWidth)
        {
            int setLength = endIndex - startIndex + 1;
            int minResampledLength = _resolution * viewportWidth;
            int bucketSize = setLength / minResampledLength;
            int actualResampledLength = setLength / bucketSize;

            double[] reducedPoints = new double[actualResampledLength];

            double min = double.MaxValue;
            double max = double.MinValue;

            for (int i = startIndex, counter = 0, rIndex = 0; i <= endIndex; i++)
            {
                double current = ((IComparable)inputPoints[i]).ToDouble();
                min = Math.Min(min, current);
                max = Math.Max(max, current);

                if (++counter >= bucketSize)
                {
                    reducedPoints[rIndex++] = 0.5 * (max + min);

                    min = double.MaxValue;
                    max = double.MinValue;
                    counter = 0;
                }
            }

            return reducedPoints;
        }
    }   
}