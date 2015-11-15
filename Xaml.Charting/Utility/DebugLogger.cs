// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// DebugLogger.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Utility
{
    /// <summary>
    /// Provides a debug logger which can be used to pipe debug messages from Ultrachart to your own code, by setting the <see cref="IUltrachartLoggerFacade"/> via SetLogger
    /// </summary>
    public class UltrachartDebugLogger
    {
        private static readonly UltrachartDebugLogger _instance = new UltrachartDebugLogger();
        private IUltrachartLoggerFacade _loggerFacade;

        private UltrachartDebugLogger() { }

        /// <summary>
        /// Gets the singleton <see cref="UltrachartDebugLogger"/> instance
        /// </summary>
        public static UltrachartDebugLogger Instance { get { return _instance; } }

        /// <summary>
        /// Writes a line to the <see cref="IUltrachartLoggerFacade"/>. By default, the facade instance is null. In this case nothing happens
        /// </summary>
        /// <remarks>Logging is performance intensive and will drastically slow down the chart.</remarks>
        /// <param name="formatString">The format string</param>
        /// <param name="args">Optional args for the format string</param>
        public void WriteLine(string formatString, params object[] args)
        {
            if (_loggerFacade != null)
            {
                _loggerFacade.Log(formatString, args);
            }
        }

        /// <summary>
        /// Sets the <see cref="IUltrachartLoggerFacade"/> to write to. By default this is null, but after being set, the <see cref="UltrachartDebugLogger"/> will write all output to this instance
        /// </summary>
        /// <param name="loggerFacade">The <see cref="IUltrachartLoggerFacade"/> instance.</param>
        /// <remarks>Logging is performance intensive and will drastically slow down the chart. Enable only when necessary</remarks>
        public void SetLogger(IUltrachartLoggerFacade loggerFacade)
        {
            _loggerFacade = loggerFacade;
        }
    }
}