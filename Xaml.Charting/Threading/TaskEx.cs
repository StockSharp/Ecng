// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// TaskEx.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecng.Xaml.Charting.Threading
{
    internal static class TaskEx
    {
        private static readonly ImmediateScheduler _immediateScheduler = new ImmediateScheduler();
        /// <summary>
        /// Returns a completed task wrapping the Result passed in
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static Task<TResult> FromResult<TResult>(TResult result)
        {
            var taskSource = new TaskCompletionSource<TResult>();
            taskSource.SetResult(result);
            return taskSource.Task;
        }

        internal static TaskScheduler ImmediateScheduler()
        {
            return _immediateScheduler;
        }
    }

    /// <summary>Provides a task scheduler that runs tasks on the current thread.</summary>
    /// <remarks>See http://blogs.msdn.com/b/pfxteam/archive/2010/04/09/9990424.aspx for more info </remarks>
    internal class ImmediateScheduler : TaskScheduler
    {
        /// <summary>Runs the provided Task synchronously on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            TryExecuteTask(task);
        }

        /// <summary>Runs the provided Task synchronously on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the Task was previously queued to the scheduler.</param>
        /// <returns>True if the Task was successfully executed; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTask(task);
        }

        /// <summary>Gets the Tasks currently scheduled to this scheduler.</summary>
        /// <returns>An empty enumerable, as Tasks are never queued, only executed.</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return Enumerable.Empty<Task>();
        }

        /// <summary>Gets the maximum degree of parallelism for this scheduler.</summary>
        public override int MaximumConcurrencyLevel { get { return 1; } }
    }
}
