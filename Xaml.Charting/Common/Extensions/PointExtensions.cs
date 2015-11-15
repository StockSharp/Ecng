// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PointExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class PointExtensions
    {
        /// <summary>
        /// Returns a new Point where X and Y components are equivalent to Math.Floor of the input point
        /// </summary>
        /// <param name="point">The input point, e.g. x=1.242, y=6.336</param>
        /// <returns>The Floor'ed point, e.g. x=1, y=6</returns>
        internal static Point Floor(this Point point)
        {
            return new Point((int)point.X, (int)point.Y);
        }
    }
}
