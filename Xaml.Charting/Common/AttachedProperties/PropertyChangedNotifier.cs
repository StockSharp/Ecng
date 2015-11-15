// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PropertyChangedNotifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Data;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Common.AttachedProperties
{
    internal sealed class PropertyChangeNotifier : DependencyObject, IDisposable
    {
        internal static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(PropertyChangeNotifier), new PropertyMetadata(null, new PropertyChangedCallback(OnPropertyChanged)));

        internal event EventHandler ValueChanged;

        private readonly WeakReference _propertySource;

        internal PropertyChangeNotifier(DependencyObject propertySource, string path)
            : this(propertySource, new PropertyPath(path))
        {
        }

        internal PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
            : this(propertySource, new PropertyPath(property))
        {
        }

        internal PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
        {
            Guard.NotNull(propertySource, "propertySource");
            Guard.NotNull(propertySource, "propertySource");

            _propertySource = new WeakReference(propertySource);
            var binding = new Binding {Path = property, Mode = BindingMode.OneWay, Source = propertySource};
            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        internal DependencyObject PropertySource
        {
            get
            {
                try
                {
                    return this._propertySource.IsAlive ? this._propertySource.Target as DependencyObject : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        [Bindable(true)]
        internal object Value
        {
            get { return GetValue(ValueProperty); }
            set { this.SetValue(ValueProperty, value); }
        }

        public void Dispose()
        {
            this.ClearValue(ValueProperty);
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var notifier = (PropertyChangeNotifier)d;
            if (null != notifier.ValueChanged)
                notifier.ValueChanged(notifier, EventArgs.Empty);
        }
    }
}
