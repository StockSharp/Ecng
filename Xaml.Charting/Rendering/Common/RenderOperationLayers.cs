// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// RenderOperationLayers.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Collections.Generic;

namespace Ecng.Xaml.Charting.Rendering.Common
{
    /// <summary>
    /// Enumeration Constants to define the layers in <see cref="RenderOperationLayers"/>
    /// </summary>
    public enum RenderLayer
    {
        /// <summary>
        /// The Axis Bands render layer, Z-order = 0
        /// </summary>
        AxisBands,

        /// <summary>
        /// The Axis Minor Gridlines render layer, Z-order = 1
        /// </summary>
        AxisMinorGridlines,

        /// <summary>
        /// The Axis Major Gridlines render layer, Z-order = 2
        /// </summary>
        AxisMajorGridlines,

        /// <summary>
        /// The RenderableSeries render layer, Z-order = 3
        /// </summary>
        RenderableSeries
    }

    /// <summary>
    /// A collection of <see cref="RenderOperationLayer"/> layers, which allow rendering operations to be posted to a layered queue for later
    /// execution in order (and correct Z-ordering). 
    /// </summary>
    /// <seealso cref="RenderLayer"></seealso>
    /// <seealso cref="RenderOperationLayer"></seealso>
    /// <seealso cref="RenderSurfaceBase"></seealso>
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
    public class RenderOperationLayers : IEnumerable<RenderOperationLayer>
    {
        private readonly IDictionary<RenderLayer, RenderOperationLayer> _layers = new Dictionary
            <RenderLayer, RenderOperationLayer>()
            {
                {RenderLayer.AxisBands, new RenderOperationLayer()},
                {RenderLayer.AxisMinorGridlines, new RenderOperationLayer()},
                {RenderLayer.AxisMajorGridlines, new RenderOperationLayer()},
                {RenderLayer.RenderableSeries, new RenderOperationLayer()},
            };

        /// <summary>
        /// Gets the <see cref="RenderOperationLayer" /> with the specified <see cref="RenderLayer"/>.
        /// </summary>
        /// <value>
        /// The <see cref="RenderOperationLayer" />.
        /// </value>
        /// <param name="layer">The layer to get.</param>
        /// <returns></returns>
        public RenderOperationLayer this[RenderLayer layer]
        {
            get { return _layers[layer]; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<RenderOperationLayer> GetEnumerator()
        {
            return _layers.Values.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Flushes the <see cref="RenderOperationLayer"/> collection, processing and executing all render operations according to the
        /// Z-order defined by the <see cref="RenderLayer"/> enumeration
        /// </summary>
        public void Flush()
        {
            foreach (RenderOperationLayer layer in this)
            {
                layer.Flush();
            }
        }
    }
}