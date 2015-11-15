// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ITickLabelViewModel.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    /// <summary>
    /// Defines the base interface to a Tick Label Viewmodel - a viewmodel which each Axis Tick Label will bind to
    /// </summary>
    public interface ITickLabelViewModel
    {
        /// <summary>
        /// Gets or sets if the Tick Label has an exponent. NOTE Only valid for Numeric Axis. Ignored by DateTime or TimeSpan axes
        /// </summary>
        bool HasExponent { get; set; }

        /// <summary>
        /// Gets or sets the Separator, for example the E symbol in Engineering notifation, or x10^ for Scientific Notation. NOTE Only valid for Numeric Axis. Ignored by DateTime or TimeSpan axes
        /// </summary>
        string Separator { get; set; }

        /// <summary>
        /// Gets or sets the exponent. This is the power of 10 exponent in string format. NOTE Only valid for Numeric Axis. Ignored by DateTime or TimeSpan axes
        /// </summary>
        string Exponent { get; set; }

        /// <summary>
        /// Gets or sets the Text for the tick label
        /// </summary>
        string Text { get; set; }
    }

    public class DefaultTickLabelViewModel : ITickLabelViewModel, INotifyPropertyChanged
    {
        private string _text;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged("Text");
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual bool HasExponent { get; set; }

        public virtual string Separator { get; set; }

        public virtual string Exponent { get; set; }
    }

    public class NumericTickLabelViewModel : DefaultTickLabelViewModel
    {
        private bool _hasExponent;
        private string _separator = String.Empty;
        private string _exponent = String.Empty;

        public override bool HasExponent
        {
            get { return _hasExponent; }
            set
            {
                if (_hasExponent != value)
                {
                    _hasExponent = value;
                    OnPropertyChanged("HasExponent");
                }
            }
        }

        public override string Separator
        {
            get { return _separator; }
            set
            {
                if (_separator != value)
                {
                    _separator = value;
                    OnPropertyChanged("Separator");
                }
            }
        }

        public override string Exponent
        {
            get { return _exponent; }
            set
            {
                if (_exponent != value)
                {
                    _exponent = value;
                    OnPropertyChanged("Exponent");
                }
            }
        }
    }
}
