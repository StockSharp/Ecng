// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IPen.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Defines the interface to a 2D pen, used to draw lines on the <see cref="IRenderSurface2D"/>
    /// </summary>
    public interface IPen2D : IPathColor, IDashSplittingContext
    {
        /// <summary>
        /// Gets the stroke thickness
        /// </summary>
        float StrokeThickness { get; }

        /// <summary>
        /// Gets if antialiasing should be used
        /// </summary>
        bool Antialiased { get; }

        /// <summary>
        /// Gets a value that describes a shape at the end of line
        /// </summary>
        PenLineCap StrokeEndLineCap { get; }
    }

    /// <summary>
    /// Defines interface to context of splitting line into dashes
    /// </summary>
    public interface IDashSplittingContext
    {
        bool HasDashes { get; }
        /// <summary>
        /// Optional array with lengths of dash pattern items
        /// </summary>
        double[] StrokeDashArray { get; }
        /// <summary>
        /// Current index in StrokeDashArray
        /// </summary>
        int StrokeDashArrayIndex { get; set; }
        /// <summary>
        /// Already passed length of current item in StrokeDashArray
        /// </summary>
        double StrokeDashArrayItemPassedLength { get; set; }
    }
}
