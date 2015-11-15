// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderOperationLayer.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Stores a queue of <see cref="Action"/> operations to perform, intended to be used to queue rendering operations and re-arrange Z-order
    /// </summary>
    /// <seealso cref="RenderLayer"/>
    /// <seealso cref="RenderOperationLayer"/>
    /// <seealso cref="RenderSurfaceBase"/>
    /// <example>
    /// 	<code title="RenderOperationLayers Example" description="Demonstrates how to enqueue operations to the RenderOperationLayers collection and later flush to ensure rendering operations get processed in the correct Z-order" lang="C#">
    /// RenderOperationLayers layers = renderContext.Layers;
    ///  
    /// // Enqueue some operations in the layers in any order
    /// layers[RenderLayer.AxisMajorGridlines].Enqueue(() =&gt; renderContext.DrawLine(/* .. */));
    /// layers[RenderLayer.AxisBands].Enqueue(() =&gt; renderContext.DrawRectangle(/* .. */));
    /// layers[RenderLayer.AxisMinorGridlines].Enqueue(() =&gt; renderContext.DrawLine(/* .. */));
    ///  
    /// // Processes all layers by executing enqueued operations in order of adding, 
    /// // and in Z-order of layers
    /// layers.Flush();</code>
    /// </example>
    public class RenderOperationLayer
    {
        private readonly List<Action> operations = new List<Action>(); 

        /// <summary>
        /// Enqueues an operation to the layer
        /// </summary>
        /// <param name="operation">The operation to queue</param>
        public void Enqueue(Action operation)
        {
            operations.Add(operation);
        }

        /// <summary>
        /// Flushes, the layer, which processes all operations and clears the queue
        /// </summary>
        public void Flush()
        {
            foreach (var operation in operations)
            {
                operation();
            }
            operations.Clear();
        }
    }
}