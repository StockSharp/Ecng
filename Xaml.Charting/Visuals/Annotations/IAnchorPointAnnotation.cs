// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IAnchorPointAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// Defines the interface to an Anchor-Point annotation, which is an <see cref="IAnnotation"/> which only has one X1,Y1 point. 
    /// This annotation may be anchored around the coordinate using various alignmnets. See the <see cref="HorizontalAnchorPoint"/> and <see cref="VerticalAnchorPoint"/> properties
    /// for more information
    /// </summary>
    public interface IAnchorPointAnnotation : IAnnotation
    {
        /// <summary>
        /// Gets or sets the <see cref="HorizontalAnchorPoint"/>. 
        /// 
        /// The value of Left means the X1,Y1 coordinate of the annotation is on the Left horizontally.
        /// The value of Center means the X1,Y1 coordinate of the annotation is at the center horizontally.
        /// The value of Right means the X1,Y1 coordinate of the annotation is at the right horizontally.
        /// </summary>
        HorizontalAnchorPoint HorizontalAnchorPoint { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="VerticalAnchorPoint"/>. 
        /// 
        /// The value of Top means the X1,Y1 coordinate of the annotation is on the Top vertically.
        /// The value of Center means the X1,Y1 coordinate of the annotation is at the center vertically.
        /// The value of Bottom means the X1,Y1 coordinate of the annotation is at the Bottom vertically.
        /// </summary>
        VerticalAnchorPoint VerticalAnchorPoint { get; set; }

        /// <summary>
        /// Gets the computed VerticalOffset in pixels to apply to this annotation when placing
        /// </summary>
        double VerticalOffset { get; }

        /// <summary>
        /// Gets the computed HorizontalOffset in pixels to apply to this annotation when placing
        /// </summary>
        double HorizontalOffset { get; }
    }
}