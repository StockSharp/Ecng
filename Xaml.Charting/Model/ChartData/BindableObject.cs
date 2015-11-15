// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BindableObject.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Provides a base-type for classes that need to raise <see cref="INotifyPropertyChanged"/> events
    /// </summary>
    [DataContract]
    public class BindableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks></remarks>
        private event PropertyChangedEventHandler _propertyChanged;

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <remarks></remarks>
        protected void OnPropertyChanged(string propertyName)
        {
            RaisePropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetField<T>(ref T field, T value, string name) {
            if(EqualityComparer<T>.Default.Equals(field, value)) return false;
            
            field = value;

            // ReSharper disable once ExplicitCallerInfoArgument
            OnPropertyChanged(name);

            return true;
        }

        // Explicit interface implementation
        private int _refCounter = 0;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                _propertyChanged += value;

                // HACK (or workaround ;-)). To prevent serious hanging in Silverlight on Theme Change after lots of drawing, we fire a property changed 
                // once in a blue moon. This causes the binding engine to collect (unsubscribe) old bindings to theme properties. 
                // Hanging only occurs in Silverlight, WPF seems to cope with this issue quite well, nevertheless we leave this in for both platforms
                if (Interlocked.Increment(ref _refCounter) > 100)
                {
                    _refCounter = 0;
                    RaisePropertyChanged(new PropertyChangedEventArgs("Nothing"));
                }
            }
            remove
            {
                //Debug.WriteLine("Removing event: Type={0}", GetType().Name);
                _propertyChanged -= value;
            }
        }

        /// <summary>
        /// Raises the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="oldValue">Old value of the property.</param>
        /// <param name="newValue">New value of the property.</param>
        /// <remarks></remarks>
        protected void OnPropertyChanged(string propertyName, object oldValue, object newValue)
        {
            RaisePropertyChanged(new PropertyChangedEventArgsWithValues(propertyName, oldValue, newValue));
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> selectorExpression) {
            RaisePropertyChanged(PropertyName(selectorExpression));
        }

        protected void RaisePropertyChanged(string name) {
            var eventHandler = _propertyChanged;
            if(eventHandler != null) eventHandler(this, new PropertyChangedEventArgs(name));
        }

        private void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            var handler = _propertyChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public static string PropertyName<T>(Expression<Func<T>> property) {
            var lambda = (LambdaExpression)property;

            MemberExpression memberExpression;
            if(lambda.Body is UnaryExpression) {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            } else {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return memberExpression.Member.Name;
        }
    }
}