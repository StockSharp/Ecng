// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// GridLinesPanel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Rendering.Common;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a panel to draw the background behind a UltrachartSurface gridlines area only. Since v2.0 this panel no longer draws gridlines, these are deferred to the <see cref="RenderSurfaceBase"/>
    /// </summary>    
    public class GridLinesPanel : ContentControl, IGridLinesPanel
    {
        private IEventAggregator _eventAggregator;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridLinesPanel"/> class.
        /// </summary>
        /// <remarks></remarks>
        public GridLinesPanel()
        {            
            DefaultStyleKey = typeof (GridLinesPanel);

            SizeChanged += GridLinesPanelSizeChanged;
        }

        /// <summary>
        /// Sets the event aggregator instance used throughout Ultrachart
        /// </summary>
        /// <value>The event aggregator instance.</value>
        /// <remarks></remarks>
        public IEventAggregator EventAggregator { set { _eventAggregator = value; } }

        /// <summary>
        /// Clears the grid lines in a specific direction
        /// </summary>
        /// <param name="xyDirection">The <see cref="XyDirection"/> to clear in, e.g. <see cref="XyDirection.XDirection"/> clears the X-Axis gridlines</param>
        /// <remarks></remarks>
        public void Clear(XyDirection xyDirection)
        {
            // Do nothing
            throw new InvalidOperationException("GridLinesPanel no longer draws gridlines. These are now added to the RenderSurfaceBase instance instead for performance. ");
        }

        /// <summary>
        /// Adds a line to the panel in the specific <see cref="XyDirection"/>
        /// </summary>
        /// <param name="xyDirection">The <see cref="XyDirection"/> to clear in, e.g. <see cref="XyDirection.XDirection"/> adds an X-Axis gridline</param>
        /// <param name="line">The line to add</param>
        /// <remarks></remarks>
        public void AddLine(XyDirection xyDirection, Line line)
        {
            // Do nothing
            throw new InvalidOperationException("GridLinesPanel no longer draws gridlines. These are now added to the RenderSurfaceBase instance instead for performance. ");
        }

        /// <summary>
        /// Gets the width of the panel in pixels
        /// </summary>
        int IGridLinesPanel.Width
        {
            get { return (int)ActualWidth; }
        }

        /// <summary>
        /// Gets the height of the panel in pixels
        /// </summary>
        int IGridLinesPanel.Height
        {
            get { return (int)ActualHeight; }
        }

        /// <summary>
        /// Generates and adds a <see cref="Line"/> element to the <see cref="GridLinesPanel"/>. Applies the direction and style to the line as 
        /// well as Id so they may be re-used (pooled)
        /// </summary>
        /// <param name="lineId">The line Id</param>
        /// <param name="xyDirection">The direction, X or Y</param>
        /// <param name="lineStyle">The style to apply to the line</param>
        /// <returns>The <see cref="Line"/> instance, which has been added to the <see cref="GridLinesPanel"/></returns>
        public Line GenerateElement(int lineId, XyDirection xyDirection, Style lineStyle)
        {
            throw new InvalidOperationException("GridLinesPanel no longer draws gridlines. These are now added to the RenderSurfaceBase instance instead for performance. ");
        }

        /// <summary>
        /// Removes all <see cref="Line"/> instances after the specified index. This method is used when re-drawing the <see cref="UltrachartSurface"/> 
        /// when the number of lines has reduced from one redraw to the next. 
        /// </summary>
        /// <param name="xyDirection">The direction to clear, X or Y</param>
        /// <param name="index">The index to remove after (inclusive)</param>
        public void RemoveElementsAfter(XyDirection xyDirection, int index)
        {
            throw new InvalidOperationException("GridLinesPanel no longer draws gridlines. These are now added to the RenderSurfaceBase instance instead for performance. ");
        }

        private void GridLinesPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //TODO: Investigate - do we need this?
            //_eventAggregator.Publish(new InvalidateUltrachartMessage(this));
        }  
    }
}