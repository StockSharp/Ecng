// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ServiceContainer.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting
{
    /// <summary>
    /// Defines the interface to a ServiceContainer used throughout Ultrachart. For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
    /// </summary>
    public interface IServiceContainer
    {
        /// <summary>
        /// Gets the service instance registered by type. For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        /// <typeparam name="T">The type of service to get </typeparam>
        /// <returns>The service instance, unique to this <see cref="UltrachartSurface"/> instance</returns>
        /// <remarks></remarks>
        T GetService<T>();

        /// <summary>
        /// Registers the service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        /// <remarks></remarks>
        void RegisterService<T>(T instance);

        void DeRegisterService<T>();
    }

    /// <summary>
    /// Provides access to services throughout Ultrachart. ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
    /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
    /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
    /// </summary>
    /// <remarks>Available Services include:
    /// <list type="bullet">
    ///     <item><see cref="IMouseManager"/>, for subscription to mouse events</item>
    ///     <item><see cref="IUltrachartRenderer"/>, for handling of the rendering pipeline</item>
    ///     <item>
    ///         <see cref="IEventAggregator"/>, with event types as follows:
    ///         <list type="bullet">
    ///             <item><see cref="InvalidateUltrachartMessage"/></item>
    ///             <item><see cref="ZoomExtentsMessage"/></item>
    ///             <item><see cref="UltrachartResizedMessage"/></item>
    ///             <item><see cref="UltrachartRenderedMessage"/></item>
    ///         </list>
    ///     </item>
    /// </list>
    /// </remarks>
    public class ServiceContainer : IServiceContainer
    {
        private readonly IDictionary<Type, object> _serviceInstances = new Dictionary<Type, object>();

        internal bool HasService<T>()
        {
            return _serviceInstances.ContainsKey(typeof (T));
        }

        /// <summary>
        /// Registers the service.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance">The instance.</param>
        /// <remarks></remarks>
        public void RegisterService<T>(T instance)
        {
            _serviceInstances[typeof(T)] = instance;
        }

        public void DeRegisterService<T>()
        {
            _serviceInstances.Remove(typeof (T));
        }

        /// <summary>
        /// Gets the service instance registered by type. For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        /// <typeparam name="T">The type of service to get</typeparam>
        /// <returns>The service instance, unique to this <see cref="UltrachartSurface"/> instance</returns>
        /// <remarks></remarks>
        public T GetService<T>()
        {
            Type type = typeof (T);
            if (!HasService<T>())
            {
                throw new Exception(string.Format("The service instance of type {0} has not been registered with the container", type));
            }

            return (T)_serviceInstances[type];
        }
    }
}