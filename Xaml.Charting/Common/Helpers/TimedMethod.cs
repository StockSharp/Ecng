// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TimedMethod.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Threading;

namespace Ecng.Xaml.Charting.Common.Helpers
{
    public class TimedMethod : IDisposable
    {
        private readonly Action _action;
        private int _milliseconds = 100;
        private DispatcherTimer _timer;
        private DispatcherPriority _priority = DispatcherPriority.Background;

        private TimedMethod(Action action)
        {
            _action = action;
        }

        public static TimedMethod Invoke(Action action)
        {
            return new TimedMethod(action);
        }

        public TimedMethod WithPriority(DispatcherPriority priority)
        {
            _priority = priority;
            return this;
        }

        public TimedMethod After(int milliseconds)
        {
            _milliseconds = milliseconds;
            return this;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }
        }

        public TimedMethod Go()
        {
            if (_milliseconds <= 0)
            {
                _action();
                return this;
            }

#if SILVERLIGHT
            _timer = new DispatcherTimer();            
#else
            _timer = new DispatcherTimer(_priority);            
#endif

            _timer.Interval = TimeSpan.FromMilliseconds(_milliseconds);
            _timer.Tick += (s, e) =>
            {
                _action();
                _timer.Stop();
            };
            _timer.Start();
            return this;
        }
    }
}
