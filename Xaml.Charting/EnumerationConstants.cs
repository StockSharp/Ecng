// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// EnumerationConstants.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines constants for X or Y direction, used by <see cref="GridLinesPanel"/> to specify creation of X or Y grid line
    /// </summary>
    public enum XyDirection
    {
        /// <summary>
        /// Refers to the X-Axis Direction
        /// </summary>
        XDirection, 

        /// <summary>
        /// Refers to the Y-Axis Direction
        /// </summary>
        YDirection,

        /// <summary>
        /// Refers to both X and Y Axis Direction
        /// </summary>
        XYDirection
    }

    /// <summary>
    /// Defines constants for Pan or Zoom actions, used by <see cref="MouseWheelZoomModifier"/> to specify required action 
    /// </summary>
    public enum ActionType
    {
        /// <summary>
        /// Refers to pan action
        /// </summary>
        Pan,

        /// <summary>
        /// Refers to zoom action
        /// </summary>
        Zoom
    }

    /// <summary>
    /// Defines constants for scientific or engineering notation on <see cref="NumericAxis"/>. For instance, 
    /// using None gives default tick labels, whereas using Normalized gives a scientific notation with superscript
    /// </summary>
    public enum ScientificNotation
    {
        /// <summary>
        /// Default tick labelling, e.g. 10000
        /// </summary>
        None,

        /// <summary>
        /// Normalized (Scientific) tick labelling, e.g. 1x10^4 with superscript
        /// </summary>
        Normalized,

        /// <summary>
        /// Engineering tick labelling, e.g. 1E+4 without superscript
        /// </summary>
        E,

        /// <summary>
        /// Tick labelling with power of other bases, determined by <see cref="LogarithmicNumericAxis.LogarithmicBase"/>.
        /// E.g. 1x[base]+4 with superscript
        /// </summary>
        LogarithmicBase
    }

    /// <summary>
    /// Defines constants for behavior of the ZoomPanModifier. 
    ///  - ClipMode.None means you can pan right off the edge of the data into uncharted space. 
    ///  - ClipMode.StretchAtExtents causes a zooming (stretch) action when you reach the edge of the data. 
    ///  - ClipAtExtents forces the panning operation to stop suddenly at the extents of the data
    ///  - ClipAtMin forces the panning operation to stop suddenly at the minimum of the data, but expand at the maximum
    /// </summary>
    public enum ClipMode
    {
        /// <summary>
        /// ClipMode.None means you can pan right off the edge of the data into uncharted space. 
        /// </summary>
        None,

        /// <summary>
        /// ClipMode.StretchAtExtents causes a zooming (stretch) action when you reach the edge of the data. 
        /// </summary>
        StretchAtExtents, 

        /// <summary>
        /// ClipAtMin forces the panning operation to stop suddenly at the minimum of the data, but expand at the maximum
        /// </summary>
        ClipAtMin,

        /// <summary>
        /// ClipAtMax forces the panning operation to stop suddenly at the maximum of the data, but expand at the minimum
        /// </summary>
        ClipAtMax,

        /// <summary>
        /// ClipAtExtents forces the panning operation to stop suddenly at the extents of the data
        /// </summary>
        ClipAtExtents
    }

    /// <summary>
    /// Defines Enumeration Constants for modes of operation of the <see cref="YAxisDragModifier"/>
    /// </summary>
    public enum AxisDragModes
    {
        /// <summary>
        /// Scale mode: the dragging scales the YAxis VisibleRange property directly, or indirectly, via the GrowBy property, if YAxis.AutoRange=true
        /// </summary>
        Scale,

        /// <summary>
        /// Pan mode: the dragging pans the YAxis VisibleRange, allowing a vertical scroll. This may only be used with YAxis.AutoRange=false
        /// </summary>
        Pan,
    }
}