// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisTitleTemplateSelector.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Used Internally by Ultrachart. Selects the Axis Title Template depending on title object type 
    /// </summary>
    public class AxisTitleTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _stringTitleTemplate;

        /// <summary>
        /// Gets or sets the standard String DataTemplate
        /// </summary>
        public DataTemplate StringTitleTemplate
        {
            get { return _stringTitleTemplate; }
            set
            {
                _stringTitleTemplate = value;
                UpdateControlTemplate();
            }
        }

        /// <summary>
        /// When overidden in derived classes, contains the logic for choosing a proper DataTemplate
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var title = item as String;

            var dataTemplate = title != null ? StringTitleTemplate : base.SelectTemplate(item, container);

            return dataTemplate;
        }
    }
}
