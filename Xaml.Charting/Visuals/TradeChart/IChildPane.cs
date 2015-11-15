// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IChildPane.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Input;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// The interface to a child pane in a <see cref="UltrachartGroup"/> control, which displays 1..Many <see cref="UltrachartSurface"/>
    /// controls in a vertical chart group. Intended specifically for stock charts. 
    /// 
    /// Derive from this interface when creating a ViewModel which will form the basis of a Child Pane in a multi-paned stock chart application
    /// </summary>
    public interface IChildPane
    {
        /// <summary>
        /// Gets or sets the Title of this Child Pane
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Causes the child pane to zoom to extents
        /// </summary>
        void ZoomExtents();

        /// <summary>
        /// A command which when invoked, closes the child pane
        /// </summary>
        ICommand ClosePaneCommand {get; set;}
    }
}
