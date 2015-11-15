// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ChartSeriesViewModel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines the interface to a <see cref="ChartSeriesViewModel"/>, a viewmodel to a single data-render series pair, which is used in the new <see cref="UltrachartSurface"/> Mvvm API. 
    /// For usage, see the SeriesSource property and the Mvvm examples in the examples suite. You may bind SeriesSource to a collection of <see cref="IChartSeriesViewModel"/> 
    /// and Ultrachart will automatically associate the <see cref="BaseRenderableSeries"/> and <see cref="IDataSeries{TX,TY}"/> instances
    /// </summary>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="IDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries"/>
    /// <seealso cref="XyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    /// <seealso cref="IXyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="IXyzDataSeries"/>
    /// <seealso cref="XyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public interface IChartSeriesViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the DataSeries
        /// </summary>
        IDataSeries DataSeries { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IRenderableSeries"/> paired with the data
        /// </summary>
        IRenderableSeries RenderSeries { get; set; }
    }

    /// <summary>
    /// A viewmodel to a single data-render series pair, used in the new <see cref="UltrachartSurface"/> Mvvm API. 
    /// For usage, see the SeriesSource property and the Mvvm examples in the examples suite. You may bind SeriesSource to a collection of <see cref="IChartSeriesViewModel"/> 
    /// and Ultrachart will automatically associated the <see cref="BaseRenderableSeries"/> and <see cref="IDataSeries"/> instances
    /// </summary>
    /// <seealso cref="Ecng.Xaml.Charting.Model.DataSeries.DataSeries{TX,TY}"/>
    /// <seealso cref="IDataSeries"/>
    /// <seealso cref="IDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyDataSeries"/>
    /// <seealso cref="XyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries{TX,TY}"/>
    /// <seealso cref="IXyyDataSeries"/>
    /// <seealso cref="XyyDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IOhlcDataSeries"/>
    /// <seealso cref="OhlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries{TX,TY}"/>
    /// <seealso cref="IHlcDataSeries"/>
    /// <seealso cref="HlcDataSeries{TX,TY}"/>
    /// <seealso cref="IXyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="IXyzDataSeries"/>
    /// <seealso cref="XyzDataSeries{TX,TY,TZ}"/>
    /// <seealso cref="BoxPlotDataSeries{TX,TY}"/>
    /// <seealso cref="BaseRenderableSeries"/>
    /// <remarks>DataSeries are assigned to the RenderableSeries via the <see cref="IRenderableSeries.DataSeries"/> property. Any time a DataSeries is appended to, the
    /// parent <see cref="UltrachartSurface"/> will be redrawn</remarks>
    public class ChartSeriesViewModel : BindableObject, IChartSeriesViewModel
    {
        private IDataSeries _dataSeries;
        private IRenderableSeries _renderSeries;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartSeriesViewModel" /> class.
        /// </summary>
        /// <param name="dataSeries">The data series.</param>
        /// <param name="renderSeries">The render series paired with the data.</param>
        public ChartSeriesViewModel(IDataSeries dataSeries, IRenderableSeries renderSeries)
        {
            _dataSeries = dataSeries;
            _renderSeries = renderSeries;
        }

        /// <summary>
        /// Gets or sets the DataSeries
        /// </summary>
        public IDataSeries DataSeries
        {
            get { return _dataSeries; }
            set 
            { 
                _dataSeries = value;
                OnPropertyChanged("DataSeries");
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IRenderableSeries"/> paired with the data
        /// </summary>
        public IRenderableSeries RenderSeries
        {
            get { return _renderSeries; }
            set 
            { 
                _renderSeries = value;
                OnPropertyChanged("RenderSeries");
            }
        }
    }
}