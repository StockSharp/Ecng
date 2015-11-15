// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MultiThreaded.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Text;
using System.Threading;

namespace Ecng.Xaml.Charting.Threading
{
    internal class MultiThreaded
    {
        /// <summary>
        /// Parallel for loop. Invokes given action, passing arguments
        /// fromInclusive - toExclusive on multiple threads.
        /// Returns when loop finished.
        /// </summary>
        internal static void For(int fromInclusive, int toExclusive, Action<int> action)
        {
            // chunkSize = 1 makes items to be processed in order.
            // Bigger chunk size should reduce lock waiting time and thus
            // increase paralelism.

            // number of process() threads
            int threadCount = Environment.ProcessorCount;
            int index = fromInclusive - 1;// -chunkSize;
            // locker object shared by all the process() delegates
            var locker = new object();

            // processing function
            // takes next chunk and processes it using action
            Action process = delegate()
                {
                    int i = 0;
                    while ((i = Interlocked.Increment(ref index)) < toExclusive)
                    {
//                        int chunkStart = 0;
//                        lock (locker)
//                        {
//                            // take next chunk
//                            index += chunkSize;
//                            chunkStart = index;
//                        }
                        // process the chunk
                        // (another thread is processing another chunk 
                        //  so the real order of items will be out-of-order)
                        //for (int i = chunkStart; i < chunkStart + chunkSize; i++)
                       // {
                        //    if (i >= toExclusive) return;
                            action(i);
                       // }
                    }
                };

            // launch process() threads
            IAsyncResult[] asyncResults = new IAsyncResult[threadCount];
            for (int i = 0; i < threadCount; ++i)
            {
                asyncResults[i] = process.BeginInvoke(null, null);
            }
            // wait for all threads to complete
            for (int i = 0; i < threadCount; ++i)
            {
                process.EndInvoke(asyncResults[i]);
            }
        }
    }
}
