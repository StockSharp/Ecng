// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisInfoTemplateSelector.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Provides class for choosing proper DataTemplate according to a <see cref="Type"/> of <see cref="AxisInfo"/>
    /// </summary>
    public class AxisInfoTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Defines the YAxisDataTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty YAxisDataTemplateProperty = DependencyProperty.Register("YAxisDataTemplate", typeof (DataTemplate), typeof (AxisInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));
        /// <summary>
        /// Defines the  XAxisDataTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty XAxisDataTemplateProperty = DependencyProperty.Register("XAxisDataTemplate", typeof(DataTemplate), typeof(AxisInfoTemplateSelector), new PropertyMetadata(OnDefautlTemplateDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="IAxis" />
        /// </summary>
        public DataTemplate YAxisDataTemplate
        {
            get { return (DataTemplate) GetValue(YAxisDataTemplateProperty); }
            set { SetValue(YAxisDataTemplateProperty, value); }
        }

        /// <summary>
        /// Gets or sets the DataTemplate for <see cref="IAxis" />
        /// </summary>
        public DataTemplate XAxisDataTemplate
        {
            get { return (DataTemplate) GetValue(XAxisDataTemplateProperty); }
            set { SetValue(XAxisDataTemplateProperty, value); }
        }

        /// <summary>
        /// When overidden in derived classes, contains the logic for choosing a proper DataTemplate
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var axisInfo = item as AxisInfo;

            var dataTemplate = base.SelectTemplate(item, container);

            if (axisInfo != null)
            {
                dataTemplate = axisInfo.IsXAxis ? XAxisDataTemplate : YAxisDataTemplate;
            }

            return dataTemplate;
        }
    }
}