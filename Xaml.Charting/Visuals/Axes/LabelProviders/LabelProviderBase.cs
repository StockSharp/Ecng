// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LabelProviderBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics;

namespace Ecng.Xaml.Charting.Visuals.Axes
{    
    /// <summary>
    /// Base class to define a LabelProvider. The LabelProvider may be set or data-bound to the <see cref="AxisBase.LabelProvider"/> property, allowing
    /// programmatic overriding of axis labels. 
    /// 
    /// Create a class which implements <see cref="ILabelProvider"/> and return string values from the <see cref="ILabelProvider.FormatLabel"/> and <see cref="ILabelProvider.FormatCursorLabel"/> methods
    /// </summary>
    public abstract class LabelProviderBase : ProviderBase, ILabelProvider
    {
        /// <summary>
        /// Called at the start of an axis render pass, before any labels are formatted for the current draw operation
        /// </summary>
        public virtual void OnBeginAxisDraw(){}

        /// <summary>
        /// Creates a <see cref="ITickLabelViewModel"/> instance, based on the data-value passed in.
        /// Invokes <see cref="FormatLabel"/> to format the specified data-value passed in.
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        public virtual ITickLabelViewModel CreateDataContext(IComparable dataValue)
        {
            return UpdateDataContext(new DefaultTickLabelViewModel(), dataValue);
        }

        /// <summary>
        /// Updates existing <see cref="ITickLabelViewModel"/>, based on the data-value passed in.
        /// Invokes <see cref="FormatLabel"/> to format the specified data-value passed in.
        /// </summary>
        /// <param name="labelDataContext">The instance to update</param>
        /// <param name="dataValue">The data-value to format</param>
        public virtual ITickLabelViewModel UpdateDataContext(ITickLabelViewModel labelDataContext, IComparable dataValue)
        {
            var formatted = FormatLabel(dataValue);
            labelDataContext.Text = formatted;

            return labelDataContext;
        }

        /// <summary>
        /// Formats a label for the axis from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted label string
        /// </returns>
        public abstract string FormatLabel(IComparable dataValue);

        /// <summary>
        /// Formats a label for the cursor, from the specified data-value passed in
        /// </summary>
        /// <param name="dataValue">The data-value to format</param>
        /// <returns>
        /// The formatted cursor label string
        /// </returns>
        public abstract string FormatCursorLabel(IComparable dataValue);
    }
}
