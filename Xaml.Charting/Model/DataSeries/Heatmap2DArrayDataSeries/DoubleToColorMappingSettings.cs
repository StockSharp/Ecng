// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DoubleToColorMappingSettings.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Model.DataSeries
{
    /// <summary>
    /// Contains settings to translate double value into color for heatmap
    /// </summary>
    internal class DoubleToColorMappingSettings
    {
        public GradientStop[] GradientStops;
        public double Minimum, ScaleFactor;

        public override bool Equals(object obj)
        {
            var equals = false;

            var settings = obj as DoubleToColorMappingSettings;
            if (settings != null)
            {
                equals = settings.Minimum.Equals(Minimum) &&
                         settings.ScaleFactor.Equals(ScaleFactor) &&
                         settings.GradientStops.Length.Equals(GradientStops.Length) &&
                         settings.GradientStops.Equals(GradientStops);
            }

            return equals;
        }

        /// <summary>
        /// contains pre-calculated color values
        /// </summary>
        public int[] CachedMap;
    }
}
