// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IGridLinesPanel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Shapes;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines the interface to the <see cref="GridLinesPanel"/>, a canvas control which displays grid lines behind the <see cref="UltrachartSurface"/>
    /// </summary>
    public interface IGridLinesPanel
    {
        /// <summary>
        /// Clears the grid lines in a specific direction
        /// </summary>
        /// <param name="xyDirection">The <see cref="XyDirection"/> to clear in, e.g. <see cref="XyDirection.XDirection"/> clears the X-Axis gridlines</param>
        void Clear(XyDirection xyDirection);

        /// <summary>
        /// Adds a line to the panel in the specific <see cref="XyDirection"/>
        /// </summary>
        /// <param name="xyDirection">The <see cref="XyDirection"/> to clear in, e.g. <see cref="XyDirection.XDirection"/> adds an X-Axis gridline</param>
        /// <param name="line">The line to add</param>
        void AddLine(XyDirection xyDirection, Line line);

        /// <summary>
        /// Gets the width of the panel in pixels
        /// </summary>
        int Width { get; }

        /// <summary>
        /// Gets the height of the panel in pixels
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets the thickness of any border applied to the panel
        /// </summary>
        Thickness BorderThickness { get; }

        /// <summary>
        /// Generates and adds a <see cref="Line"/> element to the <see cref="GridLinesPanel"/>. Applies the direction and style to the line as 
        /// well as Id so they may be re-used (pooled)
        /// </summary>
        /// <param name="lineId">The line Id</param>
        /// <param name="xyDirection">The direction, X or Y</param>
        /// <param name="lineStyle">The style to apply to the line</param>
        /// <returns>The <see cref="Line"/> instance, which has been added to the <see cref="GridLinesPanel"/></returns>
        Line GenerateElement(int lineId, XyDirection xyDirection, Style lineStyle);

        /// <summary>
        /// Removes all <see cref="Line"/> instances after the specified index. This method is used when re-drawing the <see cref="UltrachartSurface"/> 
        /// when the number of lines has reduced from one redraw to the next. 
        /// </summary>
        /// <param name="xyDirection">The direction to clear, X or Y</param>
        /// <param name="index">The index to remove after (inclusive)</param>
        void RemoveElementsAfter(XyDirection xyDirection, int index);
    }
}