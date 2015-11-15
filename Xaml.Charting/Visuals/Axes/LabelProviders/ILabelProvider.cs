// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ILabelProvider.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Interface to define a LabelProvider. The LabelFormatter may be set or data-bound to the <see cref="AxisBase.LabelProvider"/> property, allowing
    /// programmatic overriding of axis labels. 
    /// 
    /// Create a class which implements <see cref="ILabelProvider"/> and return string values from the <see cref="ILabelProvider.FormatLabel"/> and <see cref="ILabelProvider.FormatCursorLabel"/> methods
    /// </summary>
    public interface ILabelProvider
    {
        /// <summary>
        /// Called when the label formatted is initialized as it is attached to the parent axis, with the parent axis instance
        /// </summary>
        /// <param name="parentAxis">The parent <see cref="IAxis"/> instance</param>
        void Init(IAxis parentAxis);

        /// <summary>
        /// Called at the start of an axis render pass, before any labels are formatted for the current draw operation
        /// </summary>
        void OnBeginAxisDraw();

        /// <summary>
        /// Creates a <see cref="ITickLabelViewModel"/> instance, based on the data-value passed in.
        /// Invokes <see cref="FormatLabel"/> to format the specified data-value passed in.
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        ITickLabelViewModel CreateDataContext(IComparable dataValue);

        /// <summary>
        /// Updates existing <see cref="ITickLabelViewModel"/>, based on the data-value passed in.
        /// Invokes <see cref="FormatLabel"/> to format the specified data-value passed in.
        /// </summary>
        /// <param name="labelDataContext">The instance to update</param>
        /// <param name="dataValue">The data-value to format</param>
        ITickLabelViewModel UpdateDataContext(ITickLabelViewModel labelDataContext, IComparable dataValue);

        /// <summary>
        /// Formats a label for the axis from the specified data-value passed in 
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>The formatted label string</returns>
        string FormatLabel(IComparable dataValue);

        /// <summary>
        /// Formats a label for the cursor, from the specified data-value passed in 
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>The formatted cursor label string</returns>
        string FormatCursorLabel(IComparable dataValue);
    }
}