// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DispatcherExtensions.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Threading;

namespace Ecng.Xaml.Charting.Common.Extensions
{
    internal static class DispatcherExtensions
    {
        internal static void BeginInvokeAlways(this Dispatcher dispatcher, Action operation)
        {
            dispatcher.BeginInvoke(operation);
        }
            
        internal static void BeginInvokeIfRequired(this Dispatcher dispatcher, Action operation)
        {
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                operation();
                return;
            }

            dispatcher.BeginInvoke(operation);
        }
    }
}