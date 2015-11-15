// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderSynchronizedMouseMove.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Ecng.Xaml.Charting.Utility.Mouse
{
    /// <summary>
    /// Creates a 'throttled' MouseMove event which ensures that the UI
    /// rendering is not starved.
    /// </summary>    
    internal class RenderSynchronizedMouseMove : IDisposable
    {
        private bool _isWaiting;
        private readonly IPublishMouseEvents _publisher;

        internal event MouseEventHandler SynchronizedMouseMove;

        internal RenderSynchronizedMouseMove(IPublishMouseEvents publisher)
        {
            _publisher = publisher;
            publisher.MouseMove += new MouseEventHandler(PublisherMouseMove); 
        }

        public void Dispose()
        {
            _publisher.MouseMove -= PublisherMouseMove;
            SynchronizedMouseMove = null;
        }

        private void PublisherMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isWaiting)
            {
                OnSynchronizedMouseMove(e);
                _isWaiting = true;
                CompositionTarget.Rendering += CompositionTargetRendering;
            }
        }

        private void CompositionTargetRendering(object sender, EventArgs e)
        {
            _isWaiting = false;
            CompositionTarget.Rendering -= CompositionTargetRendering;
        }

        internal void OnSynchronizedMouseMove(MouseEventArgs args)
        {
            var handler = SynchronizedMouseMove;
            if (handler != null)
            {
                handler(_publisher, args);
            }            
        }
    }
}