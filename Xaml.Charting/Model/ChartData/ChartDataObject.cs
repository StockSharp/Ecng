// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ChartDataObject.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Linq;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Provides a ViewModel containing info about chart series, which can be bound to to create Rollover or legends
    /// </summary>
    public class ChartDataObject : BindableObject
    {
        private bool _showVisibilityCheckboxes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartDataObject"/> class.
        /// </summary>
        /// <remarks></remarks>
        public ChartDataObject()
        {
            SeriesInfo = new ObservableCollection<SeriesInfo>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartDataObject"/> class.
        /// </summary>
        /// <param name="seriesInfos">The series infos.</param>
        /// <remarks></remarks>
        public ChartDataObject(IEnumerable<SeriesInfo> seriesInfos) : this()
        {
            UpdateSeriesInfo(seriesInfos);
        }

        /// <summary>
        /// Workaround - used by UltrachartLegend items to bind to UltrachartLegend.ShowVisibilityCheckboxes. This property is set as a proxy 
        /// by the UltrachartLegend control itself and data-bound in the themes
        /// </summary>
        public bool ShowVisibilityCheckboxes
        {
            get { return _showVisibilityCheckboxes; }
            set
            {
                if (_showVisibilityCheckboxes == value) return;
                _showVisibilityCheckboxes = value;
                OnPropertyChanged("ShowVisibilityCheckboxes");
            }
        }
        

        /// <summary>
        /// Gets or sets a collection of <see cref="SeriesInfo"/> instances
        /// </summary>
        /// <value>The series info.</value>
        /// <remarks></remarks>
        public ObservableCollection<SeriesInfo> SeriesInfo {get;}


        public void UpdateSeriesInfo(IEnumerable<SeriesInfo> newInfos) {
            var oldInfos = SeriesInfo.ToDictionary(si => si.SeriesInfoKey);
            var newInfosDict = newInfos.ToDictionary(si => si.SeriesInfoKey);

            SeriesInfo.RemoveWhere(si => !newInfosDict.ContainsKey(si.SeriesInfoKey));
            newInfosDict.Values
                .Where(si => si.RenderableSeries.GetIncludeSeries(Modifier.Cursor) && !oldInfos.ContainsKey(si.SeriesInfoKey))
                .ForEachDo(si => SeriesInfo.Add(si));

            SeriesInfo.ForEachDo(si => si.CopyFrom(newInfosDict[si.SeriesInfoKey]));
        }
    }
}