// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DispatcherUtil.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Ecng.Xaml.Charting.Utility
{
    internal interface IDispatcherFacade
    {
        void BeginInvoke(Action action, DispatcherPriority dispatcherPriority);
        void BeginInvokeIfRequired(Action action, DispatcherPriority dispatcherPriority);
    }

    internal class DispatcherUtil : IDispatcherFacade
    {
        private Dispatcher _dispatcherInstance;
        private static bool _testMode;

        public DispatcherUtil(Dispatcher dispatcher)
        {
            if (!_testMode)
                _dispatcherInstance = dispatcher;
        }

        public void BeginInvoke(Action action, DispatcherPriority dispatcherPriority)
        {
            if (_dispatcherInstance == null)
            {
                action();
                return;
            }

#if !SILVERLIGHT
            _dispatcherInstance.BeginInvoke(action, dispatcherPriority);
#else
            _dispatcherInstance.BeginInvoke(action);
#endif
        }

        public void BeginInvokeIfRequired(Action action, DispatcherPriority priority)
        {
            if (_dispatcherInstance == null || _dispatcherInstance.CheckAccess())
            {
                action();
                return;
            }
            
            BeginInvoke(action, priority);
        }

        
        public static void SetTestMode()
        {
            _testMode = true;
        }

        public static bool GetTestMode()
        {
            return _testMode;
        }

    }
}