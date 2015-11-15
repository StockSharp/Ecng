// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// LoggedMessageBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using TinyMessenger;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Base class for automatically logged Event Aggregator messages
    /// </summary>
    public abstract class LoggedMessageBase : TinyMessageBase
    {
        /// <summary>
        /// Initializes a new instance of the MessageBase class.
        /// </summary>
        /// <param name="sender">Message sender (usually "this")</param>
        /// <remarks></remarks>
        public LoggedMessageBase(object sender)
            : base(sender)
        {
            UltrachartDebugLogger.Instance.WriteLine("Publishing {0}, Sender={1}", GetType().Name, sender.GetType().Name);
        }
    }
}