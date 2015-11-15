// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ObjectPool.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Common.Helpers
{
    //Taken from this StackOverflow post: http://stackoverflow.com/a/2564523
    /// <summary>
    /// Represents a pool of objects with a size limit.
    /// </summary>
    /// <typeparam name="T">The type of object in the pool.</typeparam>
    internal class ObjectPool<T> : IDisposable
        where T : new()
    {
        private readonly object _locker;
        private readonly Queue<T> _queue;
        private int _count;
        
        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        public ObjectPool()
        {
            _locker = new object();
            _queue = new Queue<T>();
        }

        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        public ObjectPool(int initAmount, Func<T, T> actionOnCreation): this()
        {
            for (int i = 0; i < initAmount; ++i)
            {
                var instance = actionOnCreation(new T());
                _count++;

                Put(instance);
            }
        }

        /// <summary>
        /// Gets the summary amount of created instances
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        /// <summary>
        /// Gets the amount of pooled instances
        /// </summary>
        public int AvailableCount
        {
            get { return _queue.Count; }
        }

        /// <summary>
        /// Gets the value indicating whether current ObjectPool instance is empty.
        /// </summary>
        public bool IsEmpty { get { return _queue.Count == 0; } }

        /// <summary>
        /// Retrieves an item from the pool. 
        /// </summary>
        /// <returns>The item retrieved from the pool.</returns>
        public T Get()
        {
            return Get(T => T);
        }

        /// <summary>
        /// Retrieves an item from the pool. 
        /// </summary>
        /// <returns>The item retrieved from the pool.</returns>
        public T Get(Func<T,T> actionOnCreation)
        {
            return Get(()=>actionOnCreation(new T()));
        }

        /// <summary>
        /// Retrieves an item from the pool. 
        /// </summary>
        /// <returns>The item retrieved from the pool.</returns>
        public T Get(Func<T> actionOnCreation)
        {
            lock (_locker)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }

                _count++;
                return actionOnCreation();
            }
        }

        /// <summary>
        /// Places an item in the pool.
        /// </summary>
        /// <param name="item">The item to place to the pool.</param>
        public void Put(T item)
        {
            lock (_locker)
            {
                _queue.Enqueue(item);
            }
        }

        /// <summary>
        /// Disposes of items in the pool that implement IDisposable.
        /// </summary>
        public void Dispose()
        {
            lock (_locker)
            {
                _count = 0;
                while (_queue.Count > 0)
                {
                    _queue.Dequeue();
                }
            }
        }
    }
}
