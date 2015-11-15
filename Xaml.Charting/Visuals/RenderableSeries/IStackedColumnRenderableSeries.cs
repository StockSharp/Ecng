// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// StackedColumnRenderableSeries.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

using System.Windows.Media;
using Ecng.Xaml.Charting.Model.DataSeries;
using Ecng.Xaml.Charting.Numerics.CoordinateCalculators;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Defines the interface to <see cref="StackedColumnRenderableSeries"/>
    /// </summary>
    public interface IStackedColumnRenderableSeries : IStackedRenderableSeries
    {
        /// <summary>
        /// Gets the instance of <see cref="IStackedColumnsWrapper"/> used internally for
        /// stacked series composition and rendering.
        /// </summary>
        IStackedColumnsWrapper Wrapper { get; }

        /// <summary>
        /// Gets or sets the value which specifies the width of the gap between horizontally stacked columns. 
        /// Can be set to either a relative or absolute value depending on the <see cref="SpacingMode"/> used.
        /// </summary>
        double Spacing { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SpacingMode"/> to use for the space between columns computations.
        /// E.g. the default of Absolute requires that <see cref="Spacing"/> is in pixels. The value
        /// of Relative requires that <see cref="Spacing"/> is a double value from 0.0 to 1.0.
        /// </summary>
        SpacingMode SpacingMode { get; set; }

        /// <summary>
        /// Gets or sets the value between 0.0 and 1.0 indicating how much of the space available every column occupies.
        /// </summary>
        double DataPointWidth { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether to show text labels over the columns.
        /// </summary>
        bool ShowLabel { get; set; }

        /// <summary>
        /// Gets or sets the foreground color for text labels.
        /// </summary>
        Color LabelColor{ get; set; }

        /// <summary>
        /// Gets or sets the font size for text labels.
        /// </summary>
        float LabelFontSize{ get; set; }

        /// <summary>
        /// Gets or sets the formatting string for text labels.
        /// </summary>
        string LabelTextFormatting{ get; set; }

        /// <summary>
        /// Calculates the space available per a data-point.
        /// </summary>
        /// <param name="xCoordinateCalculator">The current x coordinate calculator instance</param>
        /// <param name="pointSeries">The current <see cref="IPointSeries" /> being rendered.</param>
        /// <param name="barsAmount">Amount of bars within the viewport</param>
        /// <param name="widthFraction">The width fraction from 0.0 to 1.0, where 0.0 is infinitey small, 0.5 takes up half the available width and 1.0 means a data-point is the full width between points</param>
        int GetDatapointWidth(ICoordinateCalculator<double> xCoordinateCalculator, IPointSeries pointSeries, double barsAmount, double widthFraction);

        bool IsValidForDrawing { get; }
    }
}