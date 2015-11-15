// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisLabelTemplateSelector.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal class AxisLabelTemplateSelector : DataTemplateSelector
    {
        private DataTemplate _numericAxisLabelTemplate;

        public DataTemplate NumericAxisLabelTemplate
        {
            get { return _numericAxisLabelTemplate; }
            set
            {
                _numericAxisLabelTemplate = value;
                UpdateControlTemplate();
            }
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var label = item as ITickLabelViewModel;

            var dataTemplate = label is NumericTickLabelViewModel ? NumericAxisLabelTemplate : base.SelectTemplate(item, container);

            return dataTemplate;
        }
    }
}
