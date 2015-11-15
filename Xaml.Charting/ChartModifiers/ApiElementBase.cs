// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ApiElementBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;
using Ecng.Xaml.Charting.Visuals.Annotations;
using Ecng.Xaml.Charting.Visuals.Axes;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// Base class to expose properties and methods for <see cref="AnnotationBase"/> derived types and <see cref="ChartModifierBase"/> derived types
    /// </summary>
    public abstract class ApiElementBase : ContentControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property changes in the <see cref="INotifyPropertyChanged"/> implementation
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private IUltrachartSurface _parentSurface;

        /// <summary>
        /// Gets or sets the parent <see cref="UltrachartSurface"/> to perform operations on 
        /// </summary>
        public virtual IUltrachartSurface ParentSurface
        {
            get { return _parentSurface; }
            set
            {
                _parentSurface = value;
                OnPropertyChanged("ParentSurface");
            }
        }

        /// <summary>
        /// Returns the XAxes on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        public IEnumerable<IAxis> XAxes
        {
            get { return ParentSurface != null && ParentSurface.XAxes != null ? ParentSurface.XAxes : Enumerable.Empty<IAxis>(); }
        }

        /// <summary>
        /// Returns the YAxes on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        public IEnumerable<IAxis> YAxes
        {
            get { return ParentSurface != null && ParentSurface.YAxes != null ? ParentSurface.YAxes : Enumerable.Empty<IAxis>(); }
        }

        /// <summary>
        /// Gets the primary YAxis instance from the parent <see cref="UltrachartSurface.YAxes"/> collection
        /// </summary>
        public virtual IAxis YAxis
        {
            get
            {
                var scs = ParentSurface;

                IAxis yAxis = null;
                if (scs != null)
                {
                    yAxis = scs.YAxis;

                    if (yAxis == null && !scs.YAxes.IsNullOrEmpty())
                    {
                        yAxis = scs.YAxes.FirstOrDefault(axis => axis.IsPrimaryAxis);
                    }
                }

                return yAxis;               
            }
        }

        /// <summary>
        /// Gets the primary XAxis instance from the parent <see cref="UltrachartSurface.XAxes"/> collection
        /// </summary>
        public virtual IAxis XAxis
        {
            get
            {
                var scs = ParentSurface;

                IAxis xAxis = null;
                if (scs != null)
                {
                    xAxis = scs.XAxis;

                    if (xAxis == null && !scs.XAxes.IsNullOrEmpty())
                    {
                        xAxis = scs.XAxes.FirstOrDefault(axis => axis.IsPrimaryAxis);
                    }
                }

                return xAxis;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="IServiceContainer"/> container
        /// </summary>
        public virtual IServiceContainer Services { get; set; }

        /// <summary>
        /// Gets the <see cref="IChartModifierSurface"/> instance on the parent <see cref="UltrachartSurface"/>, which acts as a canvas to place UIElements
        /// </summary>
        public IChartModifierSurface ModifierSurface { get { return ParentSurface != null ? ParentSurface.ModifierSurface : null; } }

        /// <summary>
        /// Gets or sets whether this Element is attached to a parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <value><c>true</c> if this instance is attached; otherwise, <c>false</c>.</value>
        /// <remarks></remarks>
        public virtual bool IsAttached { get; set; }

        /// <summary>
        /// Gets the <see cref="IMainGrid"/> which is the root component for the <see cref="UltrachartSurface"/>, 
        /// containing the XAxis, YAxes, ModifierSurface, RenderSurface and GridLinesPanel
        /// </summary>
        protected IMainGrid RootGrid { get { return ParentSurface != null ? ParentSurface.RootGrid : null; } }

        /// <summary>
        /// Called when the element is attached to the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public abstract void OnAttached();
      
        /// <summary>
        /// Called immediately before the element is detached from the Chart Surface
        /// </summary>
        /// <remarks></remarks>
        public abstract void OnDetached();

        /// <summary>
        /// Gets the YAxis <see cref="IAxis"/> instance on the parent <see cref="UltrachartSurface"/> at the specified <see cref="AxisAlignment"/>
        /// </summary>
        public IAxis GetYAxis(string axisName)
        {
            if (ParentSurface == null || ParentSurface.YAxes == null)
                return null;

            return ParentSurface.YAxes.GetAxisById(axisName);
        }

        /// <summary>
        /// Gets the XAxis <see cref="IAxis"/> instance on the parent <see cref="UltrachartSurface"/> at the specified <see cref="AxisAlignment"/>
        /// </summary>
        public IAxis GetXAxis(string axisName)
        {
            if (ParentSurface == null || ParentSurface.XAxes == null)
                return null;

            return ParentSurface.XAxes.GetAxisById(axisName);
        }
        
        /// <summary>
        /// Raises the <see cref="InvalidateUltrachartMessage"/> which causes the parent <see cref="UltrachartSurface"/> to invalidate
        /// </summary>
        /// <remarks></remarks>
        protected virtual void OnInvalidateParentSurface()
        {
            if (Services != null)
            {
                Services.GetService<IEventAggregator>().Publish(new InvalidateUltrachartMessage(this));
            }
        }

        /// <summary>
        /// Gets the TemplateChild by the specified name and casts to type <typeparamref name="T" />, asserting that the result is not null
        /// </summary>
        /// <typeparam name="T">The Type of the templated part</typeparam>
        /// <param name="childName">Name of the templated part.</param>
        /// <returns>The template part instance</returns>
        /// <exception cref="System.InvalidOperationException">Unable to Apply the Control Template. Child is missing or of the wrong type</exception>
        protected T GetAndAssertTemplateChild<T>(string childName) where T : class
        {
            var templateChild = GetTemplateChild(childName) as T;

            if (templateChild == null)
            {
                throw new InvalidOperationException(string.Format(
                    "Unable to Apply the Control Template. {0} is missing or of the wrong type", childName));
            }
            return templateChild;
        }

        /// <summary>
        /// Raises the PropertyChanged event, as part of <see cref="INotifyPropertyChanged"/> implementation
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}