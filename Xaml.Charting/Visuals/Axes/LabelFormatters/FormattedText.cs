// *************************************************************************************
// ULTRACHART © Copyright ulc software Services Ltd. 2011-2013. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: info@ulcsoftware.co.uk
// 
// FormattedText.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Visuals.Axes
{
    internal class FormattedText
    {
        private bool _hasExponent;
        private const int MinDistance = 1;
        private const double ExpFontSizeCoef = 1.2;

        public TextBlock Significand
        {
            get;
            private set;
        }

        public TextBlock Exponent
        {
            get;
            private set;
        }

        public TextBlock Separator
        {
            get;
            private set;
        }

        public Rect LabelRect
        {
            get;
            private set;
        }

        public Thickness Indents
        {
            get;
            private set;
        }

        public double FontSize
        {
            get;
            private set;
        }

        public bool HasExponent 
        {
            get { return _hasExponent; }
        }

        public FormattedText(double x, double y, string text, TextAlignment textAlignment, ScientificNotation mode = ScientificNotation.None):
            this(x, y, new Thickness(), text, textAlignment, 0, mode)
        {}

        public FormattedText(double x, double y, string text, TextAlignment textAlignment, double fontSize, ScientificNotation mode = ScientificNotation.None) :
            this(x, y, new Thickness(), text, textAlignment, fontSize, mode)
        { }

        public FormattedText(double x, double y, Thickness indents, string text, TextAlignment textAlignment, double fontSize, ScientificNotation mode = ScientificNotation.None)
        {
            _hasExponent = false;
            Indents = indents;

            if (fontSize > 0d)
            {
                FontSize = fontSize;
            }

            Parse(text, mode);
            Measure(x, y, textAlignment);
        }

        private void Parse(string text, ScientificNotation mode)
        {
            var significand = text;
            var exponent = "";
            var separator = "";            

            var ind = text.IndexOfAny(new[] { 'e', 'E' });
            if (ind > 0 && mode != ScientificNotation.None)
            {
                _hasExponent = true;

                significand = text.Substring(0, ind);
                exponent = text.Substring(ind + 1);
                separator = text[ind].ToString(CultureInfo.InvariantCulture);

                if (mode == ScientificNotation.Normalized)
                {
                    separator = "x10";
                }
            }
            
            Significand = new TextBlock { Text = significand };
            if (FontSize.CompareTo(0d) != 0)
            {
                Significand.FontSize = FontSize;
            }

            Exponent = _hasExponent ? new TextBlock { Text = exponent } : null;
            Separator = _hasExponent ? new TextBlock { Text = separator } : null;
        }

        private void Measure(double x, double y, TextAlignment textAlignment)
        {
            Significand.MeasureArrange();

            if (_hasExponent)
            {
                Exponent.FontSize = Significand.FontSize/ExpFontSizeCoef;
                Separator.FontSize = Significand.FontSize;
                
                Separator.MeasureArrange();
                Exponent.MeasureArrange();
            }

            double separatorWidth = Separator == null ? 0.0 : Separator.ActualWidth;
            double exponentWidth = Exponent == null ? 0.0 : Exponent.ActualWidth;
            string exponentText = Exponent == null ? string.Empty : Exponent.Text;
            double exponentHeight = Exponent == null ? 0.0 : Exponent.ActualHeight;

            var labelWidth = Significand.ActualWidth + separatorWidth + exponentWidth + (_hasExponent ? 0 : MinDistance * 2);
            var labelHeight = Significand.ActualHeight + exponentHeight / 2;

            var yCoord = y + exponentHeight / 2;
            var xCoord = x - labelWidth / 2;

            switch (textAlignment)
            {
                case TextAlignment.Center:
                    SetLeft(Significand, xCoord);
                    SetTop(Significand, yCoord);

                    if (_hasExponent)
                    {
                        SetLeft(Separator, xCoord + Significand.ActualWidth + MinDistance);
                        SetTop(Separator, yCoord);

                        SetLeft(Exponent, xCoord + Significand.ActualWidth + separatorWidth + MinDistance);
                        SetTop(Exponent, y);
                    }

                    yCoord = y;
                    LabelRect = new Rect(xCoord - Indents.Left, yCoord - Indents.Top,
                                         labelWidth + Indents.Right,
                                         labelHeight + Indents.Bottom);

                    break;
                case TextAlignment.Left:
                case TextAlignment.Right:
                    xCoord = x;
                    yCoord = y - Significand.ActualHeight / 2;

                    SetLeft(Significand, xCoord);
                    SetTop(Significand, yCoord);

                    if (_hasExponent)
                    {
                        SetLeft(Separator, xCoord + Significand.ActualWidth + MinDistance);
                        SetTop(Separator, yCoord);

                        SetLeft(Exponent, xCoord + Significand.ActualWidth + separatorWidth + MinDistance);
                        SetTop(Exponent, yCoord - exponentHeight / 2);
                    }

                    yCoord = yCoord - exponentHeight / 2;
                    LabelRect = new Rect(x - Indents.Left, yCoord - Indents.Top,
                                         Significand.ActualWidth + separatorWidth + exponentWidth +
                                         (exponentText.IsNullOrWhiteSpace() ? 0 : MinDistance * 2) + Indents.Right,
                                         Significand.ActualHeight + exponentHeight / 2 + Indents.Bottom);
                    break;
                default:
                    throw new InvalidOperationException("Invalid TextAlignment");
            }

            LabelRect = new Rect(xCoord, yCoord, labelWidth, labelHeight);
        }

        private void SetTop(FrameworkElement element, double top)
        {
            element.Margin = new Thickness(element.Margin.Left, top, element.Margin.Right, element.Margin.Bottom);
        }

        private void SetLeft(FrameworkElement element, double left)
        {
            element.Margin = new Thickness(left, element.Margin.Top, element.Margin.Right, element.Margin.Bottom);
        }
    }
}