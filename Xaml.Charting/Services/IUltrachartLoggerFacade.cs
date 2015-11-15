// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IUltrachartLoggerFacade.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.Utility;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines the interface to a logger facade. If you wish to receive debug log messages from Ultrachart, then set a logger instance via 
    /// <see cref="UltrachartDebugLogger.SetLogger"/>. Note that logging will dramatically decrease performance, especially in a real-time scenario
    /// </summary>
    public interface IUltrachartLoggerFacade
    {
        /// <summary>
        /// Logs the string format message with optional arguments
        /// </summary>
        /// <param name="formatString">The formatting string</param>
        /// <param name="args">Optional arguments to the formatting string</param>
        void Log(string formatString, params object[] args);
    }
}