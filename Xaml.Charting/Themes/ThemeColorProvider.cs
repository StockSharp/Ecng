// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ThemeColorProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.AttachedProperties;
using Ecng.Xaml.Charting.Common.Helpers;
using Ecng.Xaml.Charting;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// Defines the interface to a Ultrachart Theme, which provides Brushes and Colors for the XAML control templates.
    /// You may implement IThemeProvider yourself and pass to ThemeManager to set the global theme for all <see cref="UltrachartSurface"/>
    /// controls.
    /// </summary>
    /// <seealso cref="ThemeManager"/>
    /// <seealso cref="ThemeColorProvider"/>
    public interface IThemeProvider
    {
        /// <summary>
        /// Gets or sets the brush used for Gridlines area border
        /// </summary>
        Brush GridBorderBrush { get; set; }

        /// <summary>
        /// Gets or sets the background of the Gridlines area
        /// </summary>
        Brush GridBackgroundBrush { get; set; }

        /// <summary>
        /// Gets or sets the background of the entire <see cref="UltrachartSurface"/>
        /// </summary>
        Brush UltrachartBackground { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <see cref="AxisBase"/> tick labels
        /// </summary>
        Brush TickTextBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <see cref="AxisBase"/> Major Grid lines. Expects a <see cref="SolidColorBrush"/>
        /// </summary>
        Brush MajorGridLinesBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <see cref="AxisBase"/> Minor Grid lines. Expects a <see cref="SolidColorBrush"/>
        /// </summary>
        Brush MinorGridLinesBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RolloverModifier"/> vertical line
        /// </summary>
        Brush RolloverLineStroke { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RolloverModifier"/> label border
        /// </summary>
        Brush RolloverLabelBorderBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RolloverModifier"/> label background
        /// </summary>
        Brush RolloverLabelBackgroundBrush { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastCandlestickRenderableSeries.UpWickColor"/>
        /// </summary>
        Color DefaultCandleUpWickColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastCandlestickRenderableSeries.DownWickColor"/>
        /// </summary>
        Color DefaultCandleDownWickColor { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="FastCandlestickRenderableSeries.UpBodyBrush"/>. 
        /// Accepts <see cref="SolidColorBrush"/> and <see cref="LinearGradientBrush"/>
        /// </summary>
        Brush DefaultCandleUpBodyBrush { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="FastCandlestickRenderableSeries.DownBodyBrush"/>. 
        /// Accepts <see cref="SolidColorBrush"/> and <see cref="LinearGradientBrush"/>
        /// </summary>
        Brush DefaultCandleDownBodyBrush { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor"/>, which is used
        /// to style the column outline. 
        /// </summary>
        Color DefaultColumnOutlineColor { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="BaseColumnRenderableSeries.FillBrush"/>. 
        /// Accepts <see cref="SolidColorBrush"/> and <see cref="LinearGradientBrush"/>
        /// </summary>
        Brush DefaultColumnFillBrush { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor"/>. 
        /// which is used to define the line color
        /// </summary>
        Color DefaultLineSeriesColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor"/>. 
        /// which defines the mountain line color
        /// </summary>
        Color DefaultMountainLineColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastBandRenderableSeries.BandDownColor"/>
        /// </summary>
        Color DefaultDownBandFillColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastBandRenderableSeries.BandUpColor"/>
        /// </summary>
        Color DefaultUpBandFillColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor"/>, 
        /// which defines the up band line color
        /// </summary>
        Color DefaultUpBandLineColor { get; set; }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastBandRenderableSeries.Series1Color"/>, 
        /// which defines the down band line color
        /// </summary>
        Color DefaultDownBandLineColor { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="BaseMountainRenderableSeries.AreaBrush"/>. 
        /// Accepts <seealso cref="SolidColorBrush"/> and <seealso cref="LinearGradientBrush"/>
        /// </summary>
        Brush DefaultMountainAreaBrush { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="FastHeatMapRenderableSeries.ColorMap"/>. 
        /// Accepts <seealso cref="LinearGradientBrush"/>. Gradient Stops are used to compute colors of the final heat signature
        /// </summary>
        Brush DefaultColorMapBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="CursorModifier"/> label text foreground
        /// </summary>
        Brush CursorLabelForeground { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="CursorModifier"/> label background
        /// </summary>
        Brush CursorLabelBackgroundBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="CursorModifier"/> label border
        /// </summary>
        Brush CursorLabelBorderBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="RubberBandXyZoomModifier"/> drag reticule fill
        /// </summary>
        Brush RubberBandFillBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="RubberBandXyZoomModifier"/> drag reticule border
        /// </summary>
        Brush RubberBandStrokeBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="CursorModifier"/> line stroke
        /// </summary>
        Brush CursorLineBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush (fill) for the <seealso cref="UltrachartOverview"/> non-selected area
        /// </summary>
        Brush OverviewFillBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush (fill) for the <see cref="UltrachartScrollbar"/> viewport area
        /// </summary>
        Brush ScrollbarFillBrush { get; set; }

        /// <summary>
        /// Gets or sets the brush for the <seealso cref="UltrachartLegend"/> background
        /// </summary>
        Brush LegendBackgroundBrush { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <seealso cref="TextAnnotation"/> background
        /// </summary>
        Brush DefaultTextAnnotationBackground { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <seealso cref="TextAnnotation"/> text foreground
        /// </summary>
        Brush DefaultTextAnnotationForeground { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <seealso cref="AxisMarkerAnnotation"/> background
        /// </summary>
        Brush DefaultAxisMarkerAnnotationBackground { get; set; }

        /// <summary>
        /// Gets or sets the default brush for the <seealso cref="AxisMarkerAnnotation"/> text-foreground
        /// </summary>
        Brush DefaultAxisMarkerAnnotationForeground { get; set; }

        /// <summary>
        /// Gets or sets the color for the <seealso cref="AxisBase"/> axis bands fill
        /// </summary>
        Color AxisBandsFill { get; set; }

        /// <summary>
        /// Gets or sets the color for the <seealso cref="BoxVolumeRenderableSeries.Timeframe2Color"/>.
        /// </summary>
        Color BoxVolumeTimeframe2Color {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="BoxVolumeRenderableSeries.Timeframe2FrameColor"/>.
        /// </summary>
        Color BoxVolumeTimeframe2FrameColor {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="BoxVolumeRenderableSeries.Timeframe3Color"/>.
        /// </summary>
        Color BoxVolumeTimeframe3Color {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="BoxVolumeRenderableSeries.CellFontColor"/>.
        /// </summary>
        Color BoxVolumeCellFontColor {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="BoxVolumeRenderableSeries.HighVolColor"/>.
        /// </summary>
        Color BoxVolumeHighVolColor {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="ClusterProfileRenderableSeries.LineColor"/>.
        /// </summary>
        Color ClusterProfileLineColor {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="ClusterProfileRenderableSeries.TextColor"/>.
        /// </summary>
        Color ClusterProfileTextColor {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="ClusterProfileRenderableSeries.ClusterColor"/>.
        /// </summary>
        Color ClusterProfileClusterColor {get; set;}

        /// <summary>
        /// Gets or sets the color for the <seealso cref="ClusterProfileRenderableSeries.ClusterMaxColor"/>.
        /// </summary>
        Color ClusterProfileClusterMaxColor {get; set;}

        /// <summary>
        /// Applies a <see cref="IThemeProvider"/> instance to this instance, by overwriting all properties and raising 
        /// <see cref="INotifyPropertyChanged"/> where necessary
        /// </summary>
        /// <param name="newTheme">The new theme instance</param>
        void ApplyTheme(IThemeProvider newTheme);

        /// <summary>
        /// Applies a <see cref="ResourceDictionary"/> to this <see cref="IThemeProvider"/> instance, by searching for the resources
        /// with the same keys (Names) as the property names in the <see cref="IThemeProvider"/> instance
        /// </summary>
        /// <param name="dictionary">The <seealso cref="ResourceDictionary"/> source</param>
        void ApplyTheme(ResourceDictionary dictionary);
    }

    /// <summary>
    /// Defines a Ultrachart Theme color provider, which provides Brushes and Colors for the XAML control templates.
    /// You may implement IThemeProvider yourself and pass to ThemeManager to set the global theme for all <see cref="UltrachartSurface"/>
    /// controls.
    /// </summary>
    /// <seealso cref="ThemeManager"/>
    /// <seealso cref="ThemeColorProvider"/>
    /// <seealso cref="IThemeProvider"/>
    public class ThemeColorProvider : BindableObject, IThemeProvider
    {
        private Brush _axisMarkerAnnotationBackground;
        private Brush _axisMarkerAnnotationForeground;
        private Brush _cursorLabelBackground;
        private Brush _cursorLabelBorder;
        private Brush _cursorLabelForeground;
        private Brush _cursorLineBrush;
        private Brush _defaultColumnFillBrush;
        private Color _defaultColumnOutlineColor;
        private Color _defaultLineSeriesColor;
        private Color _downBandFillColor;
        private Color _downBandLineColor;
        private Brush _downBodyBrush;
        private Color _downWickColor;
        private Brush _gridBackgroundBrush;
        private Brush _gridBorderBrush;
        private Brush _legendBackgroundBrush;
        private Brush _majorGridLinesBrush;
        private Brush _minorGridLinesBrush;
        private Brush _mountainAreaBrush;
        private Color _mountainLineColor;
        private Brush _defaultColorMapBrush;
        private Brush _overviewFillBrush;
        private Brush _rolloverLabelBackgroundBrush;
        private Brush _rolloverLabelBorderBrush;
        private Brush _rubberBandFill;
        private Brush _rubberBandStroke;
        private Brush _ultraChartBackground;
        private Brush _textAnnotationBackground;
        private Brush _textAnnotationForeground;
        private Brush _tickTextBrush;
        private Color _upBandFillColor;
        private Color _upBandLineColor;
        private Brush _upBodyBrush;
        private Color _upWickColor;
        private Color _axisBandsFill;
        private Brush _rolloverLineStroke;
        private Brush _scrollbarFillBrush;
        private Color _boxVolumeTimeframe2Color;
        private Color _boxVolumeTimeframe2FrameColor;
        private Color _boxVolumeTimeframe3Color;
        private Color _boxVolumeCellFontColor;
        private Color _boxVolumeHighVolColor;
        private Color _clusterProfileLineColor;
        private Color _clusterProfileTextColor;
        private Color _clusterProfileClusterColor;
        private Color _clusterProfileClusterMaxColor;

        /// <summary>
        /// Applies a <see cref="IThemeProvider" /> instance to this instance, by overwriting all properties and raising
        /// <see cref="INotifyPropertyChanged" /> where necessary
        /// </summary>
        /// <param name="newTheme">The new theme instance</param>
        public void ApplyTheme(IThemeProvider newTheme)
        {
            InterfaceHelpers.CopyInterfaceProperties(newTheme, this);
        }

        /// <summary>
        /// Applies a <see cref="ResourceDictionary" /> to this <see cref="IThemeProvider" /> instance, by searching for the resources
        /// with the same keys (Names) as the property names in the <see cref="IThemeProvider" /> instance
        /// </summary>
        /// <param name="dictionary">The <see cref="ResourceDictionary" /> source</param>
        public void ApplyTheme(ResourceDictionary dictionary)
        {
            // Ensure all resources frozen
            foreach (var resource in dictionary)
            {
                var dp = ((DictionaryEntry)resource).Value as DependencyObject;
                if (dp == null
#if !SILVERLIGHT
                    || dp.IsSealed
#endif
                    )
                    continue;

                FreezeHelper.SetFreeze(dp, true);
            }

            // Ultrachart 
            GridBorderBrush = (Brush)dictionary["GridBorderBrush"];
            GridBackgroundBrush = (Brush)dictionary["GridBackgroundBrush"];
            UltrachartBackground = (Brush)dictionary["UltrachartBackground"];

            // Grid line and tick labels
            TickTextBrush = (Brush)dictionary["TickTextBrush"];
            MajorGridLinesBrush = (Brush)dictionary["MajorGridLineBrush"];
            MinorGridLinesBrush = (Brush)dictionary["MinorGridLineBrush"];

            // Rollover defaults
            RolloverLineStroke = (Brush)dictionary["RolloverLineBrush"];
            RolloverLabelBorderBrush = (Brush)dictionary["LabelBorderBrush"];
            RolloverLabelBackgroundBrush = (Brush)dictionary["LabelBackgroundBrush"];

            // Candle / OHLC defaults
            DefaultCandleUpWickColor = (Color)dictionary["UpWickColor"];
            DefaultCandleDownWickColor = (Color)dictionary["DownWickColor"];
            DefaultCandleUpBodyBrush = (Brush)dictionary["UpBodyBrush"];
            DefaultCandleDownBodyBrush = (Brush)dictionary["DownBodyBrush"];

            // ColumnSeries defaults
            DefaultColumnOutlineColor = (Color)dictionary["ColumnLineColor"];
            DefaultColumnFillBrush = (Brush)dictionary["ColumnFillBrush"];

            // LineSeries defaults
            DefaultLineSeriesColor = (Color)dictionary["LineSeriesColor"];

            // MountainSeries defaults
            DefaultMountainAreaBrush = (Brush)dictionary["MountainAreaBrush"];
            DefaultMountainLineColor = (Color)dictionary["MountainLineColor"];

            // Heatmap defaults
            DefaultColorMapBrush = (Brush) dictionary["DefaultColorMapBrush"];

            // BandSeries defaults
            DefaultDownBandFillColor = (Color)dictionary["DownBandSeriesLineColor"];
            DefaultUpBandFillColor = (Color)dictionary["UpBandSeriesLineColor"];
            DefaultUpBandLineColor = (Color)dictionary["UpBandSeriesFillColor"];
            DefaultDownBandLineColor = (Color)dictionary["DownBandSeriesFillColor"];

            // RubberBand Zoom
            RubberBandFillBrush = (Brush)dictionary["RubberBandFillBrush"];
            RubberBandStrokeBrush = (Brush)dictionary["RubberBandStrokeBrush"];

            // Cursor
            CursorLabelForeground = (Brush)dictionary["LabelForegroundBrush"];
            CursorLabelBackgroundBrush = (Brush)dictionary["LabelBackgroundBrush"];
            CursorLabelBorderBrush = (Brush)dictionary["LabelBorderBrush"];
            CursorLineBrush = (Brush)dictionary["CursorLineBrush"];

            // Overview
            OverviewFillBrush = (Brush)dictionary["OverviewFillBrush"];

            // Scrollbar
            ScrollbarFillBrush = (Brush)dictionary["ScrollbarFillBrush"];

            // Legend
            LegendBackgroundBrush = (Brush)dictionary["LegendBackgroundBrush"];

            // TextAnnotation
            DefaultTextAnnotationBackground = (Brush)dictionary["TextAnnotationBackground"];
            DefaultTextAnnotationForeground = (Brush)dictionary["TextAnnotationForeground"];

            // AxisMarker
            DefaultAxisMarkerAnnotationBackground = (Brush)dictionary["TextAnnotationBackground"];
            DefaultAxisMarkerAnnotationForeground = (Brush)dictionary["TextAnnotationForeground"];

            // Axis Bands
            AxisBandsFill = (Color)dictionary["AxisBandsFill"];

            // BoxVolume chart
            BoxVolumeTimeframe2Color = (Color)dictionary["BoxVolumeTimeframe2Color"];
            BoxVolumeTimeframe2FrameColor = (Color)dictionary["BoxVolumeTimeframe2FrameColor"];
            BoxVolumeTimeframe3Color = (Color)dictionary["BoxVolumeTimeframe3Color"];
            BoxVolumeCellFontColor = (Color)dictionary["BoxVolumeCellFontColor"];
            BoxVolumeHighVolColor = (Color)dictionary["BoxVolumeHighVolColor"];

            // Cluster profile chart
            ClusterProfileLineColor = (Color)dictionary["ClusterProfileLineColor"];
            ClusterProfileTextColor = (Color)dictionary["ClusterProfileTextColor"];
            ClusterProfileClusterColor = (Color)dictionary["ClusterProfileClusterColor"];
            ClusterProfileClusterMaxColor = (Color)dictionary["ClusterProfileClusterMaxColor"];
        }

        /// <summary>
        /// Gets or sets the brush used for Gridlines area border
        /// </summary>
        public Brush GridBorderBrush
        {
            get { return _gridBorderBrush; }
            set
            {
                if (_gridBorderBrush != value)
                {
                    _gridBorderBrush = value;
                    OnPropertyChanged("GridBorderBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the background of the Gridlines area
        /// </summary>
        public Brush GridBackgroundBrush
        {
            get { return _gridBackgroundBrush; }
            set
            {
                if (_gridBackgroundBrush != value)
                {
                    _gridBackgroundBrush = value;
                    OnPropertyChanged("GridBackgroundBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the background of the entire <see cref="UltrachartSurface" />
        /// </summary>
        public Brush UltrachartBackground
        {
            get { return _ultraChartBackground; }
            set
            {
                if (_ultraChartBackground != value)
                {
                    _ultraChartBackground = value;
                    OnPropertyChanged("UltrachartBackground");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="AxisBase" /> tick labels
        /// </summary>
        public Brush TickTextBrush
        {
            get { return _tickTextBrush; }
            set
            {
                if (_tickTextBrush != value)
                {
                    _tickTextBrush = value;
                    OnPropertyChanged("TickTextBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="AxisBase" /> Major Grid lines. Expects a <see cref="SolidColorBrush" />
        /// </summary>
        public Brush MajorGridLinesBrush
        {
            get { return _majorGridLinesBrush; }
            set
            {
                if (_majorGridLinesBrush != value)
                {
                    _majorGridLinesBrush = value;
                    OnPropertyChanged("MajorGridLinesBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="AxisBase" /> Minor Grid lines. Expects a <see cref="SolidColorBrush" />
        /// </summary>
        public Brush MinorGridLinesBrush
        {
            get { return _minorGridLinesBrush; }
            set
            {
                if (_minorGridLinesBrush != value)
                {
                    _minorGridLinesBrush = value;
                    OnPropertyChanged("MinorGridLinesBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RolloverModifier" /> vertical line
        /// </summary>
        public Brush RolloverLineStroke
        {
            get { return _rolloverLineStroke; }
            set
            {
                if (_rolloverLineStroke != value)
                {
                    _rolloverLineStroke = value;
                    OnPropertyChanged("RolloverLineStroke");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RolloverModifier" /> label border
        /// </summary>
        public Brush RolloverLabelBorderBrush
        {
            get { return _rolloverLabelBorderBrush; }
            set
            {
                if (_rolloverLabelBorderBrush != value)
                {
                    _rolloverLabelBorderBrush = value;
                    OnPropertyChanged("RolloverLabelBorderBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RolloverModifier" /> label background
        /// </summary>
        public Brush RolloverLabelBackgroundBrush
        {
            get { return _rolloverLabelBackgroundBrush; }
            set
            {
                if (_rolloverLabelBackgroundBrush != value)
                {
                    _rolloverLabelBackgroundBrush = value;
                    OnPropertyChanged("RolloverLabelBackgroundBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastCandlestickRenderableSeries.UpWickColor" />
        /// </summary>
        public Color DefaultCandleUpWickColor
        {
            get { return _upWickColor; }
            set
            {
                if (_upWickColor != value)
                {
                    _upWickColor = value;
                    OnPropertyChanged("DefaultCandleUpWickColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastCandlestickRenderableSeries.DownWickColor" />
        /// </summary>
        public Color DefaultCandleDownWickColor
        {
            get { return _downWickColor; }
            set
            {
                if (_downWickColor != value)
                {
                    _downWickColor = value;
                    OnPropertyChanged("DefaultCandleDownWickColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="FastCandlestickRenderableSeries.UpBodyBrush" />.
        /// Accepts <see cref="SolidColorBrush" /> and <see cref="LinearGradientBrush" />
        /// </summary>
        public Brush DefaultCandleUpBodyBrush
        {
            get { return _upBodyBrush; }
            set
            {
                if (_upBodyBrush != value)
                {
                    _upBodyBrush = value;
                    OnPropertyChanged("DefaultCandleUpBodyBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="FastCandlestickRenderableSeries.DownBodyBrush" />.
        /// Accepts <see cref="SolidColorBrush" /> and <see cref="LinearGradientBrush" />
        /// </summary>
        public Brush DefaultCandleDownBodyBrush
        {
            get { return _downBodyBrush; }
            set
            {
                if (_downBodyBrush != value)
                {
                    _downBodyBrush = value;
                    OnPropertyChanged("DefaultCandleDownBodyBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor" />, which is used
        /// to style the column outline.
        /// </summary>
        public Color DefaultColumnOutlineColor
        {
            get { return _defaultColumnOutlineColor; }
            set
            {
                if (_defaultColumnOutlineColor != value)
                {
                    _defaultColumnOutlineColor = value;
                    OnPropertyChanged("DefaultColumnOutlineColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="BaseColumnRenderableSeries.FillBrush" />.
        /// Accepts <see cref="SolidColorBrush" /> and <see cref="LinearGradientBrush" />
        /// </summary>
        public Brush DefaultColumnFillBrush
        {
            get { return _defaultColumnFillBrush; }
            set
            {
                if (_defaultColumnFillBrush != value)
                {
                    _defaultColumnFillBrush = value;
                    OnPropertyChanged("DefaultColumnFillBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor" />.
        /// which is used to define the line color
        /// </summary>
        public Color DefaultLineSeriesColor
        {
            get { return _defaultLineSeriesColor; }
            set
            {
                if (_defaultLineSeriesColor != value)
                {
                    _defaultLineSeriesColor = value;
                    OnPropertyChanged("DefaultLineSeriesColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor" />.
        /// which defines the mountain line color
        /// </summary>
        public Color DefaultMountainLineColor
        {
            get { return _mountainLineColor; }
            set
            {
                if (_mountainLineColor != value)
                {
                    _mountainLineColor = value;
                    OnPropertyChanged("DefaultMountainLineColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="BaseMountainRenderableSeries.AreaBrush" />.
        /// Accepts <see cref="SolidColorBrush" /> and <see cref="LinearGradientBrush" />
        /// </summary>
        public Brush DefaultMountainAreaBrush
        {
            get { return _mountainAreaBrush; }
            set
            {
                if (_mountainAreaBrush != value)
                {
                    _mountainAreaBrush = value;
                    OnPropertyChanged("DefaultMountainAreaBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the 
        /// <see cref="FastHeatMapRenderableSeries.ColorMap" />. 
        /// Accepts 
        /// <seealso cref="LinearGradientBrush" />. Gradient Stops are used to compute colors of the final heat signature
        /// </summary>
        public Brush DefaultColorMapBrush
        {
            get { return _defaultColorMapBrush; }
            set
            {
                if (_defaultColorMapBrush != value)
                {
                    _defaultColorMapBrush = value;
                    OnPropertyChanged("DefaultColorMapBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastBandRenderableSeries.BandDownColor" />
        /// </summary>
        public Color DefaultDownBandFillColor
        {
            get { return _downBandFillColor; }
            set
            {
                if (_downBandFillColor != value)
                {
                    _downBandFillColor = value;
                    OnPropertyChanged("DefaultDownBandFillColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastBandRenderableSeries.BandUpColor" />
        /// </summary>
        public Color DefaultUpBandFillColor
        {
            get { return _upBandFillColor; }
            set
            {
                if (_upBandFillColor != value)
                {
                    _upBandFillColor = value;
                    OnPropertyChanged("DefaultUpBandFillColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="BaseRenderableSeries.SeriesColor" />,
        /// which defines the up band line color
        /// </summary>
        public Color DefaultUpBandLineColor
        {
            get { return _upBandLineColor; }
            set
            {
                if (_upBandLineColor != value)
                {
                    _upBandLineColor = value;
                    OnPropertyChanged("DefaultUpBandLineColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default color for the <see cref="FastBandRenderableSeries.Series1Color" />,
        /// which defines the down band line color
        /// </summary>
        public Color DefaultDownBandLineColor
        {
            get { return _downBandLineColor; }
            set
            {
                if (_downBandLineColor != value)
                {
                    _downBandLineColor = value;
                    OnPropertyChanged("DefaultDownBandLineColor");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="CursorModifier" /> label text foreground
        /// </summary>
        public Brush CursorLabelForeground
        {
            get { return _cursorLabelForeground; }
            set
            {
                if (_cursorLabelForeground != value)
                {
                    _cursorLabelForeground = value;
                    OnPropertyChanged("CursorLabelForeground");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="CursorModifier" /> label background
        /// </summary>
        public Brush CursorLabelBackgroundBrush
        {
            get { return _cursorLabelBackground; }
            set
            {
                if (_cursorLabelBackground != value)
                {
                    _cursorLabelBackground = value;
                    OnPropertyChanged("CursorLabelBackgroundBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="CursorModifier" /> label border
        /// </summary>
        public Brush CursorLabelBorderBrush
        {
            get { return _cursorLabelBorder; }
            set
            {
                if (_cursorLabelBorder != value)
                {
                    _cursorLabelBorder = value;
                    OnPropertyChanged("CursorLabelBorderBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RubberBandXyZoomModifier" /> drag reticule fill
        /// </summary>
        public Brush RubberBandFillBrush
        {
            get { return _rubberBandFill; }
            set
            {
                if (_rubberBandFill != value)
                {
                    _rubberBandFill = value;
                    OnPropertyChanged("RubberBandFillBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="RubberBandXyZoomModifier" /> drag reticule border
        /// </summary>
        public Brush RubberBandStrokeBrush
        {
            get { return _rubberBandStroke; }
            set
            {
                if (_rubberBandStroke != value)
                {
                    _rubberBandStroke = value;
                    OnPropertyChanged("RubberBandStrokeBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="CursorModifier" /> line stroke
        /// </summary>
        public Brush CursorLineBrush
        {
            get { return _cursorLineBrush; }
            set
            {
                if (_cursorLineBrush != value)
                {
                    _cursorLineBrush = value;
                    OnPropertyChanged("CursorLineBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush (fill) for the <see cref="UltrachartOverview" /> non-selected area
        /// </summary>
        public Brush OverviewFillBrush
        {
            get { return _overviewFillBrush; }
            set
            {
                if (_overviewFillBrush != value)
                {
                    _overviewFillBrush = value;
                    OnPropertyChanged("OverviewFillBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush (fill) for the <see cref="UltrachartScrollbar"/> viewport area
        /// </summary>
        public Brush ScrollbarFillBrush
        {
            get { return _scrollbarFillBrush; }
            set
            {
                if (_scrollbarFillBrush != value)
                {
                    _scrollbarFillBrush = value;
                    OnPropertyChanged("ScrollbarFillBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the brush for the <see cref="UltrachartLegend" /> background
        /// </summary>
        public Brush LegendBackgroundBrush
        {
            get { return _legendBackgroundBrush; }
            set
            {
                if (_legendBackgroundBrush != value)
                {
                    _legendBackgroundBrush = value;
                    OnPropertyChanged("LegendBackgroundBrush");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="TextAnnotation" /> background
        /// </summary>
        public Brush DefaultTextAnnotationBackground
        {
            get { return _textAnnotationBackground; }
            set
            {
                if (_textAnnotationBackground != value)
                {
                    _textAnnotationBackground = value;
                    OnPropertyChanged("DefaultTextAnnotationBackground");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="TextAnnotation" /> text foreground
        /// </summary>
        public Brush DefaultTextAnnotationForeground
        {
            get { return _textAnnotationForeground; }
            set
            {
                if (_textAnnotationForeground != value)
                {
                    _textAnnotationForeground = value;
                    OnPropertyChanged("DefaultTextAnnotationForeground");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="AxisMarkerAnnotation" /> background
        /// </summary>
        public Brush DefaultAxisMarkerAnnotationBackground
        {
            get { return _axisMarkerAnnotationBackground; }
            set
            {
                if (_axisMarkerAnnotationBackground != value)
                {
                    _axisMarkerAnnotationBackground = value;
                    OnPropertyChanged("DefaultAxisMarkerAnnotationBackground");
                }
            }
        }

        /// <summary>
        /// Gets or sets the default brush for the <see cref="AxisMarkerAnnotation" /> text-foreground
        /// </summary>
        public Brush DefaultAxisMarkerAnnotationForeground
        {
            get { return _axisMarkerAnnotationForeground; }
            set
            {
                if (_axisMarkerAnnotationForeground != value)
                {
                    _axisMarkerAnnotationForeground = value;
                    OnPropertyChanged("DefaultAxisMarkerAnnotationForeground");
                }
            }
        }

        /// <summary>
        /// Gets or sets the color for the <see cref="AxisBase" /> axis bands fill
        /// </summary>
        public Color AxisBandsFill
        {
            get { return _axisBandsFill; }
            set
            {
                if (_axisBandsFill != value)
                {
                    _axisBandsFill = value;
                    OnPropertyChanged("AxisBandsFill");
                }
            }
        }

        public Color BoxVolumeTimeframe2Color {
            get {return _boxVolumeTimeframe2Color;}
            set {SetField(ref _boxVolumeTimeframe2Color, value, nameof(BoxVolumeTimeframe2Color));}
        }

        public Color BoxVolumeTimeframe2FrameColor {
            get {return _boxVolumeTimeframe2FrameColor;}
            set {SetField(ref _boxVolumeTimeframe2FrameColor, value, nameof(BoxVolumeTimeframe2FrameColor));}
        }

        public Color BoxVolumeTimeframe3Color {
            get {return _boxVolumeTimeframe3Color;}
            set {SetField(ref _boxVolumeTimeframe3Color, value, nameof(BoxVolumeTimeframe3Color));}
        }

        public Color BoxVolumeCellFontColor {
            get {return _boxVolumeCellFontColor;}
            set {SetField(ref _boxVolumeCellFontColor, value, nameof(BoxVolumeCellFontColor));}
        }

        public Color BoxVolumeHighVolColor {
            get {return _boxVolumeHighVolColor;}
            set {SetField(ref _boxVolumeHighVolColor, value, nameof(BoxVolumeHighVolColor));}
        }

        public Color ClusterProfileLineColor {
            get {return _clusterProfileLineColor;}
            set {SetField(ref _clusterProfileLineColor, value, nameof(ClusterProfileLineColor));}
        }

        public Color ClusterProfileTextColor {
            get {return _clusterProfileTextColor;}
            set {SetField(ref _clusterProfileTextColor, value, nameof(ClusterProfileTextColor));}
        }

        public Color ClusterProfileClusterColor {
            get {return _clusterProfileClusterColor;}
            set {SetField(ref _clusterProfileClusterColor, value, nameof(ClusterProfileClusterColor));}
        }

        public Color ClusterProfileClusterMaxColor {
            get {return _clusterProfileClusterMaxColor;}
            set {SetField(ref _clusterProfileClusterMaxColor, value, nameof(ClusterProfileClusterMaxColor));}
        }
    }
}