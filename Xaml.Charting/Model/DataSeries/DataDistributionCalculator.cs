// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DataDistributionCalculator.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Numerics.GenericMath;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Defines the interface to a DataDistributionCalculator
    /// </summary>
    /// <typeparam name="TX"></typeparam>
    public interface IDataDistributionCalculator<TX> where TX: IComparable
    {
		/// <summary>
		/// Gets whether this DataSeries contains Sorted data in the X-direction. 
		/// Note: Sorted data will result in far faster indexing operations. If at all possible, try to keep your data sorted in the X-direction
		/// </summary>
        bool DataIsSortedAscending { get; }

        /// <summary>
        /// Gets whether the data is evenly paced
        /// </summary>
        bool DataIsEvenlySpaced { get; }

        void UpdateDataDistributionFlagsWhenAppendedXValue(ISeriesColumn<TX> xValues, TX newXValue);
        void UpdateDataDistributionFlagsWhenAppendedXValues(ISeriesColumn<TX> xValues, int countBeforeAppending, IEnumerable<TX> newXValues);
        void UpdateDataDistributionFlagsWhenInsertedXValue(ISeriesColumn<TX> xValues, int indexWhereInserted, TX newXValue);
        void UpdateDataDistributionFlagsWhenInsertedXValues(ISeriesColumn<TX> xValues, int indexWhereInserted, int insertedCount, IEnumerable<TX> newXValues);
        void UpdateDataDistributionFlagsWhenRemovedXValues();

        /// <summary>
        /// Clears the DataDistributionCalculator flags
        /// </summary>
        void ClearDataDistributionFlags();
    }

    internal static class DataDistributionCalculatorFactory
    {
        internal static IDataDistributionCalculator<TX> Create<TX>()
            where TX : IComparable
        {
            if (typeof(TX) == typeof(double))
                return (IDataDistributionCalculator<TX>)new DoubleDataDistributionCalculator();
            if (typeof(TX) == typeof(float))
                return (IDataDistributionCalculator<TX>)new FloatDataDistributionCalculator();
            else 
                return new GenericDataDistributionCalculator<TX>();
        }
    }

    internal class GenericDataDistributionCalculator<TX> : IDataDistributionCalculator<TX>
            where TX : IComparable
    {
        private static readonly IMath<TX> _xMath = GenericMathFactory.New<TX>();
                protected bool _dataIsSortedAscending = true;
        public bool DataIsSortedAscending
        {
            get { return _dataIsSortedAscending; }
        }
        protected bool _dataIsEvenlySpaced = true;
        public bool DataIsEvenlySpaced { get { return _dataIsEvenlySpaced; } }

                private TX _firstXDiff;
        private bool _firstXDiffIsCalculated = false;

        public void UpdateDataDistributionFlagsWhenRemovedXValues()
        {
            _dataIsEvenlySpaced = false;
        }

        void UpdateDataDistributionFlagsWhenAppendedXValue(IList<TX> xValues, TX newXValue, int lastIndexAfterAppending)
        {
            if (_dataIsSortedAscending)
            {
                if (lastIndexAfterAppending > 0)
                {
                    // if more than 2 elements
                    if (xValues[lastIndexAfterAppending].CompareTo(xValues[lastIndexAfterAppending - 1]) < 0) // did it become unsorted?
                    {
                        _dataIsSortedAscending = false;
                        _dataIsEvenlySpaced = false;
                    }
                }
            }
            if (_dataIsEvenlySpaced)
            {
                if (lastIndexAfterAppending > 1)
                {
                    // if more than 3 elements
                    if (_firstXDiffIsCalculated == false)
                    {
                        _firstXDiff = _xMath.Subtract(xValues[1], xValues[0]);
                        _firstXDiffIsCalculated = true;
                    }

                    TX difference = _xMath.Subtract(xValues[lastIndexAfterAppending], xValues[lastIndexAfterAppending - 1]);
                    if (!difference.Equals(_firstXDiff))
                    {
                        // data became unevenly spaced
                        _dataIsEvenlySpaced = false;
                    }
                }
            }
        }
        public virtual void UpdateDataDistributionFlagsWhenAppendedXValue(ISeriesColumn<TX> xValues2, TX newXValue)
        {
            var xValues = (IList<TX>)xValues2;
            var lastIndexAfterAppending = xValues.Count - 1;
            UpdateDataDistributionFlagsWhenAppendedXValue(xValues2, newXValue, lastIndexAfterAppending);
        }
        public virtual void UpdateDataDistributionFlagsWhenAppendedXValues(
            ISeriesColumn<TX> xValues2,
            int countBeforeAppending, 
            IEnumerable<TX> newXValues)
        {
            var xValues = (IList<TX>)xValues2;
    
            TX previousXValue = default(TX);
            bool previousXValueIsValid = false;
            if (countBeforeAppending != 0)
            {
                previousXValue = xValues[countBeforeAppending - 1];
                previousXValueIsValid = true;
            }

          //  var lastIndexAfterAppending = countBeforeAppending;
            foreach (TX newXValue in newXValues)
            {
                if (!previousXValueIsValid)
                {
                    previousXValue = newXValue;
                    previousXValueIsValid = true;
                    continue;
                }

               // UpdateDataDistributionFlagsWhenAppendedXValue(xValues, item, lastIndexAfterAppending);
               // lastIndexAfterAppending++;
                if (_dataIsSortedAscending)
                {
                    if (newXValue.CompareTo(previousXValue) < 0) // did it become unsorted?
                    {
                        _dataIsSortedAscending = false;
                        _dataIsEvenlySpaced = false;
                        break;
                    }
                }

                if (_dataIsEvenlySpaced)
                {
                    var newXDiff = _xMath.Subtract(newXValue, previousXValue);

                    // if more than 3 elements
                    if (_firstXDiffIsCalculated == false)
                    {
                        _firstXDiff = newXDiff;
                        _firstXDiffIsCalculated = true;
                    }
                    else
                    {
                        if (!_firstXDiff.Equals(newXDiff))
                        {
                            // data became unevenly spaced
                            _dataIsEvenlySpaced = false;
                        }
                    }
                }

                previousXValue = newXValue;
            }
        }
        public virtual void UpdateDataDistributionFlagsWhenInsertedXValue(ISeriesColumn<TX> xValues2, int indexWhereInserted, TX newXValue)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
                var xValues = ((IList<TX>)xValues2);
                var xValuesCount = xValues.Count;

                if (indexWhereInserted == 0)
                {
                    // inserted at the beginning: it could still remain evenly spaced
                    if (xValuesCount > 1)
                    {
                        var xValue1 = xValues[1];
                        TX xDiff = _xMath.Subtract(xValue1, newXValue);
                        if (xDiff.CompareTo(_xMath.ZeroValue) < 0)
                        {
                            _dataIsSortedAscending = false;
                            _dataIsEvenlySpaced = false;
                            return;
                        }

                        if (_dataIsEvenlySpaced)
                        {
                            if (xValuesCount > 2)
                            {
                                if (!xDiff.Equals(_xMath.Subtract(xValues[2], xValue1)))
                                {
                                    // data became unevenly spaced
                                    _dataIsEvenlySpaced = false;
                                }
                            }
                        }
                    }
                }
                else if (indexWhereInserted == xValuesCount)
                {
                    // if inserted at the end
                    UpdateDataDistributionFlagsWhenAppendedXValue(xValues2, newXValue);
                }
                else
                {
                    // inserted in the middle
                    _dataIsEvenlySpaced = false;
                    if (_dataIsSortedAscending)
                    {
                        if (indexWhereInserted > 0)
                        {
                            var previousXValue = xValues[indexWhereInserted - 1];
                            if (previousXValue.CompareTo(newXValue) > 0) _dataIsSortedAscending = false;
                        }
                        if (indexWhereInserted < xValuesCount - 1)
                        {
                            var nextXValue = xValues[indexWhereInserted + 1];
                            if (nextXValue.CompareTo(newXValue) < 0) _dataIsSortedAscending = false;
                        }
                    }
                }
            }
        }
        public virtual void UpdateDataDistributionFlagsWhenInsertedXValues(ISeriesColumn<TX> xValues2, int indexWhereInserted, int insertedCount, IEnumerable<TX> newXValues)
        {
            var xValues = ((IList<TX>)xValues2);
            var xValuesCount = xValues.Count;
            if (indexWhereInserted + insertedCount == xValuesCount)
            {
                // inserted at the end: it could still remain evenly spaced
                UpdateDataDistributionFlagsWhenAppendedXValues(xValues2, xValuesCount - insertedCount, newXValues);
            }
            else if (indexWhereInserted == 0)
            {
                // inserted at the beginning: it could still remain evenly spaced
                if (xValuesCount > 2)
                {
                    var lastXValue = xValues[1];
                    var firstXDiff = _xMath.Subtract(lastXValue, xValues[0]);
                    _firstXDiff = firstXDiff;
                    _firstXDiffIsCalculated = true;

                    for (int i = 2; i < insertedCount; i++)
                    {
                        var newXValue = xValues[i];
                        var xDiff = _xMath.Subtract(newXValue, lastXValue);
                        lastXValue = newXValue;

                        if (xDiff.CompareTo(_xMath.ZeroValue) < 0)
                        {
                            _dataIsSortedAscending = false;
                            _dataIsEvenlySpaced = false;
                            break;
                        }

                        if (_dataIsEvenlySpaced)
                        {
                            if (!xDiff.Equals(firstXDiff))
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                                break;
                            }
                            firstXDiff = xDiff;
                        }
                    }
                }
            }
            else
            { // inserted in the middle
                _dataIsEvenlySpaced = false;
                if (_dataIsSortedAscending)
                { // is it still sorted?
                    var previousXValue = xValues[indexWhereInserted - 1];
                    int maxIndex = indexWhereInserted + insertedCount + 1;
                    for (int i = indexWhereInserted; i < maxIndex; i++) // i points to range from first inserted item to first item after inserted batch
                    {
                        var xValue = xValues[i];
                        if (xValue.CompareTo(previousXValue) < 0)
                        {
                            _dataIsSortedAscending = false;
                            break;
                        }
                        previousXValue = xValue;
                    }
                }
            }
        }
        public void ClearDataDistributionFlags()
        {
            _dataIsSortedAscending = true;
            _dataIsEvenlySpaced = true;
        }
    }
    internal class DoubleDataDistributionCalculator : GenericDataDistributionCalculator<double>
    {
        private const double MinVisibleRelativeXDiffError = (double)1/8000;
                double _lastXValue;
        double _firstXDiff;
        bool _firstXDiffIsValid = false;
        private double _minVisibleXDiffError;
        void UpdateDataDistributionFlagsWhenAppendedXValue(ISeriesColumn<double> xValues, double newXValue, int lastIndexAfterAppending)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
				if (lastIndexAfterAppending > 0)
                {
                    double xDiff = newXValue - _lastXValue;
                    if (xDiff < 0)
                    {
                        _dataIsSortedAscending = false;
                        _dataIsEvenlySpaced = false;
                        return;
                    }

                    if (_dataIsEvenlySpaced)
                    {
                        if (_firstXDiffIsValid)
                        {
                            double xDiffError = xDiff - _firstXDiff;
                            if (xDiffError < 0) xDiffError = -xDiffError;

                            if (xDiffError > _minVisibleXDiffError)
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                            }
                        }
                        else
                        {
                            _firstXDiffIsValid = true;
                            _firstXDiff = xDiff;
                            _minVisibleXDiffError = xDiff*MinVisibleRelativeXDiffError;
                        }
                    }
                }
                _lastXValue = newXValue;
            }
        }

	    public override void UpdateDataDistributionFlagsWhenAppendedXValue(ISeriesColumn<double> xValues,
	                                                                       double newXValue)
	    {
		    UpdateDataDistributionFlagsWhenAppendedXValue(xValues, newXValue, ((IList<double>) xValues).Count - 1);
	    }

	    void UpdateDataDistributionFlagsWhenAppendedDataArray(IList<double> xValues, int countBeforeAppending, double[] newXValues, int newXValuesLength)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
				int arrayLength = newXValuesLength;
                double minVisibleXDiffError = _minVisibleXDiffError;

                                if (countBeforeAppending > 0 && newXValuesLength > 0)
                {
                    var xDiff = newXValues[0] - _lastXValue;
                    if (_dataIsSortedAscending)
                    {
                        // check sorting with previous batch
                        if (_lastXValue > newXValues[0]) _dataIsSortedAscending = false;
                    }

                    if (_dataIsEvenlySpaced)
                    {
                        // check distance with previous batch
                        if (_firstXDiffIsValid && newXValuesLength > 0)
                        {
                            var error = xDiff - _firstXDiff;
                            if (error < 0) error = -error;
                            if (error > _minVisibleXDiffError)
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                            }
                        }
                        else
                        {
                            _firstXDiff = xDiff;
                            _firstXDiffIsValid = true;
                            _minVisibleXDiffError = xDiff * MinVisibleRelativeXDiffError;
                        }
                    }
                }

                bool nativeCodeHasWorked = false;
