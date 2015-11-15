// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ItemPane.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Input;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// An ItemContainer for panes in the <see cref="UltrachartGroup"/> control
    /// </summary>
    public class ItemPane : INotifyPropertyChanged
    {
        private bool _isTabbed;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the Pane UIElement
        /// </summary>
        public FrameworkElement PaneElement { get; set; }

        /// <summary>
        /// Gets or sets the Pane ViewModel
        /// </summary>
        public IChildPane PaneViewModel { get; set; }

        internal bool IsMainPane { get; set; }

        /// <summary>
        /// Gets or sets whether this pane is tabbed
        /// </summary>
        public bool IsTabbed
        {
            get { return _isTabbed; }
            internal set
            {
                _isTabbed = value;
                OnPropertyChanged("IsTabbed");
            }
        }

        /// <summary>
        /// Gets or sets the change orientation command.
        /// </summary>
        public ICommand ChangeOrientationCommand { get; set; }

        /// <summary>
        /// Gets or sets the close pane command.
        /// </summary>
        public ICommand ClosePaneCommand { get; set; }

        /// <summary>
        /// Implementation of <see cref="INotifyPropertyChanged"/>
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if(handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }

}
