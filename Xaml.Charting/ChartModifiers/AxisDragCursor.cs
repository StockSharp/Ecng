// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// BitmapPrintingHelper.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AxisDragCursor"/> class.
    /// </summary>
    public class AxisDragCursor : Control
    {
        /// <summary>
        /// Defines the Angle DependencyProperty
        /// </summary>
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof (double), typeof (AxisDragCursor), new PropertyMetadata(default(double)));

        /// <summary>
        /// Initializes a new instance of the <see cref="YAxisDragModifier"/> class.
        /// </summary>
        public AxisDragCursor()
        {
            DefaultStyleKey = typeof (AxisDragCursor);
        }

        /// <summary>
        /// Defines by what Angle to rotate AxisDragCursor 
        /// </summary>
        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }
    }
}