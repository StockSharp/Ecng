// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PointResamplerBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.GenericMath;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.PointResamplers;

namespace Ecng.Xaml.Charting.Numerics
{
    /// <summary>
    /// Provides the interface to a Pointresampler - an algorithm which is able to reduce the number of data-points in a series for rendering fast, while maintaining visual accuracy of the series 
    /// </summary>
    public interface IPointResampler
    {
        /// <summary>
        /// Transforms the input X and Y series into an <see cref="IPointSeries"/>, a resampled, reduced dataset for rendering on screen
        /// </summary>
        /// <param name="resamplingMode">The <see cref="ResamplingMode"/> to use</param>
        /// <param name="pointRange">The indices of the X and Y input data to use (clips by indices)</param>
        /// <param name="viewportWidth">The current width of the Viewport</param>
        /// <param name="isFifo">If the data is FIFO (Circular buffered) data</param>
        /// <param name="isCategoryAxis">If the XAxis is a category axis</param>
        /// <param name="xColumn">The input X-Value Series</param>
        /// <param name="yColumn">The input Y-Value Series</param>
        /// <param name="dataIsSorted">If the data is sorted in the X-Direction</param>
        /// <param name="dataIsEvenlySpaced">If the data is Evenly Spaced in X</param>
        /// <param name="dataIsDisplayedAs2d">If the data is presented as a scatter, e.g. not line</param>
        /// <param name="visibleXRange">The VisibleRange of the XAxis at the time of resampling</param>
        /// <returns>The transformed dataset for rendering</returns>
        IPointSeries Execute(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isFifo, bool isCategoryAxis, IList xColumn, IList yColumn, bool? dataIsSorted, bool? dataIsEvenlySpaced, bool? dataIsDisplayedAs2d, IRange visibleXRange);
    }

    internal abstract class PointResamplerBase : IPointResampler
    {
        internal static bool RequiresReduction(ResamplingMode resamplingMode, IndexRange pointIndices, int viewportWidth)
        {
            int setLength = pointIndices.Max - pointIndices.Min + 1;
            int resampledLength = 4 * viewportWidth;

            return resamplingMode != ResamplingMode.None & setLength > resampledLength;
        }

        public abstract IPointSeries Execute(ResamplingMode resamplingMode, IndexRange pointRange, int viewportWidth, bool isFifo, bool isCategoryAxis, IList xColumn, IList yColumn, bool? dataIsSorted, bool? dataIsEvenlySpaced, bool? dataIsDisplayedAs2d, IRange visibleXRange);

        static void GetXRange<TX>(IList<TX> xColumn, IndexRange pointRange, IRange visibleXRange, out double minXInclusive, out double maxXInclusive)
            where TX : IComparable
        {
            minXInclusive = xColumn[pointRange.Min].ToDouble();
            maxXInclusive = xColumn[pointRange.Max].ToDouble();

            if (visibleXRange != null && !(visibleXRange is IndexRange))
            {
                minXInclusive = visibleXRange.Min.ToDouble();
                maxXInclusive = visibleXRange.Max.ToDouble();
            }
        }
        
        static bool GetMinMaxValuesForPixel<TX,TY>(
            IMath<TX> xMath, IMath<TY> yMath, UncheckedList<TX> xValues, UncheckedList<TY> yValues, int pointerToNextElement // could be outside of this pixel (in case of gap)
                            , int maxRemainingNumberOfPointsInPixel, double pixelEndXInclusive,
                             out double minYInPixel, out double maxYInPixel, out int numberOfPointsInPixel
                             )
            where TX : IComparable
            where TY : IComparable
        {
            var currY = yMath.ToDouble(yValues[pointerToNextElement]);
            var currX = xMath.ToDouble(xValues[pointerToNextElement]);
            numberOfPointsInPixel = 0;
            minYInPixel = currY;
            maxYInPixel = currY;

            bool isNanPixel = false;
            // enumerate elements until they go out of span
            while (currX <= pixelEndXInclusive)
            { // this point is inside span
                if (numberOfPointsInPixel == 0)
                {
                    isNanPixel = double.IsNaN(currY);
                }
                else if (isNanPixel != double.IsNaN(currY))
                {
                    return false;
                } 
                
                if (!(currY > minYInPixel)) minYInPixel = currY;
                if (!(currY < maxYInPixel)) maxYInPixel = currY;
                numberOfPointsInPixel++;

                if (numberOfPointsInPixel < maxRemainingNumberOfPointsInPixel)
                {
                    pointerToNextElement++;
                    currY = yMath.ToDouble(yValues[pointerToNextElement]);
                    currX = xMath.ToDouble(xValues[pointerToNextElement]);
                }
                else
                {
                    break;
                }
            }

            return true;
        }


#if !SILVERLIGHT
        unsafe static bool GetMinMaxValuesForPixel(
            double* xValues, 
            double* yValues, 
            int maxRemainingNumberOfPointsInPixel, double pixelEndXInclusive,
            out double minYInPixel, out double maxYInPixel, out int numberOfPointsInPixel
            )
        {
            var currY = *yValues;
            var currX = *xValues;
            numberOfPointsInPixel = 0;
            minYInPixel = currY;
            maxYInPixel = currY;

            bool isNanPixel = false;
            // enumerate elements until they go out of span
            while (currX <= pixelEndXInclusive)
            { // this point is inside span
                if (numberOfPointsInPixel == 0)
                {
                    isNanPixel = double.IsNaN( currY );
                }
                else if (isNanPixel != double.IsNaN(currY))
                {
                    return false;
                }

                if (!(currY > minYInPixel)) minYInPixel = currY;
                if (!(currY < maxYInPixel)) maxYInPixel = currY;
                numberOfPointsInPixel++;

                if (numberOfPointsInPixel < maxRemainingNumberOfPointsInPixel)
                {
                    xValues++;
                    yValues++;
                    currY = *yValues;
                    currX = *xValues;
                }
                else
                {
                    break;
                }
            }

            return true;
        }
#endif

