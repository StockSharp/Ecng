// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AdornerBase.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Axes;
using Ecng.Xaml.Charting.Visuals.RenderableSeries;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    abstract class AdornerBase : FrameworkElement, IAnnotationAdorner, IReceiveMouseEvents
    {
        private Canvas _parentCanvas;

        private IAnnotation _adornedElement;
        private IServiceContainer _services;

        protected AdornerBase(IAnnotation adornedElement)
        {
            _adornedElement = adornedElement;

            Canvas.SetZIndex(this, 100);
        }        

        /// <summary>
        /// Gets the <see cref="ServiceContainer"/> which provides access to services throughout Ultrachart. 
        /// ServiceContainers are created one per <see cref="UltrachartSurface"/> instance, 
        /// and shared between peripheral components such as <see cref="AxisBase"/>, <see cref="BaseRenderableSeries"/>, <see cref="ChartModifierBase"/> instances.
        /// For a full list of available services, see the remarks on <see cref="ServiceContainer"/>
        /// </summary>
        public IServiceContainer Services
        {
            get { return _services; }
            set
            {
                if (_services != null)
                {
                    _services.GetService<IMouseManager>().Unsubscribe(this);
                }

                _services = value;

                if (_services != null)
                {
                    _services.GetService<IMouseManager>().Subscribe(_adornedElement, this);
                }
            }
        }

        /// <summary>
        /// Gets or sets the parent <see cref="UltrachartSurface"/> to perform operations on
        /// </summary>
        /// <value>The parent surface.</value>
        /// <remarks></remarks>
        public Canvas ParentCanvas
        {
            get { return _parentCanvas; }
            set
            {
                if (_parentCanvas != null)
                {
                    OnDetached();
                }

                _parentCanvas = value;

                if (_parentCanvas != null)
                {
                    OnAttached();
                }
            }
        }

        public string MouseEventGroup
        {
            get;
            set;
        }

        public IAnnotation AdornedAnnotation
        {
            get { return _adornedElement; }
        }

        public bool CanReceiveMouseEvents()
        {
            return IsEnabled && ParentCanvas != null;
        }

        public virtual void OnAttached()
        {
            _parentCanvas.Children.Add(this);            
            Initialize();
        }

        public virtual void OnDetached()
        {
            Clear();
            ParentCanvas.Children.Remove(this);
            Services = null;
        }

        public Point GetPointRelativeToRoot(Point point)
        {
            var adornedElement = AdornedAnnotation as UIElement;
            if (adornedElement == null)
                return point;

            var rootGrid = AdornedAnnotation.ParentSurface.RootGrid as UIElement;
            var result = adornedElement.TranslatePoint(point, rootGrid);
            return result;
        }

        /// <summary>
        /// Called when a Mouse DoubleClick occurs on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierDoubleClick(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when a Mouse Button is pressed on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseDown(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when the Mouse is moved on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse move operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseMove(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when a Mouse Button is released on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse button operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseUp(ModifierMouseArgs e) { }

        /// <summary>
        /// Called when the Mouse Wheel is scrolled on the parent <see cref="UltrachartSurface"/>
        /// </summary>
        /// <param name="e">Arguments detailing the mouse wheel operation</param>
        /// <remarks></remarks>
        public virtual void OnModifierMouseWheel(ModifierMouseArgs e) { }

        public void OnModifierTouchDown(ModifierTouchManipulationArgs e)
        {
            throw new System.NotImplementedException();
        }

        public void OnModifierTouchMove(ModifierTouchManipulationArgs e)
        {
            throw new System.NotImplementedException();
        }

        public void OnModifierTouchUp(ModifierTouchManipulationArgs e)
        {
            throw new System.NotImplementedException();
        }

        public void OnMasterMouseLeave(ModifierMouseArgs e) { }

        /// <summary>
        /// Gets or sets whether this Adorner is enabled. 
        /// </summary>
        public 
#if !SILVERLIGHT
            new
#endif
            bool IsEnabled
        {
            get { return AdornedAnnotation.IsSelected; }
            set { AdornedAnnotation.IsSelected = value; }
        }

        public abstract void Initialize();

        public abstract void UpdatePositions();

        public abstract void Clear();
    }
}
