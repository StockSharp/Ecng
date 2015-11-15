// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderMessages.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media.Imaging;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// When published, causes the <see cref="UltrachartSurface"/> to queue up an asynchronous redraw 
    /// </summary>
    public class InvalidateUltrachartMessage : LoggedMessageBase
    {
        /// <summary>
        /// Initializes a new instance of the MessageBase class.
        /// </summary>
        /// <param name="sender">Message sender (usually "this")</param>
        /// <remarks></remarks>
        public InvalidateUltrachartMessage(object sender) : base(sender)
        {            
        }
    }

    /// <summary>
    /// When published, causes the <see cref="UltrachartSurface"/> to zoom to extents and redraw
    /// </summary>
    public class ZoomExtentsMessage : LoggedMessageBase
    {
        /// <summary>
        /// If set to <c>true</c> zooms in the Y-direction only.
        /// </summary>
        public bool ZoomYOnly { get; private set; }

        /// <summary>
        /// Initializes a new instance of the MessageBase class.
        /// </summary>
        /// <param name="sender">Message sender (usually "this")</param>
        /// <remarks></remarks>
        public ZoomExtentsMessage(object sender) : base(sender)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZoomExtentsMessage" /> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="zoomYOnly">if set to <c>true</c> zooms in the Y-direction only.</param>
        public ZoomExtentsMessage(object sender, bool zoomYOnly) : this(sender)
        {
            ZoomYOnly = zoomYOnly;
        }
    }

    /// <summary>
    /// Published by <see cref="UltrachartSurface"/> after the chart surface resizes
    /// </summary>
    public class UltrachartResizedMessage : LoggedMessageBase
    {
        /// <summary>
        /// Initializes a new instance of the MessageBase class.
        /// </summary>
        /// <param name="sender">Message sender (usually "this")</param>
        /// <remarks></remarks>
        public UltrachartResizedMessage(object sender)
            : base(sender)
        {
        }
    }

    /// <summary>
    /// Published by <see cref="UltrachartSurface"/> immediately before the end of a render pass
    /// </summary>
    public class UltrachartRenderedMessage : LoggedMessageBase
    {
        private readonly IRenderContext2D _renderContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UltrachartRenderedMessage" /> class.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="renderContext">The render context.</param>
        public UltrachartRenderedMessage(object sender, IRenderContext2D renderContext) : base(sender)
        {
            _renderContext = renderContext;
        }

        /// <summary>
        /// OBSOLETE
        /// </summary>
        [Obsolete("BitmapContext is no longer exposed. Instead, use RenderContext for 2D drawing operations")]
        public BitmapContext BitmapContext
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the RenderContext
        /// </summary>
        public IRenderContext2D RenderContext
        {
            get { return _renderContext; }
        }
    }
}