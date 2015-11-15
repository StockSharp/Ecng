// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UpdateSuspender.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Defines the interface to an <see cref="UpdateSuspender"/>, a disposable class which allows nested suspend/resume operations on an <see cref="ISuspendable"/> target
    /// </summary>
    public interface IUpdateSuspender : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether updates for this instance are currently suspended
        /// </summary>
        bool IsSuspended { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the target will resume when the IUpdateSuspender is disposed. Default is True
        /// </summary>
        bool ResumeTargetOnDispose { get; set; }

        /// <summary>
        /// Gets or sets an associated Tab for this <see cref="IUpdateSuspender"/> instance
        /// </summary>
        object Tag { get; }
    }

    internal class UpdateSuspender : IUpdateSuspender
    {
        private readonly object _tag;
        private readonly ISuspendable _target;
        private static readonly IDictionary<ISuspendable, int> _suspendedInstances = new Dictionary<ISuspendable, int>();
        private static readonly object _syncRoot = new object();

        internal UpdateSuspender(ISuspendable target, object tag) : this(target)
        {
            _tag = tag;
        }

        internal UpdateSuspender(ISuspendable target)
        {
            ResumeTargetOnDispose = true;

            _target = target;

            lock (_syncRoot)
            {
                if (!_suspendedInstances.ContainsKey(_target))
                    _suspendedInstances.Add(_target, 0);

                Inc(_target);
            }
        }

        public bool IsSuspended
        {
            get
            {
                return GetIsSuspended(_target);
            }
        }

        public bool ResumeTargetOnDispose { get; set; }
    
        public void Dispose()
        {
            lock (_syncRoot)
            {
                _target.DecrementSuspend();

                if (Dec(_target) == 0)
                {
                    _suspendedInstances.Remove(_target);
                    _target.ResumeUpdates(this);
                }
            }
        }        

        internal static IDictionary<ISuspendable, int> SuspendedInstances { get { return _suspendedInstances; } }

        public object Tag
        {
            get { return _tag; }
        }

        internal static bool GetIsSuspended(ISuspendable target)
        {
            lock (_syncRoot)
            {
                return _suspendedInstances.ContainsKey(target) && _suspendedInstances[target] != 0;
            }
        }

        private static void Inc(ISuspendable target)
        {
            _suspendedInstances[target]++;
        }

        private static int Dec(ISuspendable target)
        {
            int current = _suspendedInstances[target];
            current--;
            _suspendedInstances[target] = current;
            return current;
        }
    }
}