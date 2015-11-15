// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAxisBaseCommon.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Windows;
using System.Windows.Shapes;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Databinding;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Base properties and methods for 2D and 3D axis types
    /// </summary>
    public interface IAxisBaseCommon : IAxisParams
    {
        /// <summary>
        /// Raised when the VisibleRange is changed
        /// </summary>
        event EventHandler<VisibleRangeChangedEventArgs> VisibleRangeChanged;

        /// <summary>
        /// Gets or sets the ParentSurface that this Axis is associated with
        /// </summary>
        IUltrachartSurfaceBase ParentSurface { get; set; }        
    }
}