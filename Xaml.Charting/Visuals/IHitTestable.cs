// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IHitTestable.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Defines the base interface for a type which can be hit-tested
    /// </summary>
    /// <remarks></remarks>
    public interface IHitTestable
    {
        /// <summary>
        /// Gets the width of the <see cref="IHitTestable"/>
        /// </summary>
        double ActualWidth { get; }

        /// <summary>
        /// Gets the height of the <see cref="IHitTestable"/>
        /// </summary>
        double ActualHeight { get; }

        /// <summary>
        /// Translates the point relative to the other <see cref="IHitTestable"/> element
        /// </summary>
        /// <param name="point">The input point relative to this <see cref="IHitTestable"/></param>
        /// <param name="relativeTo">The other <see cref="IHitTestable"/> to use when transforming the point</param>
        /// <returns>The transformed Point</returns>
        Point TranslatePoint(Point point, IHitTestable relativeTo);

        /// <summary>
        /// Returns true if the Point is within the bounds of the current <see cref="IHitTestable"/> element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>true if the Point is within the bounds</returns>
        bool IsPointWithinBounds(Point point);

        /// <summary>
        /// Gets the bounds of the current <see cref="IHitTestable"/> element relative to another <see cref="IHitTestable"/> element
        /// </summary>
        /// <param name="relativeTo"></param>
        /// <returns></returns>
        Rect GetBoundsRelativeTo(IHitTestable relativeTo);
    }
}