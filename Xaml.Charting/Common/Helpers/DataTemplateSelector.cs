// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DataTemplateSelector.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines base interface for DataTemplateSelector which is used for selecting DataTemplate
    /// </summary>
    public interface IDataTemplateSelector
    {
        /// <summary>
        /// Contains the logic for choosing a proper DataTemplate
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        DataTemplate SelectTemplate(object item, DependencyObject container);

        /// <summary>
        /// Raised when one of DataTemplate properties changed
        /// </summary>
        event EventHandler DataTemplateChanged;
    }

    /// <summary>
    /// Provides the base functionality for template selectors, used by the <see cref="RolloverModifier"/> and <see cref="CursorModifier"/>
    /// to select an appropriate <see cref="DataTemplate"/> for the <see cref="HitTestInfo"/> type outputted by the modifiers (which is dependent on RenderableSeries type)
    /// </summary>
    public abstract class DataTemplateSelector : ContentControl, IDataTemplateSelector
    {
        /// <summary>
        /// Defines the DefaultTemplate DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DefaultTemplateProperty = DependencyProperty.Register("DefaultTemplate", typeof (DataTemplate), typeof (DataTemplateSelector), new PropertyMetadata(default(DataTemplate),OnDefautlTemplateDependencyPropertyChanged));

        /// <summary>
        /// Gets or sets the template which is used as default
        /// </summary>
        public DataTemplate DefaultTemplate
        {
            get { return (DataTemplate) GetValue(DefaultTemplateProperty); }
            set { SetValue(DefaultTemplateProperty, value); }
        }

        /// <summary>
        /// Forces an update of ControlTemplate due to known bug in Wpf 4
        /// </summary>
        /// <remarks>
        /// See http://social.msdn.microsoft.com/Forums/nl/wpf/thread/e6643abc-4457-44aa-a3ee-dd389c88bd86 for more info
        /// </remarks>
        protected void UpdateControlTemplate()
        {
            //Need to call when DataTemplates changed due to known bug in Wpf 4
            // look at http://social.msdn.microsoft.com/Forums/nl/wpf/thread/e6643abc-4457-44aa-a3ee-dd389c88bd86 for more info
#if !SILVERLIGHT
            if (ContentTemplate == null)
            {
                ContentTemplate = SelectTemplate(Content, this);
            }
#endif
        }

        /// <summary>
        /// When overidden in derived classes, contains the logic for choosing a proper DataTemplate
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public virtual DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return DefaultTemplate;
        }

        /// <summary>
        /// Raised when one of DataTemplate properties changed
        /// </summary>
        public event EventHandler DataTemplateChanged;

        /// <summary>
        /// Called when the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property changes.
        /// </summary>
        /// <param name="oldContent">The old value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property.</param>
        /// <param name="newContent">The new value of the <see cref="P:System.Windows.Controls.ContentControl.Content" /> property.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            var template = SelectTemplate(newContent, this);

            ContentTemplate = template;
        }

        /// <summary>
        /// Raises the DataTemplateChanged event
        /// </summary>
        protected void OnDataTemplateChanged()
        {
            var handler = this.DataTemplateChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected static void OnDefautlTemplateDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = d as DataTemplateSelector;
            if (selector != null)
            {
                selector.UpdateControlTemplate();

                selector.OnDataTemplateChanged();
            }
        }
    }
}
