using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ecng.Xaml.Charting.Rendering.Common;
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal class RenderTimerHelper
    {
        private RenderTimer _renderTimer;
        private volatile bool _needToRedraw;

        private readonly Action _action;
        private readonly IDispatcherFacade _dispatcher;

        public RenderTimerHelper(Action action, IDispatcherFacade dispatcher)
        {
            _action = action;
            _dispatcher = dispatcher;
        }

        public void OnLoaded()
        {
            StopTimer();
            StartTimer();

            OnRenderTimeElapsed();
        }

        private void StopTimer()
        {
            if (_renderTimer != null)
            {
                _renderTimer.Dispose();
                _renderTimer = null;
            }
        }

        private void StartTimer()
        {
            _renderTimer = new RenderTimer(null, _dispatcher, OnRenderTimeElapsed);
        }

        public void OnUnlodaed()
        {
            StopTimer();
        }

        private void OnRenderTimeElapsed()
        {
            if (_needToRedraw)
            {
                try
                {
                    _action();
                }
                finally
                {
                    _needToRedraw = false;
                }
            }
        }

        public void Invalidate()
        {
            _needToRedraw = true;
        }
    }
}
