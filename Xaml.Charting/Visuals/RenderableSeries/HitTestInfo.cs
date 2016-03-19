// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// HitTestInfo.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Model.DataSeries;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides information on a series hit test operation, see <see cref="BaseRenderableSeries.HitTest(Point, bool)"/> for more information
    /// </summary>
    public struct HitTestInfo
    {
        private bool _isEmpty;

        /// <summary>
        /// Gets special value which represents a <see cref="HitTestInfo"/> instance without data
        /// </summary>
        public static readonly HitTestInfo Empty = new HitTestInfo(true);

        /// <summary>
        /// Gets or sets the Name of the <see cref="IDataSeries"/> which was hit
        /// </summary>
        public string DataSeriesName { get; set; }

        /// <summary>
        /// Provides information about the type of <see cref="IDataSeries"/> that was hit tested
        /// </summary>
        public DataSeriesType DataSeriesType { get; set; }

        /// <summary>
        /// Gets or sets a point snapped to the X-Y value of the series
        /// </summary>
        public Point HitTestPoint { get; set; }

        /// <summary>
        /// Gets or sets a point snapped to the X-Y1 series
        /// </summary>
        public Point Y1HitTestPoint { get; set; }

        /// <summary>
        /// Gets or sets the X Value at the hit-test site
        /// </summary>
        public IComparable XValue { get; set; }

        /// <summary>
        /// Gets or sets the Y-Value at the hit-test site
        /// </summary>
        public IComparable YValue { get; set; }

        /// <summary>
        /// Gets or sets the Y1-Value at the hit-test site
        /// </summary>
        public IComparable Y1Value { get; set; }

        /// <summary>
        /// Gets or sets the Z-value at the hit-test site
        /// </summary>
        public IComparable ZValue { get; set; }

        /// <summary>
        /// Gets or sets the DataSeriesIndex a the hit-test site
        /// </summary>
        public int DataSeriesIndex { get; set; }

        /// <summary>
        /// Gets or sets whether the HitTest operation was a hit or not
        /// </summary>
        /// <remarks>Defined as the input point being within a small distance of the output hittest point</remarks>
        public bool IsHit { get; set; }

        /// <summary>
        /// Gets or sets whether the HitTest operation was a hit at X or not
        /// </summary>
        /// <remarks>Defined as the input point being within a small distance of the output hittest point</remarks>
        public bool IsVerticalHit { get; set; }

        /// <summary>
        /// Gets or sets whether the input point is between first and last series point or not
        /// </summary>
        public bool IsWithinDataBounds { get; set; }

        /// <summary>
        /// Gets or sets the Error High value at the hit-test site
        /// </summary>
        public IComparable ErrorHigh { get; set; }

        /// <summary>
        /// Gets or sets the Error Low value at the hit-test site
        /// </summary>
        public IComparable ErrorLow { get; set; }

        
        /// <summary>
        /// Gets or sets the Open-Value at the hit-test site
        /// </summary>
        public IComparable OpenValue { get; set; }

        /// <summary>
        /// Gets or sets the High-Value at the hit-test site
        /// </summary>
        public IComparable HighValue { get; set; }

        /// <summary>
        /// Gets or sets the Low-Value at the hit-test site
        /// </summary>
        public IComparable LowValue { get; set; }

        /// <summary>
        /// Gets or sets the Close-Value at the hit-test site
        /// </summary>
        public IComparable CloseValue { get; set; }


        
        /// <summary>
        /// Gets or sets the Minimum-Value at the hit-test site
        /// </summary>
        public IComparable Minimum { get; set; }

        /// <summary>
        /// Gets or sets the Maximum-Value at the hit-test site
        /// </summary>
        public IComparable Maximum { get; set; }

        /// <summary>
        /// Gets or sets the Median-Value at the hit-test site
        /// </summary>
        public IComparable Median { get; set; }

        /// <summary>
        /// Gets or sets the LowerQuartile-Value at the hit-test site
        /// </summary>
        public IComparable LowerQuartile { get; set; }

        /// <summary>
        /// Gets or sets the UpperQuartile-Value at the hit-test site
        /// </summary>
        public IComparable UpperQuartile { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public double Persentage { get; set; }

        /// <summary>
        /// Volume value for TimeframeSegmentDataSeries.
        /// </summary>
        public long Volume {get; set;}

        private HitTestInfo(bool isEmpty)
            : this()
        {
            _isEmpty = isEmpty;
        }

        /// <summary>
        /// Returns the value, indicating whether current instance of <see cref="HitTestInfo"/> is empty
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            return _isEmpty;
        }
    }
}