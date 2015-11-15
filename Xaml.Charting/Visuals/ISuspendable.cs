// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ISuspendable.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Types which implement ISuspendable can have updates suspended/resumed. Useful for batch operations 
    /// </summary>
    public interface ISuspendable
    {
        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        bool IsSuspended { get; }

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>The disposable Update Suspender</returns>
        IUpdateSuspender SuspendUpdates();

        /// <summary>
        /// Resumes updates on the target, intended to be called by IUpdateSuspender
        /// </summary>
        void ResumeUpdates(IUpdateSuspender suspender);

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        void DecrementSuspend();
    }
}