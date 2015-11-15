// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PropertyChangedEventArgsWithValues.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting
{
    /// <summary>
    ///  Provides data for the System.ComponentModel.INotifyPropertyChanged.PropertyChanged event.
    /// </summary>
    public class PropertyChangedEventArgsWithValues : PropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the PropertyChangedEventArgsWithValues class
        /// </summary>
        /// <param name="propertyName"> The name of the property that changed.</param>
        /// <param name="oldValue"> Old value of the property that changed. </param>
        /// <param name="newValue"> New value of the property that changed. </param>
        public PropertyChangedEventArgsWithValues(string propertyName, object oldValue, object newValue): base(propertyName)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets an old value of property that changed
        /// </summary>
        public object OldValue
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets a new value of property that changed
        /// </summary>
        public object NewValue
        {
            get;
            protected set;
        }
    }
}
