// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2013. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// ThemeBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

using System.Windows;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Themes
{
    public class ThemeBase : ResourceDictionary, IThemeProvider
    {
        public ThemeBase()
        {
        }

        public void InitFrom(ResourceDictionary dictionary)
        {
            // Ultrachart 
            GridBorderBrush = (Brush)dictionary["GridBorderBrush"];
            GridBackgroundBrush = (Brush)dictionary["GridBackgroundBrush"];
            UltrachartBackground = (Brush)dictionary["UltrachartBackground"];

            // Grid line and tick labels
            TickTextBrush = (Brush)dictionary["TickTextBrush"];
            MajorGridLinesBrush = (Brush)dictionary["MajorGridLineBrush"];
            MinorGridLinesBrush = (Brush)dictionary["MinorGridLineBrush"];

            // Rollover defaults
            RolloverLineStroke = (Brush)dictionary["RolloverineBrush"];
            RolloverLabelBorderBrush = (Brush)dictionary["LabelBorderBrush"];
            RolloverLabelBackgroundBrush = (Brush)dictionary["LabelBackgroundBrush"];

            // Candle / OHLC defaults
            DefaultCandleUpWickColor = (Color)dictionary["UpWickColor"];
            DefaultCandleDownWickColor = (Color) dictionary["DownWickColor"];
            DefaultCandleUpBodyColor = (Color) this["UpBodyColor"];
            DefaultCandleDownBodyColor = (Color) this["DownBodyColor"];

            // ColumnSeries defaults
            DefaultColumnOutlineColor = (Color)this["ColumnLineColor"];
            DefaultColumnFillColor = (Color) this["ColumnFillColor"];

            // LineSeries defaults
            DefaultLineSeriesColor = (Color) this["LineSeriesColor"];

            // MountainSeries defaults
            DefaultMountainAreaColor = (Color) this["MountainAreaColor"];
            DefaultMountainLineColor = (Color) this["MountainLineColor"];

            // BandSeries defaults
            DefaultDownBandFillColor = (Color)this["DownBandSeriesLineColor"];
            DefaultUpBandFillColor = (Color)this["UpBandSeriesLineColor"];
            DefaultUpBandLineColor = (Color)this["UpBandSeriesFillColor"];
            DefaultDownBandLineColor = (Color)this["DownBandSeriesFillColor"];

            // RubberBand Zoom
            RubberBandFillBrush = (Brush) this["RubberBandFillBrush"];
            RubberBandStrokeBrush = (Brush)this["RubberBandStrokeBrush"];

            // Cursor
            CursorLabelForeground = (Brush)this["LabelForegroundBrush"];
            CursorLabelBackgroundBrush = (Brush)this["LabelBorderBrush"];
            CursorLabelBorderBrush = (Brush)this["DownBandSeriesLabelBorderBrushFillColor"];
            CursorLineBrush = (Brush) this["CursorLineBrush"];

            // Overview
            OverviewFillColor = (Color) this["OverviewFillColor"];

            // Legend
            LegendBackgroundBrush = (Brush)this["LegendBackgroundBrush"];

            // TextAnnotation
            DefaultTextAnnotationBackground = (Brush)this["TextAnnotationForeground"];
            DefaultTextAnnotationForeground = (Brush)this["TextAnnotationBackground"];

            // AxisMarker
            DefaultAxisMarkerAnnotationBackground = (Brush)this["TextAnnotationBackground"];
            DefaultAxisMarkerAnnotationForeground = (Brush) this["TextAnnotationForeground"];
        }

        public Brush GridBorderBrush { get; set; }
        public Brush GridBackgroundBrush { get; set; }
        public Brush UltrachartBackground { get; set; }
        public Brush TickTextBrush { get; set; }
        public Brush MajorGridLinesBrush { get; set; }
        public Brush MinorGridLinesBrush { get; set; }
        public Brush RolloverLineStroke { get; set; }
        public Brush RolloverLabelBorderBrush { get; set; }
        public Brush RolloverLabelBackgroundBrush { get; set; }
        public Color DefaultCandleUpWickColor { get; set; }
        public Color DefaultCandleDownWickColor { get; set; }
        public Color DefaultCandleUpBodyColor { get; set; }
        public Color DefaultCandleDownBodyColor { get; set; }
        public Color DefaultColumnOutlineColor { get; set; }
        public Color DefaultColumnFillColor { get; set; }
        public Color DefaultLineSeriesColor { get; set; }
        public Color DefaultMountainLineColor { get; set; }
        public Color DefaultMountainAreaColor { get; set; }
        public Color DefaultDownBandFillColor { get; set; }
        public Color DefaultUpBandFillColor { get; set; }
        public Color DefaultUpBandLineColor { get; set; }
        public Color DefaultDownBandLineColor { get; set; }
        public Brush CursorLabelForeground { get; set; }
        public Brush CursorLabelBackgroundBrush { get; set; }
        public Brush CursorLabelBorderBrush { get; set; }
        public Brush CursorLineBrush { get; set; }

        public Brush RubberBandFillBrush { get; set; }
        public Brush RubberBandStrokeBrush { get; set; }
        
        public Color OverviewFillColor { get; set; }
        public Brush LegendBackgroundBrush { get; set; }
        public Brush DefaultTextAnnotationBackground { get; set; }
        public Brush DefaultTextAnnotationForeground { get; set; }
        public Brush DefaultAxisMarkerAnnotationBackground { get; set; }
        public Brush DefaultAxisMarkerAnnotationForeground { get; set; }

        public void ApplyTheme(IThemeProvider newTheme)
        {
            throw new System.NotImplementedException();
        }
    }
}
