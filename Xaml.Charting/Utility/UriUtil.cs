// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UriUtil.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Reflection;
using System.Text;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.Utility
{
    internal static class UriUtil
    {
#if !SILVERLIGHT
        internal static Uri MakePackUri(string resource)
        {
            return new Uri(string.Format("pack://application:,,,/{0};component/{1}", typeof (UriUtil).Assembly.GetDllName(),
                                 resource), UriKind.RelativeOrAbsolute);
        }

        internal static Uri MakePackUri(Assembly assembly, string resource)
        {
            return new Uri(string.Format("pack://application:,,,/{0};component/{1}", assembly.GetDllName(),
                                 resource), UriKind.RelativeOrAbsolute);
        }
#endif

#if SILVERLIGHT
        internal static Uri PackUri { get { return new Uri(string.Format("/{0};component/Resources/SCW.png", typeof(UltrachartSurface).Assembly.GetDllName()), UriKind.RelativeOrAbsolute); } }
        internal static Uri PackUri2 { get { return new Uri(string.Format("/{0};component/Resources/SCW.png", typeof(UltrachartSurface).Assembly.GetDllName()), UriKind.RelativeOrAbsolute); } }
        internal static Uri ExtUri { get { return new Uri("http://www.ultrachart.com"); } }
#else
        internal static Uri PackUri { get { return new Uri(string.Format("pack://application:,,,/{0};component/Resources/SCW.png", typeof(UltrachartSurface).Assembly.GetDllName())); } }
        internal static Uri PackUri2 { get { return new Uri(string.Format("pack://application:,,,/{0};component/Resources/sct.png", typeof(UltrachartSurface).Assembly.GetDllName())); } }
        internal static string ExtUri { get { return "http://www.ultrachart.com"; } }
#endif
    }
}
