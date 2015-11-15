// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ChartGroupChild.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting
{
    internal class ChartGroup
    {
        public ChartGroup(IUltrachartSurface ultraChartSurface)
        {
            Guard.NotNull(ultraChartSurface, "ultraChartSurface");

            UltrachartSurface = ultraChartSurface;
        }

        internal IUltrachartSurface UltrachartSurface { get; private set; }

        public override bool Equals(object obj)
        {
            var other = obj as ChartGroup;
            if (other == null) return false;

            return other.UltrachartSurface == UltrachartSurface;
        }

        public override int GetHashCode()
        {
            return UltrachartSurface.GetHashCode();
        }

        internal void RestoreState()
        {
            var surface = UltrachartSurface as UltrachartSurface;
            if (surface == null) return;

            if (surface.AxisAreaLeft != null)
            {
                surface.AxisAreaLeft.Margin = new Thickness();
            }

            if (surface.AxisAreaRight != null)
            {
                surface.AxisAreaRight.Margin = new Thickness();
            }
        }
    }
}