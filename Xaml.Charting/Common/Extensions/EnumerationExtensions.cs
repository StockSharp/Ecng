// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// EnumerationExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Numerics;
using Ecng.Xaml.Charting.Visuals.Annotations;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class EnumerationExtensions
    {
        internal static bool IsTop(this LabelPlacement placement)
        {
            return placement == LabelPlacement.Top || placement == LabelPlacement.TopLeft ||
                   placement == LabelPlacement.TopRight;
        }

        internal static bool IsBottom(this LabelPlacement placement)
        {
            return placement == LabelPlacement.Bottom || placement == LabelPlacement.BottomLeft ||
                   placement == LabelPlacement.BottomRight;
        }

        internal static bool IsRight(this LabelPlacement placement)
        {
            return placement == LabelPlacement.Right || placement == LabelPlacement.TopRight ||
                   placement == LabelPlacement.BottomRight;
        }

        internal static bool IsLeft(this LabelPlacement placement)
        {
            return placement == LabelPlacement.Left || placement == LabelPlacement.TopLeft ||
                   placement == LabelPlacement.BottomLeft;
        }

        internal static bool IsAxis(this LabelPlacement placement)
        {
            return placement == LabelPlacement.Axis;
        }
    }
}
