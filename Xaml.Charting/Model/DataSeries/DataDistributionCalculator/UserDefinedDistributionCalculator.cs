using System;
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Allows user provided flags for IsSortedAscending and IsEvenlySpaced, flags which are used to determine the correct algorithm for sorting, searching and data-compression in Ultrachart. 
    /// Overridding these flags allows for faster operation where the data distribution is known in advance
    /// </summary>
    /// <typeparam name="TX">The type of the x-data.</typeparam>
    public class UserDefinedDistributionCalculator<TX> : BaseDataDistributionCalculator<TX>
        where TX : IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDefinedDistributionCalculator{TX}"/> class.
        /// </summary>
        public UserDefinedDistributionCalculator()
        {
            IsSortedAscending = true;
            IsEvenlySpaced = true;
        }

        /// <summary>
        /// Gets or sets if the data is sorted
        /// </summary>
        public bool IsSortedAscending
        {
            get { return base.DataIsSortedAscending; }
            set { base.DataIsSortedAscending = value; }
        }

        /// <summary>
        /// Gets or sets if the data is evenly spaced, within a visual epsilon (typically 1.0/8000.0 of the default spacing)
        /// </summary>
        public bool IsEvenlySpaced
        {
            get { return base.DataIsEvenlySpaced; }
            set { base.DataIsEvenlySpaced = value; }
        }

        /// <summary>
        /// Called when X Values are appended. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public override void OnAppendXValue(ISeriesColumn<TX> xValues, TX newXValue, bool acceptsUnsortedData)
        {
            // Do nothing
        }

        /// <summary>
        /// Called when X Values are appended. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="countBeforeAppending"></param>
        /// <param name="newXValues"></param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public override void OnAppendXValues(ISeriesColumn<TX> xValues, int countBeforeAppending, IEnumerable<TX> newXValues, bool acceptsUnsortedData)
        {
            // Do nothing
        }

        /// <summary>
        /// Called when X Values are inserted. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="indexWhereInserted"></param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public override void OnInsertXValue(ISeriesColumn<TX> xValues, int indexWhereInserted, TX newXValue, bool acceptsUnsortedData)
        {
            // Do nothing
        }

        /// <summary>
        /// Called when X Values are inserted. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="indexWhereInserted"></param>
        /// <param name="insertedCount"></param>
        /// <param name="newXValues"></param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public override void OnInsertXValues(ISeriesColumn<TX> xValues, int indexWhereInserted, int insertedCount, IEnumerable<TX> newXValues,
            bool acceptsUnsortedData)
        {
            // Do nothing
        }
    }
}