        protected static IPointSeries ReducePointsMinMaxUnevenlySpaced<TX, TY>(IList<TX> xColumn, IList<TY> yColumn, IndexRange pointRange, int viewportWidth, bool isCategoryAxis, IRange visibleXRange)
            where TX : IComparable
            where TY : IComparable
        {
            /*
             algorithm of uneven resampling
            
            given min and max X value - bounds of screen, number of pixels
            for each pixel {
            calculate X bounds of pixel
            call subroutine (ASM) which goes through X and Y values for this pixel, and calculates min and max Y. it exists when X value goes out of pixel
            the subroutine also returns number of data elements in pixel
            after the subroutine, add points to result list as following:
            if there are points in current pixel
            {
            if last pixel was empty
            {
                add first point from current pixel  (it is first point after a gap)
            }
            add max and min point from current pixel
                (there could be 3 resampled points per pixel)
            }
            if there are no points and previous pixel was not empty
            {
                add last point from previous pixel
                (it is a point which goes before a gap)
            }

            
             * ASM subroutine has to be specific for each pair of TX and TY and 32/64bit mode
             
             */

            double minXInclusive, maxXInclusive;
            GetXRange(xColumn, pointRange, visibleXRange, out minXInclusive, out maxXInclusive);
            
            int endIndexInclusive = pointRange.Max;
            int startIndexInclusive = pointRange.Min;
            double pixelX = (maxXInclusive - minXInclusive) / viewportWidth;
            var reducedPoints = new Point2DSeries(viewportWidth * 2 + 1); // 2 points for min and max + 1 for nect point

            // ToUncheckedList with a true parameter will make a copy if an unchecked list cannot be retrieved, which 
            // will consume additional memory. You might want to consider not making a copy and use 
            // ReducePointUnevenImpl implementation that accepts a IList instead
            // of an UncheckedList (especially if a custom made C++/ASM implementation doesn't exist).
            var yValues = yColumn.ToUncheckedList( true );
            var xValues = xColumn.ToUncheckedList( true );

            bool isReduced = false;
#if !SILVERLIGHT
            if (xValues != null && yValues != null)
            {
                {
                    var xDoubleValues = xValues as UncheckedList<double>;
                    var yDoubleValues = yValues as UncheckedList<double>;
                    if (xDoubleValues != null && yDoubleValues != null)
                    {
                        ReducePointUnevenImpl(xDoubleValues, yDoubleValues, reducedPoints,
                            startIndexInclusive, endIndexInclusive,
                            minXInclusive, maxXInclusive,
                            viewportWidth, isCategoryAxis);
                        isReduced = true;
                    }
                }

                if (!isReduced)
                {
                    var xDoubleValues = xValues as UncheckedList<double>;
                    var yFloatValues = yValues as UncheckedList<float>;
                    if (xDoubleValues != null && yFloatValues != null)
                    {
                        // TODO: Implement
                        ReducePointUnevenImpl(xDoubleValues, yFloatValues, reducedPoints,
                            startIndexInclusive, endIndexInclusive,
                            minXInclusive, maxXInclusive,
                            viewportWidth, isCategoryAxis);
                        isReduced = true;
                    }
                }

                // TODO: Check other pairs
            }
#endif

            if( !isReduced )
            {
                ReducePointUnevenImpl(
                    xValues, yValues, 
                    reducedPoints,
                    startIndexInclusive, endIndexInclusive,
                    minXInclusive, maxXInclusive,
                    viewportWidth, isCategoryAxis);
            }

            reducedPoints.Freeze();
            return reducedPoints;
        }

#if !SILVERLIGHT
        /// <summary>
        /// This needs to be implemented for each supported pair of TX,TY.
        /// Maybe use T4 templates?
        /// For optimal performance, this method should be implemented in C++/ASM and not just the 
        /// GetMinMaxValuesForPixel method (this avoid per-pixel unmanaged call transitions). 
        /// However, that would also require that reducedPoints is passed as pre-allocated buffer 
        /// and not as a Point2DSeries. NaN handling makes it a bit difficult to 
        /// pre-allocate though (since NaNs can cause multiple lines per pixel).  
        /// </summary>
        private static unsafe void ReducePointUnevenImpl(
            UncheckedList<double> xValues,
            UncheckedList<double> yValues,
            Point2DSeries reducedPoints, 
            int startIndexInclusive, 
            int endIndexInclusive,
            double minXInclusive,
            double maxXInclusive,
            int viewportWidth,
            bool isCategoryAxis )
        {
            fixed (double* unsafeXValues = &xValues.Array[startIndexInclusive])
            {
                fixed (double* unsafeYValues = &yValues.Array[startIndexInclusive])
                {
                    var reducedXValues = reducedPoints.XValues;
                    var reducedYValues = reducedPoints.YValues;
                    
                    double* pNextXValue = unsafeXValues;
                    double* pNextYValue = unsafeYValues;

                    var indexOfNextElement = startIndexInclusive;
                    bool previousPixelWasEmpty = true;

                    double pixelX = (maxXInclusive - minXInclusive) / viewportWidth;

                    // for each pixel
                    int pixelIndex = 0;
                    while( pixelIndex < viewportWidth )
                    {
                        double maxYInPixel, minYInPixel; int numberOfPointsInPixel;
                        if( GetMinMaxValuesForPixel(pNextXValue, pNextYValue, endIndexInclusive - indexOfNextElement + 1,
                            minXInclusive + pixelX * (pixelIndex + 1),
                            out minYInPixel, out maxYInPixel, out numberOfPointsInPixel
                            ) )
                        {
                            pixelIndex++;
                        } 

                        /*
                            general algorithm:		
                            if there are points 
                           {
                                if last pixel was empty
                                {
                                    add first point
                                }
                                add max and min point
                           }
                           if there are no points and previous pixel was not empty
                           {
                                add last point from previous pixel
                           }
                        */

                        double x = isCategoryAxis ? (double)indexOfNextElement : *pNextXValue;
                        if (numberOfPointsInPixel != 0)
                        {
                            if (previousPixelWasEmpty == true)
                            {
                                // add first item from this span
                                reducedXValues.Add(x);
                                reducedYValues.Add(*pNextYValue);
                            }

                            reducedXValues.Add(x);
                            reducedYValues.Add(minYInPixel);
                            reducedXValues.Add(x);
                            reducedYValues.Add(maxYInPixel);
                        }
                        else
                        {
                            if (previousPixelWasEmpty == false)
                            {
                                //add last item from previous span	
                                reducedXValues.Add(*(pNextXValue - 1));
                                reducedYValues.Add(*(pNextYValue - 1));
                            }
                        }

                        indexOfNextElement += numberOfPointsInPixel;
                        pNextXValue += numberOfPointsInPixel;
                        pNextYValue += numberOfPointsInPixel;
                        if (indexOfNextElement > endIndexInclusive) break;
                        previousPixelWasEmpty = numberOfPointsInPixel == 0;
                    }

                    if (indexOfNextElement <= endIndexInclusive)
                    { // add last point
                        double x = isCategoryAxis ? (double)indexOfNextElement : *pNextXValue;
                        reducedXValues.Add(x);
                        reducedYValues.Add(*pNextYValue);
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Generic implementation where an unchecked list has been retrieved for x- and y-values.
        /// </summary>
        private static void ReducePointUnevenImpl<TX, TY>(
            UncheckedList<TX> xValues,
            UncheckedList<TY> yValues,
            Point2DSeries reducedPoints,
            int startIndexInclusive,
            int endIndexInclusive,
            double minXInclusive,
            double maxXInclusive,
            int viewportWidth,
            bool isCategoryAxis)
            where TX : IComparable
            where TY : IComparable
        {
            var xMath = GenericMathFactory.New<TX>();
            var yMath = GenericMathFactory.New<TY>();

            var reducedXValues = reducedPoints.XValues;
            var reducedYValues = reducedPoints.YValues;

            var indexOfNextElement = startIndexInclusive;
            bool previousPixelWasEmpty = true;

            double pixelX = (maxXInclusive - minXInclusive) / viewportWidth;

            // for each pixel
            int pixelIndex = 0;
            while (pixelIndex < viewportWidth)                
            {
                double maxYInPixel, minYInPixel; 
                int numberOfPointsInPixel;
                if (GetMinMaxValuesForPixel(
                    xMath, yMath,
                    xValues, yValues,
                    indexOfNextElement,
                    endIndexInclusive - indexOfNextElement + 1,
                    minXInclusive + pixelX * (pixelIndex + 1),
                    out minYInPixel, out maxYInPixel, out numberOfPointsInPixel
                    ))
                {
                    pixelIndex++;
                }
                /*
                    general algorithm:		
                    if there are points 
                    {
                        if last pixel was empty
                        {
                            add first point
                        }
                        add max and min point
                    }
                    if there are no points and previous pixel was not empty
                    {
                        add last point from previous pixel
                    }
                */

                double x = isCategoryAxis ? (double)indexOfNextElement : xMath.ToDouble( xValues[indexOfNextElement] );
                if (numberOfPointsInPixel != 0)
                {

                    if (previousPixelWasEmpty == true)
                    {
                        // add first item from this span
                        reducedXValues.Add(x);
                        reducedYValues.Add(yMath.ToDouble(yValues[indexOfNextElement]));
                    }

                    reducedXValues.Add(x);
                    reducedYValues.Add(minYInPixel);
                    reducedXValues.Add(x);
                    reducedYValues.Add(maxYInPixel);
                }
                else
                {
                    if (previousPixelWasEmpty == false)
                    {
                        //add last item from previous span	
                        reducedXValues.Add(xMath.ToDouble(xValues[indexOfNextElement-1]));
                        reducedYValues.Add(yMath.ToDouble(yValues[indexOfNextElement-1]));
                    }
                }

                indexOfNextElement += numberOfPointsInPixel;
                if (indexOfNextElement > endIndexInclusive) break;
                previousPixelWasEmpty = numberOfPointsInPixel == 0;
            }

            if (indexOfNextElement <= endIndexInclusive)
            { // add last point
                double x = isCategoryAxis ? (double)indexOfNextElement : xMath.ToDouble(xValues[indexOfNextElement]);
                reducedXValues.Add(x);
                reducedYValues.Add(yMath.ToDouble(yValues[indexOfNextElement]));
            }
        }


				private const int _binsWidth = 400;
	    private const int _binsHeight = 300;
	    private static byte[] _bins;
		[DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
		private static extern void memset(
			IntPtr dst,
			int filler,
			int count);
		protected static IPointSeries ResampleInClusterMode<TX, TY>(IList<TX> xColumn, IList<TY> yColumn,
																			   IndexRange pointRange, int viewportWidth,
																			   bool isCategoryAxis)
			where TX : IComparable
			where TY : IComparable
		{
			unchecked
			{
				var xMath = GenericMathFactory.New<TX>();
				var yMath = GenericMathFactory.New<TY>();

				// allocate, reset memory of bool[,] bins
				if (_bins == null) _bins = new byte[_binsWidth*_binsHeight];
				else
				{
					var pinnedArray = GCHandle.Alloc(_bins, GCHandleType.Pinned);
					var pointer = pinnedArray.AddrOfPinnedObject();
					memset(pointer, 0, _bins.Length);
					pinnedArray.Free();
				}
				

				// find min, max X and Y in input data
				TX xMin, xMax;
				ArrayOperations.MinMax(xColumn, out xMin, out xMax);
				var xMinMaxNorm = (!xMax.Equals(xMin)) ? (_binsWidth - 1) / xMath.Subtract(xMax, xMin).ToDouble() : 0;
				TY yMin, yMax;
				ArrayOperations.MinMax(yColumn, out yMin, out yMax);
				var yMinMaxNorm = (!yMax.Equals(yMin)) ? (_binsHeight - 1) / yMath.Subtract(yMax, yMin).ToDouble() : 0;


				var reducedPoints = new Point2DSeries(100);//??? cant estimate capacity at this time
				var sourceX = xColumn.ToUncheckedList();
				var sourceY = yColumn.ToUncheckedList();
				// for each input point:
				for (int seriesIndex = 0; seriesIndex < xColumn.Count; seriesIndex++)
				{
					var x = sourceX[seriesIndex];
					var y = sourceY[seriesIndex];
					//    map point into bins 2D index space
					//    if (bin[by,bx] == false) { bins[by,bx]=true; output this point }
					//     // else (if bin is busy) ignore the point
					var xIndex = (int)(xMinMaxNorm * xMath.Subtract(x, xMin).ToDouble());
					var yIndex = (int)(yMinMaxNorm * yMath.Subtract(y, yMin).ToDouble());
					var index = xIndex + yIndex*_binsWidth;
					if (_bins[index] == 0)
					{
						_bins[index] = 1;
						reducedPoints.Add(new Point2D(x.ToDouble(), y.ToDouble()));
					}
				}

                reducedPoints.Freeze();
				return reducedPoints;
			}
		}
    }
}