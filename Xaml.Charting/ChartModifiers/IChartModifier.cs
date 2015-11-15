// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// IChartModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.Specialized;
using System.Windows;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Defines the interface to a <see cref="ChartModifierBase"/>, which can be used to extend the interactivity or rendering of the <see cref="UltrachartSurface"/>
    /// </summary>
    public interface IChartModifier : IChartModifierBase
    {
        /// <summary>
        /// Gets or sets the parent <see cref="UltrachartSurface"/> to perform operations on 
        /// </summary>
        IUltrachartSurface ParentSurface { get; set; }

        /// <summary>
        /// Gets the XAxis <see cref="IAxis"/> instance on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        IAxis XAxis { get; }

        /// <summary>
        /// Returns the YAxes on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        IEnumerable<IAxis> YAxes { get; }

        /// <summary>
        /// Gets the primary YAxis, this is the first axis in the YAxes collection
        /// </summary>
        IAxis YAxis { get; }

        /// <summary>
        /// Gets the YAxis <see cref="IAxis"/> instance on the parent <see cref="UltrachartSurface"/> with the specified Id
        /// </summary>
        /// <param name="axisId">The Id of the axis to get</param>
        /// <returns>The Axis instance</returns>
        IAxis GetYAxis(string axisId);

        /// <summary>
        /// Gets whether the mouse point is within the bounds of the hit-testable element. Assumes the mouse-point has not been translated yet (performs translation)
        /// </summary>
        /// <param name="mousePoint"></param>
        /// <param name="hitTestable"></param>
        /// <returns></returns>
        bool IsPointWithinBounds(Point mousePoint, IHitTestable hitTestable);

        /// <summary>
        /// Instantly stops any inertia that can be associated with this modifier.
        /// </summary>
        void ResetInertia();

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.XAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        void OnXAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args);

        /// <summary>
        /// Called with the <see cref="UltrachartSurface.XAxes" /> <see cref="AxisCollection" /> changes
        /// </summary>
        void OnYAxesCollectionChanged(object sender, NotifyCollectionChangedEventArgs args);

        /// <summary>
        /// Called when the AnnotationCollection changes
        /// </summary>
        void OnAnnotationCollectionChanged(object sender, NotifyCollectionChangedEventArgs args);
    }
}