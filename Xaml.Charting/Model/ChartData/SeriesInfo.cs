// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// SeriesInfo.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Media;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Provides a ViewModel containing info about an Xy series, such as name, x, y values, color
    /// </summary>
    public class XySeriesInfo : SeriesInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XySeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The renderable series.</param>
        /// <param name="hitTestInfo"> </param>
        public XySeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo){}
    }

    /// <summary>
    /// Provides a ViewModel containing info about an BoxPlot series, such as name, open high low close values, color
    /// </summary>
    public class BoxPlotSeriesInfo : SeriesInfo
    {
        private double _minimumValue;
        private double _maximumValue;
        private double _lowerQuartileValue;
        private double _upperQuartileValue;
        private double _medianValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxPlotSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The RenderableSeries instance that this <see cref="SeriesInfo"/> is bound to.</param>
        /// <param name="hitTestInfo"> </param>
        public BoxPlotSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            MinimumValue = Convert.ToDouble(hitTestInfo.Minimum);
            MaximumValue = Convert.ToDouble(hitTestInfo.Maximum);
            MedianValue = Convert.ToDouble(hitTestInfo.Median);

            LowerQuartileValue = Convert.ToDouble(hitTestInfo.LowerQuartile);
            UpperQuartileValue = Convert.ToDouble(hitTestInfo.UpperQuartile);
        }

        /// <summary>
        /// Gets or sets the Minimum value
        /// </summary>
        public double MinimumValue
        {
            get { return _minimumValue; }
            set
            {
                _minimumValue = value;

                OnPropertyChanged("MinimumValue");
                OnPropertyChanged("FormattedMinimumValue");
            }
        }

        /// <summary>
        /// Gets a formatted MinimumValue.
        /// </summary>
        public string FormattedMinimumValue
        {
            get { return GetYCursorFormattedValue(MinimumValue); }
        }

        /// <summary>
        /// Gets or sets the Maximum value
        /// </summary>
        public double MaximumValue
        {
            get { return _maximumValue; }
            set
            {
                _maximumValue = value;

                OnPropertyChanged("MaximumValue");
                OnPropertyChanged("FormattedMaximumValue");
            }
        }

        /// <summary>
        /// Gets a formatted MaximumValue.
        /// </summary>
        public string FormattedMaximumValue
        {
            get { return GetYCursorFormattedValue(MaximumValue); }
        }

        /// <summary>
        /// Gets or sets the Median value
        /// </summary>
        public double MedianValue
        {
            get { return _medianValue; }
            set
            {
                _medianValue = value;

                OnPropertyChanged("MedianValue");
                OnPropertyChanged("FormattedMedianValue");
            }
        }

        /// <summary>
        /// Gets a formatted MedianValue.
        /// </summary>
        public string FormattedMedianValue
        {
            get { return GetYCursorFormattedValue(MedianValue); }
        }

        /// <summary>
        /// Gets or sets the LowerQuartile value
        /// </summary>
        public double LowerQuartileValue
        {
            get { return _lowerQuartileValue; }
            set
            {
                _lowerQuartileValue = value;

                OnPropertyChanged("LowerQuartileValue");
                OnPropertyChanged("FormattedLowerQuartileValue");
            }
        }

        /// <summary>
        /// Gets a formatted LowerQuartileValue.
        /// </summary>
        public string FormattedLowerQuartileValue
        {
            get { return GetYCursorFormattedValue(LowerQuartileValue); }
        }

        /// <summary>
        /// Gets or sets the UpperQuartile value
        /// </summary>
        public double UpperQuartileValue
        {
            get { return _upperQuartileValue; }
            set
            {
                _upperQuartileValue = value;

                OnPropertyChanged("UpperQuartileValue");
                OnPropertyChanged("FormattedUpperQuartileValue");
            }
        }

        /// <summary>
        /// Gets a formatted UpperQuartileValue.
        /// </summary>
        public string FormattedUpperQuartileValue
        {
            get { return GetYCursorFormattedValue(UpperQuartileValue); }
        }
    }

    /// <summary>
    /// Provides a ViewModel containing info about an HLC series, such as name, high low values, color
    /// </summary>
    public class HlcSeriesInfo : SeriesInfo
    {
        private double _highValue;
        private double _lowValue;
        private double _closeValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="OhlcSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The RenderableSeries instance that this <see cref="SeriesInfo"/> is bound to.</param>
        /// <param name="hitTestInfo"> </param>
        public HlcSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            HighValue = Convert.ToDouble(hitTestInfo.ErrorHigh);
            LowValue = Convert.ToDouble(hitTestInfo.ErrorLow);
            CloseValue = Convert.ToDouble(hitTestInfo.YValue);
        }

        /// <summary>
        /// Gets or sets the High value.
        /// </summary>
        public double HighValue
        {
            get { return _highValue; }
            set
            {
                _highValue = value;

                OnPropertyChanged("HighValue");
                OnPropertyChanged("FormattedHighValue");
            }
        }

        /// <summary>
        /// Gets a formatted HighValue.
        /// </summary>
        public string FormattedHighValue
        {
            get { return GetYCursorFormattedValue(HighValue); }
        }

        /// <summary>
        /// Gets or sets the Low value.
        /// </summary>
        public double LowValue
        {
            get { return _lowValue; }
            set
            {
                _lowValue = value;

                OnPropertyChanged("LowValue");
                OnPropertyChanged("FormattedLowValue");
            }
        }

        /// <summary>
        /// Gets a formatted LowValue.
        /// </summary>
        public string FormattedLowValue
        {
            get { return GetYCursorFormattedValue(LowValue); }
        }

        /// <summary>
        /// Gets or sets the Close value.
        /// </summary>
        public double CloseValue
        {
            get { return _closeValue; }
            set
            {
                _closeValue = value;

                OnPropertyChanged("CloseValue");
                OnPropertyChanged("FormattedCloseValue");
            }
        }

        /// <summary>
        /// Gets a formatted CloseValue.
        /// </summary>
        public string FormattedCloseValue
        {
            get { return GetYCursorFormattedValue(CloseValue); }
        }
    } 

    /// <summary>
    /// Provides a ViewModel containing info about an OHLC series, such as name, open high low close values, color
    /// </summary>
    public class OhlcSeriesInfo : HlcSeriesInfo
    {
        private double _openValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="OhlcSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The RenderableSeries instance that this <see cref="SeriesInfo"/> is bound to.</param>
        /// <param name="hitTestInfo"> </param>
        public OhlcSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            OpenValue = Convert.ToDouble(hitTestInfo.OpenValue);
            HighValue = Convert.ToDouble(hitTestInfo.HighValue);
            LowValue = Convert.ToDouble(hitTestInfo.LowValue);
            CloseValue = Convert.ToDouble(hitTestInfo.CloseValue);
        }

        /// <summary>
        /// Gets or sets the Open value.
        /// </summary>
        public double OpenValue
        {
            get { return _openValue; }
            set
            {
                _openValue = value;

                OnPropertyChanged("OpenValue");
                OnPropertyChanged("FormattedOpenValue");
            }
        }

        /// <summary>
        /// Gets a formatted OpenValue.
        /// </summary>
        public string FormattedOpenValue
        {
            get { return GetYCursorFormattedValue(OpenValue); }
        }
    }

    /// <summary>
    /// Provides a ViewModel containing info about an Xyy series
    /// </summary>
    public class BandSeriesInfo: SeriesInfo
    {
        private IComparable _y1Value;
        private Point _xy1Coordinate;
        private bool _isFirstSeries;

        /// <summary>
        /// Initializes a new instance of the <see cref="BandSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The <see cref="IRenderableSeries"/>.</param>
        /// <param name="hitTestInfo"> </param>
        public BandSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            Y1Value = hitTestInfo.Y1Value;
            Xy1Coordinate = hitTestInfo.Y1HitTestPoint;
        }

        /// <summary>
        /// Gets or sets value indicates whether this is Up or Down line of the <see cref="FastBandRenderableSeries"/>
        /// </summary>
        public bool IsFirstSeries
        {
            get { return _isFirstSeries; }
            set { _isFirstSeries = value; }
        }

        /// <summary>
        /// Gets or sets the Y1 value, which is used in the <see cref="FastBandRenderableSeries"/>.
        /// </summary>
        /// <value>The value.</value>
        /// <remarks></remarks>
        public IComparable Y1Value
        {
            get
            {
                return _y1Value;
            }
            set
            {
                _y1Value = value;
                OnPropertyChanged("Y1Value");
                OnPropertyChanged("FormattedY1Value");
            }
        }

        /// <summary>
        /// Gets a formatted Y1Value.
        /// </summary>
        public string FormattedY1Value
        {
            get { return GetYCursorFormattedValue(Y1Value); }
        }

        /// <summary>
        /// Gets or sets the xy coordinate in pixels of the data-point being inspected (for <see cref="FastBandRenderableSeries"/> series)
        /// </summary>
        public Point Xy1Coordinate
        {
            get { return _xy1Coordinate; }
            set
            {
                _xy1Coordinate = value;
                OnPropertyChanged("Xy1Coordinate");
            }
        }
    }

    /// <summary>
    /// Provides a ViewModel containing info about an Xyz series
    /// </summary>
    public class XyzSeriesInfo : SeriesInfo
    {
        private IComparable _zValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ecng.Xaml.Charting.BandSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The <see cref="IRenderableSeries"/>.</param>
        /// <param name="hitTestInfo"> </param>
        public XyzSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            ZValue = hitTestInfo.ZValue;
        }

        /// <summary>
        /// Gets or sets the Z value, which is returned as hit-test result of the <see cref="XyyDataSeries{TX,TY}"/>.
        /// </summary>
        /// <value>The value</value>
        /// <remarks></remarks>
        public IComparable ZValue
        {
            get
            {
                return _zValue;
            }
            set
            {
                _zValue = value;
                OnPropertyChanged("ZValue");
            }
        }
    }

    /// <summary>
    /// Provides a ViewModel containing info about a Heatmap series
    /// </summary>
    public class HeatmapSeriesInfo : XyzSeriesInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ecng.Xaml.Charting.HeatmapSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The <see cref="IRenderableSeries"/>.</param>
        /// <param name="hitTestInfo"> </param>
        public HeatmapSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo) : base(rSeries, hitTestInfo) { }
    }

    /// <summary>
    /// Provides a ViewModel containing info about stacked Xy series
    /// </summary>
    public class XyStackedSeriesInfo : SeriesInfo
    {
        private IComparable _accumulated;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ecng.Xaml.Charting.BandSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The <see cref="IRenderableSeries"/>.</param>
        /// <param name="hitTestInfo"> </param>
        public XyStackedSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            AccumulatedValue = hitTestInfo.YValue;

            YValue = hitTestInfo.Y1Value;
            Value = YValue.ToDouble();
        }

        /// <summary>
        /// Gets or sets the accumulated value of all stacked series, which is returned as hit-test result of the stacked <see cref="XyDataSeries{TX,TY}"/>.
        /// </summary>
        /// <value>The value</value>
        /// <remarks></remarks>
        public IComparable AccumulatedValue
        {
            get
            {
                return _accumulated;
            }
            set
            {
                _accumulated = value;
                OnPropertyChanged("AccumulatedValue");
            }
        }
    }

    /// <summary>
    /// Provides a ViewModel containing info about one hundred percent stacked Xy series
    /// </summary>
    public class OneHundredPercentStackedSeriesInfo : SeriesInfo
    {
        private double _percentage;

        /// <summary>
        /// Initializes a new instance of the <see cref="Ecng.Xaml.Charting.OneHundredPercentStackedSeriesInfo" /> class.
        /// </summary>
        /// <param name="rSeries">The <see cref="IRenderableSeries"/>.</param>
        /// <param name="hitTestInfo"> </param>
        public OneHundredPercentStackedSeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : base(rSeries, hitTestInfo)
        {
            Percentage = hitTestInfo.Persentage;
        }

        /// <summary>
        /// Gets or sets the DataSeriesIndex a the hit-test site
        /// </summary>
        public double Percentage
        {
            get { return _percentage; }
            set
            {
                _percentage = value;
                OnPropertyChanged("Percentage");
            }
        }
    }

    /// <summary>
    /// <para>
    /// Provides a ViewModel containing info about a series, such as name, value, color. <see cref="SeriesInfo"/> types are
    /// produced by the <see cref="RolloverModifier"/>, <see cref="CursorModifier"/> and <see cref="LegendModifier"/>. They are
    /// consumed by the <see cref="UltrachartLegend"/> and may be consumed by a custom <see cref="ItemsControl"/> binding to collection
    /// of <see cref="SeriesInfo"/>. 
    /// </para>
    /// <para>
    /// See the examples suite, specifically RolloverModifier, SciTrader and Legends examples for more information
    /// </para>
    /// </summary>
    public class SeriesInfo : BindableObject, ICloneable
    {
        private readonly IRenderableSeries _rSeries;
        private string _seriesName;
        private IComparable _yValue;
        private IComparable _xValue;
        private Color _seriesColor;
        private DataSeriesType _dataSeriesType;
        private double _yValueDouble;
        private Point _xyCoordinate;
        private bool _isHit;
        private int _dataSeriesIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesInfo"/> class.
        /// </summary>
        /// <remarks></remarks>
        public SeriesInfo(IRenderableSeries rSeries)
        {
            _rSeries = rSeries;

            SeriesName = rSeries.DataSeries != null ? rSeries.DataSeries.SeriesName : null;
            SeriesColor = rSeries.SeriesColor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesInfo"/> class.
        /// </summary>
        /// <param name="rSeries">The <see cref="IRenderableSeries"/> that this SeriesInfo represents.</param>
        /// <param name="hitTestInfo"></param>
        public SeriesInfo(IRenderableSeries rSeries, HitTestInfo hitTestInfo)
            : this(rSeries)
        {
            DataSeriesType = hitTestInfo.DataSeriesType;
            DataSeriesIndex = hitTestInfo.DataSeriesIndex;

            IsHit = hitTestInfo.IsHit;

            XValue = hitTestInfo.XValue;
            YValue = hitTestInfo.YValue;
            Value = hitTestInfo.YValue.ToDouble();

            XyCoordinate = hitTestInfo.HitTestPoint;
        }

        /// <summary>
        /// Gets or sets whether the <see cref="IRenderableSeries"/> that this <see cref="SeriesInfo"/> represents is visible or not. 
        /// NOTE: Setting this value will show or hide the associated <see cref="IRenderableSeries"/> and may be data-bound to
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is visible; otherwise, <c>false</c>.
        /// </value>
        public bool IsVisible
        {
            get { return RenderableSeries.IsVisible; }
            set
            {
                // DO not prevent assignment if value == value. This is so that PropertyChanged gets raised
                // here for SeriesInfo after RenderableSeries.IsVisible is changed
                ////if (RenderableSeries.IsVisible == value) return;
                RenderableSeries.IsVisible = value;
                OnPropertyChanged("IsVisible");
            }
        }

        /// <summary>
        /// Gets or sets the type of the data series.
        /// </summary>
        /// <value>
        /// The type of the data series.
        /// </value>
        public DataSeriesType DataSeriesType
        {
            get { return _dataSeriesType; }
            set
            {
                _dataSeriesType = value;
                OnPropertyChanged("DataSeriesType");
            }
        }
        
        /// <summary>
        /// Gets or sets the color of the series.
        /// </summary>
        /// <value>The color of the series.</value>
        /// <remarks></remarks>
        public Color SeriesColor
        {
            get { return _seriesColor; }
            set
            {
                _seriesColor = value;
                OnPropertyChanged("SeriesColor");
            }
        }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        /// <remarks></remarks>
        public string SeriesName
        {
            get
            {
                return _seriesName;
            }
            set
            {
                _seriesName = value;
                OnPropertyChanged("SeriesName");
            }
        }

        /// <summary>
        /// Gets or sets the Y-value.
        /// </summary>
        /// <value>The value.</value>
        /// <remarks></remarks>
        public double Value
        {
            get
            {
                return _yValueDouble;
            }
            set
            {
                _yValueDouble = value;
                OnPropertyChanged("Value");
            }
        }

        /// <summary>
        /// Gets or sets the Y-value.
        /// </summary>
        /// <value>The value.</value>
        /// <remarks></remarks>
        public IComparable YValue
        {
            get
            {
                return _yValue;
            }
            set
            {
                _yValue = value;

                OnPropertyChanged("YValue");
                OnPropertyChanged("FormattedYValue");
            }
        }

        /// <summary>
        /// Gets a formatted YValue.
        /// </summary>
        public string FormattedYValue
        {
            get { return GetYCursorFormattedValue(YValue); }
        }

        /// <summary>
        /// Gets or sets the X-value.
        /// </summary>
        /// <value>The value.</value>
        /// <remarks></remarks>
        public IComparable XValue
        {
            get
            {
                return _xValue;
            }
            set
            {
                _xValue = value;

                OnPropertyChanged("XValue");
                OnPropertyChanged("FormattedXValue");
            }
        }

        /// <summary>
        /// Gets a formatted XValue.
        /// </summary>
        public string FormattedXValue
        {
            get { return GetXCursorFormattedValue(XValue); }
        }

        /// <summary>
        /// Gets or sets the xy coordinate in pixels of the data-point being inspected
        /// </summary>        
        public Point XyCoordinate
        {
            get { return _xyCoordinate; }
            set
            {
                _xyCoordinate = value;
                OnPropertyChanged("XyCoordinate");
            }
        }

        /// <summary>
        /// Get or sets whether the HitTest operation was a hit or not
        /// </summary>
        public bool IsHit
        {
            get { return _isHit; }
            set
            {
                _isHit = value;
                OnPropertyChanged("IsHit");
            }
        }

        /// <summary>
        /// Gets or sets the DataSeriesIndex a the hit-test site
        /// </summary>
        public int DataSeriesIndex
        {
            get { return _dataSeriesIndex; }
            set
            {
                _dataSeriesIndex = value;
                OnPropertyChanged("DataSeriesIndex");
            }
        }

        /// <summary>
        /// Gets the <see cref="IRenderableSeries"/> instance which this <see cref="SeriesInfo"/> wraps
        /// </summary>
        public IRenderableSeries RenderableSeries
        {
            get { return _rSeries; }
        }

        /// <summary>
        /// Returns a value formatted using Y axis format for cursors
        /// </summary>
        /// <param name="value">The value to format</param>
        protected string GetYCursorFormattedValue(IComparable value)
        {
            var result = RenderableSeries.YAxis.FormatCursorText(value);

            return result;
        }

        /// <summary>
        /// Returns a value formatted using X axis format for cursors
        /// </summary>
        /// <param name="value">The value to format</param>
        protected string GetXCursorFormattedValue(IComparable value)
        {
            var result = RenderableSeries.XAxis.FormatCursorText(value);

            return result;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public void CopyFrom(SeriesInfo other) {
            if(other.RenderableSeries != _rSeries)
                throw new InvalidOperationException("invalid series");

            SeriesName = other.SeriesName;
            YValue = other.YValue;
            XValue = other.XValue;
            SeriesColor = other.SeriesColor;
            DataSeriesType = other.DataSeriesType;
            Value = other.Value;
            XyCoordinate = other.XyCoordinate;
            IsHit = other.IsHit;
            DataSeriesIndex = other.DataSeriesIndex;
        }
    }
}