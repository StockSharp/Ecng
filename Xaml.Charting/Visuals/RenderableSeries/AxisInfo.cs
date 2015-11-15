// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisInfo.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Provides information on an axis hit test operation, see <see cref="AxisBase.HitTest(Point)"/> for more information
    /// </summary>
    public class AxisInfo : BindableObject
    {
        private string _axisId;
        private string _axisTitle;
        private AxisAlignment _axisAlignment;
        private IComparable _dataValue;
        private string _axisFormattedDataValue;
        private bool _isHorizontal;
        private bool _isXAxis;
        private string _cursorFormattedDataValue;
        private bool _isMasterChartAxis = true;

        /// <summary>
        /// Gets or sets the current Axis Id
        /// </summary>
        public string AxisId
        {
            get { return _axisId; }
            set
            {
                _axisId = value;
                OnPropertyChanged("AxisId");
            }
        }

        /// <summary>
        /// Gets or sets the current Axis Title
        /// </summary>
        public string AxisTitle
        {
            get { return _axisTitle; }
            set
            {
                _axisTitle = value;
                OnPropertyChanged("AxisTitle");
            }
        }

        /// <summary>
        /// Gets or sets the current Axis alignment
        /// </summary>
        public AxisAlignment AxisAlignment
        {
            get { return _axisAlignment; }
            set
            {
                _axisAlignment = value;
                OnPropertyChanged("AxisAlignment");
            }
        }

        /// <summary>
        /// Gets or sets the DataValue at the axis hit test point
        /// </summary>
        public IComparable DataValue
        {
            get { return _dataValue; }
            set
            {
                _dataValue = value;
                OnPropertyChanged("DataValue");
            }
        }

        /// <summary>
        /// Gets or sets a Formatted data value using the Axis.FormatText method
        /// </summary>
        public string AxisFormattedDataValue
        {
            get { return _axisFormattedDataValue; }
            set
            {
                _axisFormattedDataValue = value;
                OnPropertyChanged("AxisFormattedDataValue");
            }
        }

        ///// <summary>
        ///// Gets or sets the current orientation, X, or Y
        ///// </summary>
        //[Obsolete("AxisInfo.AxisOrientation property is obsolete, please, use AxisInfo.IsHorizontal instead", true)]
        //public string AxisOrientation
        //{
        //    get
        //    {
        //        throw new Exception(
        //            "AxisInfo.AxisOrientation property is obsolete, please, use AxisInfo.IsHorizontal instead");
        //    }
        //    set
        //    {
        //        throw new Exception(
        //            "AxisInfo.AxisOrientation property is obsolete, please, use AxisInfo.IsHorizontal instead");
        //    }
        //}

        /// <summary>
        /// Gets or sets the current orientation, indicating whether <see cref="IAxis"/> is horizontal or not
        /// </summary>
        public bool IsHorizontal
        {
            get { return _isHorizontal; }
            set
            {
                _isHorizontal = value;
                OnPropertyChanged("IsHorizontal");
            }
        }

        /// <summary>
        /// Gets or sets whether the current axis is an X-Axis or not
        /// </summary>
        public bool IsXAxis
        {
            get { return _isXAxis; }
            set
            {
                _isXAxis = value;
                OnPropertyChanged("IsXAxis");
            }
        }

        /// <summary>
        /// Gets or sets a Cursor Formatted data value, using the Axis.FormatCursorText method
        /// </summary>
        public string CursorFormattedDataValue
        {
            get { return _cursorFormattedDataValue; }
            set
            {
                _cursorFormattedDataValue = value;
                OnPropertyChanged("CursorFormattedDataValue");
            }
        }

        /// <summary>
        /// Gets or sets the value, indicating that the associated axis belongs to the surface,
        /// where a mouse event occured originally. See <see cref="Ecng.Xaml.Charting.ChartModifiers.ModifierEventArgsBase.IsMaster"/>
        /// </summary>
        public bool IsMasterChartAxis
        {
            get { return _isMasterChartAxis; }
            set
            {
                _isMasterChartAxis = value;
                OnPropertyChanged("IsMasterChartAxis");
            }
        }
    }
}