//#if !SILVERLIGHT
//                var nativeDataDistributionCalculator = Utility.UnmanagedDll.DllLoader.NativeDataDistributionCalculator;
//                if (nativeDataDistributionCalculator != null)
//                {
//                    nativeDataDistributionCalculator.IsDataEvenlySpacedAndSorted_double(newXValues, ref _dataIsEvenlySpaced,
//                                                                                        ref _dataIsSortedAscending);
//                    nativeCodeHasWorked = true;
//                }
//#endif

                if (nativeCodeHasWorked == false)
                { // do it in C# if native module is unavailable
                    unchecked
                    {
                        // cache variables into stack or CPU registers
                        var lastXValue = newXValues[0];
                        var firstXDiff = _firstXDiff;
                        var firstXDiffIsValid = _firstXDiffIsValid;

                        for (int i = 1; i < arrayLength; i++)
                        {
                            var newXValue = newXValues[i];
                            double xDiff = newXValue - lastXValue;
                            if (xDiff < 0)
                            {
                                _dataIsSortedAscending = false;
                                _dataIsEvenlySpaced = false;
                                break;
                            }
                            lastXValue = newXValue;

                            if (_dataIsEvenlySpaced)
                            {
                                if (firstXDiffIsValid)
                                {
                                    double error = xDiff - firstXDiff;
                                    if (error < 0) error = -error;
                                    if (error > minVisibleXDiffError)
                                    {
                                        // data became unevenly spaced
                                        _dataIsEvenlySpaced = false;
                                    }
                                }
                                else
                                {
                                    firstXDiff = xDiff;
                                    firstXDiffIsValid = true;
                                }
                            }
                        }

                        _lastXValue = lastXValue;
                        _firstXDiffIsValid = firstXDiffIsValid;
                        _firstXDiff = firstXDiff;
                    }
                }
                _lastXValue = newXValues[arrayLength - 1];
                if (_firstXDiffIsValid == false && arrayLength > 1)
                {
                    _firstXDiff = _lastXValue - newXValues[arrayLength - 2];
                    _firstXDiffIsValid = true;
                    _minVisibleXDiffError = _firstXDiff * MinVisibleRelativeXDiffError;
                }
            }
        }
        public override void UpdateDataDistributionFlagsWhenAppendedXValues(ISeriesColumn<double> xValues, int countBeforeAppending, IEnumerable<double> newXValues)
        {
            // Array case (Fastest)
            var array = newXValues as double[];
            if (array != null)
            {
                UpdateDataDistributionFlagsWhenAppendedDataArray(xValues, countBeforeAppending, array, array.Length);
                return;
            }

            // IList case (Fast)
            var iList = newXValues as IList<double>;
            if (iList != null)
            {
                int count = iList.Count;
                var iListArray = iList.ToUncheckedList();
                UpdateDataDistributionFlagsWhenAppendedDataArray(xValues, countBeforeAppending, iListArray, count);
                return;
            }

            // IEnumerable case (slowest)
            var doubleEnumerable = newXValues;
	        int indexAfterAppending = countBeforeAppending;
            foreach (double item in doubleEnumerable)
            {
                UpdateDataDistributionFlagsWhenAppendedXValue(xValues, item, indexAfterAppending);
	            indexAfterAppending++;
            }
        }
        public override void UpdateDataDistributionFlagsWhenInsertedXValue(ISeriesColumn<double> xValues2, int indexWhereInserted, double newXValue)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
                var xValues = ((IList<double>)xValues2);
                var xValuesCount = xValues.Count;

                if (indexWhereInserted == 0)
                {
                    // inserted at the beginning: it could still remain evenly spaced
                    if (xValuesCount > 1)
                    {
                        var xValue1 = xValues[1];
                        double xDiff = xValue1 - newXValue;
                        _firstXDiff = xDiff;
                        _firstXDiffIsValid = true;
                        if (xDiff < 0)
                        {
                            _dataIsSortedAscending = false;
                            _dataIsEvenlySpaced = false;
                            return;
                        }
                      

                        if (_dataIsEvenlySpaced)
                        {
                            if (_firstXDiffIsValid)
                            {
                                double error = xDiff - (xValues[2] - xValue1);
                                if (error < 0) error = -error;
                                if (error > _minVisibleXDiffError)
                                {
                                    // data became unevenly spaced
                                    _dataIsEvenlySpaced = false;
                                }
                            }
                        }
                    }
                }
                else if (indexWhereInserted == xValuesCount)
                {
                    // if inserted at the end
                    UpdateDataDistributionFlagsWhenAppendedXValue(xValues2, newXValue);
                }
                else
                {
                    // inserted in the middle
                    _dataIsEvenlySpaced = false;
                    if (_dataIsSortedAscending)
                    {
                        if (indexWhereInserted > 0)
                        {
                            var previousXValue = xValues[indexWhereInserted - 1];
                            if (previousXValue > newXValue) _dataIsSortedAscending = false;
                        }
                        if (indexWhereInserted < xValuesCount - 1)
                        {
                            var nextXValue = xValues[indexWhereInserted + 1];
                            if (nextXValue < newXValue) _dataIsSortedAscending = false;
                        }
                    }
                }
            }
        }
        public override void UpdateDataDistributionFlagsWhenInsertedXValues(ISeriesColumn<double> xValues2, int indexWhereInserted, int insertedCount, IEnumerable<double> newXValues)
        {
            var xValues = ((IList<double>)xValues2);
            var xValuesCount = xValues.Count;
            if (indexWhereInserted + insertedCount == xValuesCount)
            {
                // inserted at the end: it could still remain evenly spaced
                UpdateDataDistributionFlagsWhenAppendedXValues(xValues2, xValuesCount - insertedCount, newXValues);
            }
            else if (indexWhereInserted == 0)
            {
                // inserted at the beginning: it could still remain evenly spaced
                if (xValuesCount > 2)
                {
                    var lastXValue = xValues[1];
                    var firstXDiff = lastXValue - xValues[0];
                    _firstXDiff = firstXDiff;
                    _firstXDiffIsValid = true;
                    _minVisibleXDiffError = _firstXDiff * MinVisibleRelativeXDiffError;
                    var minVisibleXDiffError = _minVisibleXDiffError;

                    // analyze XValues as IList, access it by index
                    for (int i = 2; i < insertedCount; i++)
                    {
                        var newXValue = xValues[i];
                        double xDiff = newXValue - _lastXValue;
                        _lastXValue = newXValue;

                        if (xDiff < 0)
                        {
                            _dataIsSortedAscending = false;
                            _dataIsEvenlySpaced = false;
                            break;
                        }

                        if (_dataIsEvenlySpaced)
                        {
                            double error = xDiff - firstXDiff;
                            if (error < 0) error = -error;
                            if (error > minVisibleXDiffError)
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
            { // inserted in the middle
                _dataIsEvenlySpaced = false;
                if (_dataIsSortedAscending)
                { // is it still sorted?
                    var previousXValue = xValues[indexWhereInserted - 1];
                    int maxIndex = indexWhereInserted + insertedCount + 1;
                    for (int i = indexWhereInserted; i < maxIndex; i++) // i points to range from first inserted item to first item after inserted batch
                    {
                        var xValue = xValues[i];
                        if (xValue < previousXValue)
                        {
                            _dataIsSortedAscending = false;
                            break;
                        }
                        previousXValue = xValue;
                    }
                }
            }
        }
    }
    internal class FloatDataDistributionCalculator : GenericDataDistributionCalculator<float>
    {
        private const float MinVisibleRelativeXDiffError = (float)1 / 8000;
                double _lastXValue;
        double _firstXDiff;
        bool _firstXDiffIsValid = false;
        private double _minVisibleXDiffError;
		void UpdateDataDistributionFlagsWhenAppendedXValue(ISeriesColumn<float> xValues, float newXValue, int lastIndexAfterAppending)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
				if (lastIndexAfterAppending > 0)
                {
                    double xDiff = newXValue - _lastXValue;
                    if (xDiff < 0)
                    {
                        _dataIsSortedAscending = false;
                        _dataIsEvenlySpaced = false;
                        return;
                    }

                    if (_dataIsEvenlySpaced)
                    {
                        if (_firstXDiffIsValid)
                        {
                            double xDiffError = xDiff - _firstXDiff;
                            if (xDiffError < 0) xDiffError = -xDiffError;

                            if (xDiffError > _minVisibleXDiffError)
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                            }
                        }
                        else
                        {
                            _firstXDiffIsValid = true;
                            _firstXDiff = xDiff;
                            _minVisibleXDiffError = xDiff * MinVisibleRelativeXDiffError;
                        }
                    }
                }
                _lastXValue = newXValue;
            }
        }
		public override void UpdateDataDistributionFlagsWhenAppendedXValue(ISeriesColumn<float> xValues, float newXValue)
		{
			UpdateDataDistributionFlagsWhenAppendedXValue(xValues, newXValue, ((IList<float>)xValues).Count - 1);
		}
		void UpdateDataDistributionFlagsWhenAppendedDataArray(IList<float> xValues, int countBeforeAppending, float[] newXValues, int newXValuesLength)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
                int arrayLength = newXValuesLength;
                double minVisibleXDiffError = _minVisibleXDiffError;

                                if (countBeforeAppending > 0 && newXValuesLength > 0)
                {
                    var xDiff = newXValues[0] - _lastXValue;
                    if (_dataIsSortedAscending)
                    {
                        // check sorting with previous batch
                        if (_lastXValue > newXValues[0]) _dataIsSortedAscending = false;
                    }

                    if (_dataIsEvenlySpaced)
                    {
                        // check distance with previous batch
                        if (_firstXDiffIsValid && newXValuesLength > 0)
                        {
                            var error = xDiff - _firstXDiff;
                            if (error < 0) error = -error;
                            if (error > _minVisibleXDiffError)
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                            }
                        }
                        else
                        {
                            _firstXDiff = xDiff;
                            _firstXDiffIsValid = true;
                            _minVisibleXDiffError = xDiff * MinVisibleRelativeXDiffError;
                        }
                    }
                }

                bool nativeCodeHasWorked = false;
                //#if !SILVERLIGHT
                //                var nativeDataDistributionCalculator = Utility.UnmanagedDll.DllLoader.NativeDataDistributionCalculator;
                //                if (nativeDataDistributionCalculator != null)
                //                {
                //                    nativeDataDistributionCalculator.IsDataEvenlySpacedAndSorted_double(newXValues, ref _dataIsEvenlySpaced,
                //                                                                                        ref _dataIsSortedAscending);
                //                    nativeCodeHasWorked = true;
                //                }
                //#endif

                if (nativeCodeHasWorked == false)
                { // do it in C# if native module is unavailable
                    unchecked
                    {
                        // cache variables into stack or CPU registers
                        var lastXValue = newXValues[0];
                        var firstXDiff = _firstXDiff;
                        var firstXDiffIsValid = _firstXDiffIsValid;

                        for (int i = 1; i < arrayLength; i++)
                        {
                            var newXValue = newXValues[i];
                            double xDiff = newXValue - lastXValue;
                            if (xDiff < 0)
                            {
                                _dataIsSortedAscending = false;
                                _dataIsEvenlySpaced = false;
                                break;
                            }
                            lastXValue = newXValue;

                            if (_dataIsEvenlySpaced)
                            {
                                if (firstXDiffIsValid)
                                {
                                    double error = xDiff - firstXDiff;
                                    if (error < 0) error = -error;
                                    if (error > minVisibleXDiffError)
                                    {
                                        // data became unevenly spaced
                                        _dataIsEvenlySpaced = false;
                                    }
                                }
                                else
                                {
                                    firstXDiff = xDiff;
                                    firstXDiffIsValid = true;
                                }
                            }
                        }

                        _lastXValue = lastXValue;
                        _firstXDiffIsValid = firstXDiffIsValid;
                        _firstXDiff = firstXDiff;
                    }
                }
                _lastXValue = newXValues[arrayLength - 1];
                if (_firstXDiffIsValid == false && arrayLength > 1)
                {
                    _firstXDiff = _lastXValue - newXValues[arrayLength - 2];
                    _firstXDiffIsValid = true;
                    _minVisibleXDiffError = _firstXDiff * MinVisibleRelativeXDiffError;
                }
            }
        }
        public override void UpdateDataDistributionFlagsWhenAppendedXValues(ISeriesColumn<float> xValues, int countBeforeAppending, IEnumerable<float> newXValues)
        {
            // Array case (Fastest)
            var array = newXValues as float[];
            if (array != null)
            {
                UpdateDataDistributionFlagsWhenAppendedDataArray(xValues, countBeforeAppending, array, array.Length);
                return;
            }

            // IList case (Fast)
            var iList = newXValues as IList<float>;
            if (iList != null)
            {
                int count = iList.Count;
                var iListArray = iList.ToUncheckedList();
                UpdateDataDistributionFlagsWhenAppendedDataArray(xValues, countBeforeAppending, iListArray, count);
                return;
            }

            // IEnumerable case (slowest)
            var doubleEnumerable = newXValues;
	        int indexAfterAppending = countBeforeAppending;
            foreach (float item in doubleEnumerable)
            {
                UpdateDataDistributionFlagsWhenAppendedXValue(xValues, item, indexAfterAppending);
	            indexAfterAppending++;
            }
        }
        public override void UpdateDataDistributionFlagsWhenInsertedXValue(ISeriesColumn<float> xValues2, int indexWhereInserted, float newXValue)
        {
            if (_dataIsSortedAscending || _dataIsEvenlySpaced)
            {
                var xValues = ((IList<float>)xValues2);
                var xValuesCount = xValues.Count;

                if (indexWhereInserted == 0)
                {
                    // inserted at the beginning: it could still remain evenly spaced
                    if (xValuesCount > 1)
                    {
                        var xValue1 = xValues[1];
                        double xDiff = xValue1 - newXValue;
                        _firstXDiff = xDiff;
                        _firstXDiffIsValid = true;
                        if (xDiff < 0)
                        {
                            _dataIsSortedAscending = false;
                            _dataIsEvenlySpaced = false;
                            return;
                        }


                        if (_dataIsEvenlySpaced)
                        {
                            if (_firstXDiffIsValid)
                            {
                                double error = xDiff - (xValues[2] - xValue1);
                                if (error < 0) error = -error;
                                if (error > _minVisibleXDiffError)
                                {
                                    // data became unevenly spaced
                                    _dataIsEvenlySpaced = false;
                                }
                            }
                        }
                    }
                }
                else if (indexWhereInserted == xValuesCount)
                {
                    // if inserted at the end
                    UpdateDataDistributionFlagsWhenAppendedXValue(xValues2, newXValue);
                }
                else
                {
                    // inserted in the middle
                    _dataIsEvenlySpaced = false;
                    if (_dataIsSortedAscending)
                    {
                        if (indexWhereInserted > 0)
                        {
                            var previousXValue = xValues[indexWhereInserted - 1];
                            if (previousXValue > newXValue) _dataIsSortedAscending = false;
                        }
                        if (indexWhereInserted < xValuesCount - 1)
                        {
                            var nextXValue = xValues[indexWhereInserted + 1];
                            if (nextXValue < newXValue) _dataIsSortedAscending = false;
                        }
                    }
                }
            }
        }
        public override void UpdateDataDistributionFlagsWhenInsertedXValues(ISeriesColumn<float> xValues2, int indexWhereInserted, int insertedCount, IEnumerable<float> newXValues)
        {
            var xValues = ((IList<float>)xValues2);
            var xValuesCount = xValues.Count;
            if (indexWhereInserted + insertedCount == xValuesCount)
            {
                // inserted at the end: it could still remain evenly spaced
                UpdateDataDistributionFlagsWhenAppendedXValues(xValues2, xValuesCount - insertedCount, newXValues);
            }
            else if (indexWhereInserted == 0)
            {
                // inserted at the beginning: it could still remain evenly spaced
                if (xValuesCount > 2)
                {
                    var lastXValue = xValues[1];
                    var firstXDiff = lastXValue - xValues[0];
                    _firstXDiff = firstXDiff;
                    _firstXDiffIsValid = true;
                    _minVisibleXDiffError = _firstXDiff * MinVisibleRelativeXDiffError;
                    var minVisibleXDiffError = _minVisibleXDiffError;

                    // analyze XValues as IList, access it by index
                    for (int i = 2; i < insertedCount; i++)
                    {
                        var newXValue = xValues[i];
                        double xDiff = newXValue - _lastXValue;
                        _lastXValue = newXValue;

                        if (xDiff < 0)
                        {
                            _dataIsSortedAscending = false;
                            _dataIsEvenlySpaced = false;
                            break;
                        }

                        if (_dataIsEvenlySpaced)
                        {
                            double error = xDiff - firstXDiff;
                            if (error < 0) error = -error;
                            if (error > minVisibleXDiffError)
                            {
                                // data became unevenly spaced
                                _dataIsEvenlySpaced = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
            { // inserted in the middle
                _dataIsEvenlySpaced = false;
                if (_dataIsSortedAscending)
                { // is it still sorted?
                    var previousXValue = xValues[indexWhereInserted - 1];
                    int maxIndex = indexWhereInserted + insertedCount + 1;
                    for (int i = indexWhereInserted; i < maxIndex; i++) // i points to range from first inserted item to first item after inserted batch
                    {
                        var xValue = xValues[i];
                        if (xValue < previousXValue)
                        {
                            _dataIsSortedAscending = false;
                            break;
                        }
                        previousXValue = xValue;
                    }
                }
            }
        }
    }

}
