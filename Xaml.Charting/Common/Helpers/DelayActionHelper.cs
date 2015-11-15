using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using Ecng.Xaml.Charting.Common.Extensions;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    internal class DelayActionHelper
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private EventHandler _eventHandler;

        public double Interval
        {
            get { return _timer.Interval.Milliseconds; }
            set
            {
                _timer.Interval = TimeSpan.FromMilliseconds(value); 
                Restart();
            }
        }

        public void Start(Action action)
        {
            // unsubscribe from previous action
            if (_eventHandler != null)
                _timer.Tick -= _eventHandler;

            _eventHandler = (sender, args) =>
            {
                Stop();

                action();
            };

            _timer.Tick += _eventHandler;

            Restart();
        }

        public void Restart()
        {
            _timer.Stop();
            _timer.Start();
        }
        
        public void Stop()
        {
            _timer.Stop();
        }
    }
}
