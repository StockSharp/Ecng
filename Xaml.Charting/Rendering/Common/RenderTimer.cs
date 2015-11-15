// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderTimer.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Media;
using System.Windows.Threading;
using Ecng.Xaml.Charting.Utility;
using Ecng.Xaml.Charting.Utility.Mouse;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    internal class RenderTimer : IDisposable
    {
        private readonly IDispatcherFacade _dispatcher;
        private readonly Action _renderOperation;
        private DispatcherTimer _timer;
        private volatile bool _isInRenderOperation;
        private volatile bool _disposed;

        internal RenderTimer(double? maxFrameRate, IDispatcherFacade dispatcher, Action renderOperation)
        {
            _dispatcher = dispatcher;
            _renderOperation = renderOperation;

            if (maxFrameRate.HasValue)
            {
                // User specifies MaxFrameRate, use DispatcherTimer to drive rendering
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / NumberUtil.Constrain(maxFrameRate.Value, 0.0, 100.0));
                _timer.Tick += TimerElapsed;
                _timer.Start();
            }
            else
            {
                // Default rendering based off CompositionTarget.Rendering event
                CompositionTarget.Rendering -= OnCompositionTargetRendering;
                CompositionTarget.Rendering += OnCompositionTargetRendering;
            }
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            if (_isInRenderOperation) 
                return;

            _isInRenderOperation = true;
            new CompositionSyncedDelegate(Callback);
        }

        private void Callback()
        {
            try { _renderOperation(); }
            finally { _isInRenderOperation = false; }
        }

        private void OnCompositionTargetRendering(object sender, EventArgs e)
        {
            if (_isInRenderOperation) return;
            _isInRenderOperation = true;

            Callback();
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Tick -= TimerElapsed;
                    _timer = null;
                }
                else if (!_disposed)
                {
                    _dispatcher.BeginInvokeIfRequired(() => CompositionTarget.Rendering -= OnCompositionTargetRendering, DispatcherPriority.Normal);                    
                }

                _disposed = true;
            }
        }
    }
}
