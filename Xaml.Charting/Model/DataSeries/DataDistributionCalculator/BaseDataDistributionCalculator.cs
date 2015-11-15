using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Xaml.Charting.Numerics.GenericMath;

namespace Ecng.Xaml.Charting.Model.DataSeries
{   
    public abstract class BaseDataDistributionCalculator<TX> : IDataDistributionCalculator<TX>
        where TX : IComparable
    {        
        internal const String DataInsertedUnsortedWarning =
            "Data has been Inserted to a DataSeries which is unsorted in the X-Direction. " + PostAmble;

        internal const String DataAppendedUnsortedWarning =
            "Data has been Appended to a DataSeries which is unsorted in the X-Direction. " + PostAmble;

        private const String PostAmble = "Unsorted data can have severe performance implications in Ultrachart.\r\n" +
            "For maximum performance, please double-check that you are only inserting sorted data to Ultrachart. " +
            "Alternatively, to disable this warning and allow unsorted data, please set DataSeries.AcceptsUnsortedData = true. " +
            "For more info see Performance Tips and Tricks at http://support.ultrachart.com/index.php?/Knowledgebase/Article/View/17227/36/performance-tips-and-tricks";

        /// <summary>
        /// Gets whether this DataSeries contains Sorted data in the X-direction.
        /// Note: Sorted data will result in far faster indexing operations. If at all possible, try to keep your data sorted in the X-direction
        /// </summary>
        public bool DataIsSortedAscending { get; protected set; }

        /// <summary>
        /// Gets whether the data is evenly paced
        /// </summary>
        public bool DataIsEvenlySpaced { get; protected set; }

        /// <summary>
        /// Updates the data distribution flags when x values removed.
        /// </summary>
        public void UpdateDataDistributionFlagsWhenRemovedXValues()
        {
            DataIsEvenlySpaced = false;
        }

        /// <summary>
        /// Called when X Values are appended. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public abstract void OnAppendXValue(
            ISeriesColumn<TX> xValues,
            TX newXValue, 
            bool acceptsUnsortedData);

        /// <summary>
        /// Called when X Values are appended. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="countBeforeAppending"></param>
        /// <param name="newXValues"></param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public abstract void OnAppendXValues(
            ISeriesColumn<TX> xValues,
            int countBeforeAppending,
            IEnumerable<TX> newXValues,
            bool acceptsUnsortedData);

        /// <summary>
        /// Called when X Values are inserted. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="indexWhereInserted"></param>
        /// <param name="insertedCount"></param>
        /// <param name="newXValues"></param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public abstract void OnInsertXValues(
            ISeriesColumn<TX> xValues,
            int indexWhereInserted, 
            int insertedCount,
            IEnumerable<TX> newXValues,
            bool acceptsUnsortedData);

        /// <summary>
        /// Called when X Values are inserted. Should update the Data Distribution flags
        /// </summary>
        /// <param name="xValues">The x values.</param>
        /// <param name="indexWhereInserted"></param>
        /// <param name="newXValue">The new x value.</param>
        /// <param name="acceptsUnsortedData">if set to <c>true</c> the series accepts unsorted data.</param>
        public abstract void OnInsertXValue(
            ISeriesColumn<TX> xValues,
            int indexWhereInserted,
            TX newXValue,
            bool acceptsUnsortedData);

        /// <summary>
        /// Clears the DataDistributionCalculator flags
        /// </summary>
        public virtual void Clear()
        {
            DataIsSortedAscending = true;
            DataIsEvenlySpaced = true;
        }
    }
}