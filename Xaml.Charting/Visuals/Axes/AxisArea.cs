// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisArea.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Provides a container for Axis within the <see cref="UltrachartSurface"/>. Styled by control template
    /// </summary>
    public class AxisArea : ItemsControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AxisArea"/> class.
        /// </summary>
        /// <remarks></remarks>
        public AxisArea()
        {
        }

        internal void SafeRemoveItem(object item)
        {            
            try
            {
                if (item == null) return;
                if (this.Items == null) return;
                if (this.Items.Contains(item) == false) return;

                this.Items.Remove(item);
            }
            // Suppresses NullReferenceException reported in KAN-223-38229 and OZD-619-13161
            // ReSharper disable once EmptyGeneralCatchClause
            catch { }
        }
    }
}