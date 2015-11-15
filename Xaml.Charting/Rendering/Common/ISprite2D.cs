// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ISprite2D.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Defines the interface to a 2D Sprite, a small fixed-size bitmap which is rendered repeatedly to the viewport
    /// </summary>
    /// <seealso cref="IRenderContext2D"/>
    /// <seealso cref="RenderContextBase"/>
    public interface ISprite2D : IDisposable
    {
        /// <summary>
        /// Gets the width of the Spite in pixels
        /// </summary>
        float Width { get; }

        /// <summary>
        /// Gets the height of the Sprite in pixels
        /// </summary>
        float Height { get; }
    }
}
