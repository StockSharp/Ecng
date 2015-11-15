// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TemplatableControl.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Controls;
using System.Windows.Data;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.RenderableSeries
{
    /// <summary>
    /// Placeholder class for an empty control. Should be styled via control template
    /// </summary>
    public class TemplatableControl : ContentControl
    {
        
    }

    /// <summary>
    /// Placeholder class for a PointMarker. Should be styled via control template
    /// </summary>
    public class PointMarker : TemplatableControl
    {
        /// <summary>
        /// Defines the DeferredContent DependencyProperty
        /// </summary>
        public static readonly DependencyProperty DeferredContentProperty =
            DependencyProperty.Register("DeferredContent", typeof(DataTemplate), typeof(PointMarker), new PropertyMetadata(OnDeferredContentChanged));

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> which content is going to be used as a Content
        /// </summary>
        public DataTemplate DeferredContent
        {
            get { return (DataTemplate)GetValue(DeferredContentProperty); }
            set { SetValue(DeferredContentProperty, value); }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            if (DeferredContent != null)
            {
                Content = DeferredContent.LoadContent();
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="PointMarker"/>, which was created from the <paramref name="template"/>
        /// </summary>
        public static PointMarker CreateFromTemplate(ControlTemplate template, object dataContext = null)
        {
            PointMarker marker = null;

            if (template != null)
            {
                marker = new PointMarker { Template = template };

                if (dataContext != null) marker.DataContext = dataContext;

#if SILVERLIGHT
                marker.MeasureArrange();

                // Width, height required for offsetting on Canvas
                marker.Width = marker.DesiredSize.Width;
                marker.Height = marker.DesiredSize.Height;
#endif

                marker.ApplyTemplate();
            }

            return marker;
        }

        private static void OnDeferredContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pointMarker = d as PointMarker;
            if (pointMarker != null && pointMarker.DeferredContent != null)
            {
                var content = pointMarker.DeferredContent.LoadContent() as FrameworkElement;

                if (content != null)
                {
                    var binding = new Binding("DataContext") {Source = pointMarker};
                    content.SetBinding(DataContextProperty, binding);

                    pointMarker.Content = content;
                }
            }
        }
    }

    /// <summary>
    /// Used as a helper class to place a Legend inside. Used by <see cref="LegendModifier"/>.
    /// </summary>
    public class LegendPlaceholder : Control
    {
        /// <summary>
        /// Initializes a new <see cref="LegendPlaceholder"/> instance.
        /// </summary>
        public LegendPlaceholder()
        {
            DefaultStyleKey = typeof(LegendPlaceholder);
        }
    }

    /// <summary>
    /// Placeholder class for a <see cref="IDataTemplateSelector"/> instance
    /// </summary>
    public class TooltipControl : TemplatableControl
    {
        /// <summary>
        /// Defines Selector DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectorProperty = DependencyProperty.Register("Selector", typeof(IDataTemplateSelector), typeof(TooltipControl), new PropertyMetadata(default(DataTemplateSelector), OnSelectorDependencyPropertyChanged));

        /// <summary>
        /// Defines SelectorContext DependencyProperty
        /// </summary>
        public static readonly DependencyProperty SelectorContextProperty = DependencyProperty.Register("SelectorContext", typeof(object), typeof(TooltipControl), new PropertyMetadata(OnSelectorContextDependencyPropertyChanged));

        /// <summary>
        /// Gest or sets instance of <see cref="IDataTemplateSelector"/> which selects data template for current content
        /// </summary>
        public IDataTemplateSelector Selector
        {
            get { return (IDataTemplateSelector)GetValue(SelectorProperty); }
            set { SetValue(SelectorProperty, value); }
        }

        /// <summary>
        /// Gets or sets context for <see cref="Selector"/>
        /// </summary>
        public object SelectorContext
        {
            get { return (object) GetValue(SelectorContextProperty); }
            set { SetValue(SelectorContextProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TooltipControl"/> class.
        /// </summary>
        public TooltipControl()
        {
            DefaultStyleKey = typeof(TooltipControl);
        }

        private void UpdateContentTemplate(object context)
        {
            if (Selector != null)
            {
                var template = Selector.SelectTemplate(context, this);

                ContentTemplate = template;
            }
        }

        private static void OnSelectorDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TooltipControl;
            if (control != null)
            {
                var newSelector = e.NewValue as IDataTemplateSelector;
                if (newSelector != null)
                {
                    newSelector.DataTemplateChanged += control.UpdateContentTemplate;
                }

                var oldSelector = e.OldValue as IDataTemplateSelector;
                if (oldSelector != null)
                {
                    oldSelector.DataTemplateChanged -= control.UpdateContentTemplate;
                }

                control.UpdateContentTemplate(control.SelectorContext);
            }
        }

        void UpdateContentTemplate(object sender, EventArgs args)
        {
            UpdateContentTemplate(SelectorContext);
        }

        private static void OnSelectorContextDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TooltipControl;
            if (control != null)
            {
                control.UpdateContentTemplate(control.SelectorContext);
            }
        }
    }

    /// <summary>
    /// Used as a root element for <see cref="UltrachartScrollbar"/> resizing grip.
    /// </summary>
    public class ScrollbarResizeGrip : Control
    {
        public ScrollbarResizeGrip()
        {
            DefaultStyleKey = typeof(ScrollbarResizeGrip);
        }
    }

    /// <summary>
    /// Used as a root element for <see cref="UltrachartScrollbar"/> viewport which shows currently selected area.
    /// </summary>
    public class ScrollbarViewport : Control
    {
        public ScrollbarViewport()
        {
            DefaultStyleKey = typeof(ScrollbarViewport);
        }
    }
}