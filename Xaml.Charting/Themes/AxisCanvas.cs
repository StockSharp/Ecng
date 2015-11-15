// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AxisCanvas.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.Themes
{
    /// <summary>
    /// The AxisCanvas provides an auto-sizing canvas for the axis labels
    /// </summary>
    /// <remarks>
    /// Under MS-PL from http://themechanicalbride.blogspot.com/2008/11/auto-sizing-canvas-for-silverlight-and.html.
    /// </remarks>
    public class AxisCanvas : Panel, ISuspendable
    {
        
        /// <summary>
        /// Identifies the SizeWidthToContent dependency property.
        /// </summary>
        public static readonly 
#if !SILVERLIGHT
            new 
#endif
            DependencyProperty ClipToBoundsProperty = DependencyProperty.Register("ClipToBounds", typeof(bool), typeof(AxisCanvas), new PropertyMetadata(false, OnClipToBoundsChanged));

        /// <summary>
        /// Identifies the SizeWidthToContent dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeWidthToContentProperty = DependencyProperty.Register("SizeWidthToContent", typeof (bool), typeof (AxisCanvas), new PropertyMetadata(false, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the SizeHeightToContent dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeHeightToContentProperty = DependencyProperty.Register("SizeHeightToContent", typeof(bool), typeof(AxisCanvas), new PropertyMetadata(false, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the Left dependency property.
        /// </summary>
        public static readonly DependencyProperty LeftProperty = DependencyProperty.RegisterAttached("Left", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the Right dependency property.
        /// </summary>
        public static readonly DependencyProperty RightProperty = DependencyProperty.RegisterAttached("Right", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the Top dependency property.
        /// </summary>
        public static readonly DependencyProperty TopProperty = DependencyProperty.RegisterAttached("Top", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the Bottom dependency property.
        /// </summary>
        public static readonly DependencyProperty BottomProperty = DependencyProperty.RegisterAttached("Bottom", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));
        
        /// <summary>
        /// Identifies the CenterLeft dependency property.
        /// </summary>
        public static readonly DependencyProperty CenterLeftProperty = DependencyProperty.RegisterAttached("CenterLeft", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the CenterRight dependency property.
        /// </summary>
        public static readonly DependencyProperty CenterRightProperty = DependencyProperty.RegisterAttached("CenterRight", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the CenterTop dependency property.
        /// </summary>
        public static readonly DependencyProperty CenterTopProperty = DependencyProperty.RegisterAttached("CenterTop", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Identifies the CenterBottom dependency property.
        /// </summary>
        public static readonly DependencyProperty CenterBottomProperty = DependencyProperty.RegisterAttached("CenterBottom", typeof(double), typeof(AxisCanvas), new PropertyMetadata(double.NaN, OnRenderablePropertyChanged));

        /// <summary>
        /// Gets the value of the Left attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The Left property value for the UIElement.</returns>
        public static double GetLeft(UIElement element)
        {
            return (double)element.GetValue(LeftProperty);
        }

        /// <summary>
        /// Sets the value of the Left attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed Left value.</param>
        public static void SetLeft(UIElement element, double value)
        {
            element.SetValue(LeftProperty, value);
        }

        /// <summary>
        /// Gets the value of the Right attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The Right property value for the UIElement.</returns>
        public static double GetRight(UIElement element)
        {
            return (double)element.GetValue(RightProperty);
        }

        /// <summary>
        /// Sets the value of the Right attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed Right value.</param>
        public static void SetRight(UIElement element, double value)
        {
            element.SetValue(RightProperty, value);
        }
        
        /// <summary>
        /// Gets the value of the Top attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The Top property value for the UIElement.</returns>
        public static double GetTop(UIElement element)
        {
            return (double)element.GetValue(TopProperty);
        }

        /// <summary>
        /// Sets the value of the Top attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed Top value.</param>
        public static void SetTop(UIElement element, double value)
        {
            element.SetValue(TopProperty, value);
        }

        /// <summary>
        /// Gets the value of the Bottom attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The Bottom property value for the UIElement.</returns>
        public static double GetBottom(UIElement element)
        {
            return (double)element.GetValue(BottomProperty);
        }

        /// <summary>
        /// Sets the value of the Bottom attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed Bottom value.</param>
        public static void SetBottom(UIElement element, double value)
        {
            element.SetValue(BottomProperty, value);
        }

        /// <summary>
        /// Gets the value of the CenterLeft attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The CenterLeft property value for the UIElement.</returns>
        public static double GetCenterLeft(UIElement element)
        {
            return (double)element.GetValue(CenterLeftProperty);
        }

        /// <summary>
        /// Sets the value of the CenterLeft attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed CenterLeft value.</param>
        public static void SetCenterLeft(UIElement element, double value)
        {
            element.SetValue(CenterLeftProperty, value);
        }

        /// <summary>
        /// Gets the value of the CenterRight attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The CenterRight property value for the UIElement.</returns>
        public static double GetCenterRight(UIElement element)
        {
            return (double)element.GetValue(CenterRightProperty);
        }

        /// <summary>
        /// Sets the value of the CenterRight attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed CenterRight value.</param>
        public static void SetCenterRight(UIElement element, double value)
        {
            element.SetValue(CenterRightProperty, value);
        }

        /// <summary>
        /// Gets the value of the CenterTop attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The CenterTop property value for the UIElement.</returns>
        public static double GetCenterTop(UIElement element)
        {
            return (double)element.GetValue(CenterTopProperty);
        }

        /// <summary>
        /// Sets the value of the CenterTop attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed CenterTop value.</param>
        public static void SetCenterTop(UIElement element, double value)
        {
            element.SetValue(CenterTopProperty, value);
        }

        /// <summary>
        /// Gets the value of the CenterBottom attached property for a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement from which the property value is read.</param>
        /// <returns>The CenterBottom property value for the UIElement.</returns>
        public static double GetCenterBottom(UIElement element)
        {
            return (double)element.GetValue(CenterBottomProperty);
        }

        /// <summary>
        /// Sets the value of the CenterBottom attached property to a specified UIElement.
        /// </summary>
        /// <param name="element">The UIElement to which the attached property is written.</param>
        /// <param name="value">The needed CenterBottom value.</param>
        public static void SetCenterBottom(UIElement element, double value)
        {
            element.SetValue(CenterBottomProperty, value);
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether the dynamic canvas should
        /// size its width to its content.
        /// </summary>
        public bool SizeWidthToContent
        {
            get { return (bool)GetValue(SizeWidthToContentProperty); }
            set { SetValue(SizeWidthToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the canvas should size its
        /// height to its content.
        /// </summary>
        public bool SizeHeightToContent
        {
            get { return (bool)GetValue(SizeHeightToContentProperty); }
            set { SetValue(SizeHeightToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value which indicates whether to clip the content of this element.
        /// </summary>
        public
#if !SILVERLIGHT
            new
#endif
            bool ClipToBounds
        {
            get { return (bool)GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

#if !SILVERLIGHT
        /// <summary>
        /// Returns a geometry for a clipping mask. The mask applies if the layout system attempts to arrange an element that is larger than the available display space.
        /// </summary>
        /// <param name="layoutSlotSize">The size of the part of the element that does visual presentation.</param>
        /// <returns>
        /// The clipping geometry.
        /// </returns>
        protected override Geometry GetLayoutClip(Size layoutSlotSize)
        {
            return ClipToBounds ? base.GetLayoutClip(layoutSlotSize) : null;
        }
#endif

        /// <summary>
        /// Invalidates the position of child elements.
        /// </summary>
        private void Invalidate()
        {
            if (IsSuspended) return;

            if (SizeHeightToContent || SizeWidthToContent)
            {
                InvalidateMeasure();
            }
            else
            {
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Measures all the children and returns their size.
        /// </summary>
        /// <param name="constraint">The available size.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size constraint)
        {
            var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

            foreach (UIElement child in Children)
            {
                child.Measure(availableSize);
            }

            double maxWidth = 0;
            double maxHeight = 0;
            if (SizeHeightToContent || SizeWidthToContent)
            {
                var visualChildren = Children.OfType<UIElement>().ToArray();

                if (SizeWidthToContent && !visualChildren.IsNullOrEmpty())
                {
                    maxWidth =
                        visualChildren
                            .Where(child => !GetLeft(child).IsNaN())
                            .Select(child => GetLeft(child) + child.DesiredSize.Width)
                            .Concat(visualChildren
                                .Where(child => !GetCenterLeft(child).IsNaN())
                                .Select(child => GetCenterLeft(child) + (child.DesiredSize.Width/2))).MaxOrNullable() ??
                        visualChildren.Max(child => child.DesiredSize.Width);


                    double maxRightOffset = visualChildren
                        .Where(child => !GetRight(child).IsNaN())
                        .Select(child => (maxWidth - GetRight(child)) - child.DesiredSize.Width)
                        .Concat(visualChildren
                            .Where(child => !GetCenterRight(child).IsNaN())
                            .Select(child => (maxWidth - GetCenterRight(child)) - (child.DesiredSize.Width/2)))
                        .MinOrNullable() ?? 0.0;


                    if (maxRightOffset < 0.0)
                    {
                        maxWidth += Math.Abs(maxRightOffset);
                    }
                }

                if (SizeHeightToContent && !visualChildren.IsNullOrEmpty())
                {
                    maxHeight =
                        visualChildren
                            .Where(child => !GetTop(child).IsNaN())
                            .Select(child => GetTop(child) + child.DesiredSize.Height)
                            .Concat(visualChildren
                                .Where(child => !GetCenterTop(child).IsNaN())
                                .Select(child => GetCenterTop(child) + (child.DesiredSize.Height/2)))
                            .MaxOrNullable() ?? visualChildren.Max(child => child.DesiredSize.Height);

                    double maxBottomOffset =
                        visualChildren
                            .Where(child => !GetBottom(child).IsNaN())
                            .Select(child => (maxHeight - GetBottom(child)) - child.DesiredSize.Height)
                            .Concat(visualChildren
                                .Where(child => !GetCenterBottom(child).IsNaN())
                                .Select(child => (maxHeight - GetCenterBottom(child)) - (child.DesiredSize.Height/2)))
                            .MinOrNullable() ?? 0.0;

                    if (maxBottomOffset < 0.0)
                    {
                        maxHeight += Math.Abs(maxBottomOffset);
                    }
                }
            }

            availableSize = new Size(Math.Max(maxWidth, 0), Math.Max(maxHeight, 0));

            return availableSize;
        }

        /// <summary>
        /// Arranges all children in the correct position.
        /// </summary>
        /// <param name="arrangeSize">The size to arrange element's within.
        /// </param>
        /// <returns>The size that element's were arranged in.</returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            foreach (UIElement element in Children)
            {
                var arrangedRect = GetArrangedRect(arrangeSize, element);

                element.Arrange(arrangedRect);
            }

            return arrangeSize;
        }

        /// <summary>
        /// Get arranged rectangle of element
        /// </summary>
        /// <param name="arrangeSize">The size to arrange element within</param>
        /// <param name="element">The element which need to be arranged</param>
        /// <returns>Arranged Rect of element</returns>
        protected virtual Rect GetArrangedRect(Size arrangeSize, UIElement element)
        {
            double x = 0.0;
            double y = 0.0;
            double left = GetLeft(element);
            double centerLeft = GetCenterLeft(element);
            double halfWidth = (element.DesiredSize.Width/2.0);
            if (!left.IsNaN())
            {
                x = left;
            }
            else if (!centerLeft.IsNaN())
            {
                x = centerLeft - halfWidth;
            }
            else
            {
                double right = GetRight(element);
                if (!right.IsNaN())
                {
                    x = (arrangeSize.Width - element.DesiredSize.Width) - right;
                }
                else
                {
                    double centerRight = GetCenterRight(element);
                    if (!centerRight.IsNaN())
                    {
                        x = (arrangeSize.Width - halfWidth) - centerRight;
                    }
                }
            }

            double top = GetTop(element);
            double centerTop = GetCenterTop(element);
            double halfHeight = (element.DesiredSize.Height/2.0);
            if (!top.IsNaN())
            {
                y = top;
            }
            else if (!centerTop.IsNaN())
            {
                y = centerTop - halfHeight;
            }
            else
            {
                double bottom = GetBottom(element);
                if (!bottom.IsNaN())
                {
                    y = (arrangeSize.Height - element.DesiredSize.Height) - bottom;
                }
                else
                {
                    double centerBottom = GetCenterBottom(element);
                    if (!centerBottom.IsNaN())
                    {
                        y = (arrangeSize.Height - halfHeight) - centerBottom;
                    }
                }
            }

            return AdjustArrangedRectPosition(new Rect(new Point(x, y), element.DesiredSize), arrangeSize);
        }

        /// <summary>
        /// Adjust position of element before arranging
        /// </summary>
        /// <param name="arrangedRect">Rect of current element</param>
        /// <param name="arrangeSize">The size to arrange element within</param>
        /// <returns></returns>
        protected virtual Rect AdjustArrangedRectPosition(Rect arrangedRect, Size arrangeSize)
        {
            return arrangedRect;
        }

        /// <summary>
        /// Gets a value indicating whether updates for the target are currently suspended
        /// </summary>
        public bool IsSuspended
        {
            get { return UpdateSuspender.GetIsSuspended(this); }
        }

        /// <summary>
        /// Suspends drawing updates on the target until the returned object is disposed, when a final draw call will be issued
        /// </summary>
        /// <returns>
        /// The disposable Update Suspender
        /// </returns>
        public IUpdateSuspender SuspendUpdates()
        {
            return new UpdateSuspender(this){ResumeTargetOnDispose = false};
        }

        /// <summary>
        /// Called by IUpdateSuspender each time a target suspender is disposed. When the final
        /// target suspender has been disposed, ResumeUpdates is called
        /// </summary>
        public void DecrementSuspend() { }

        /// <summary>
        /// Resumes updates on the target, intended to be called by IUpdateSuspender
        /// </summary>
        /// <param name="suspender"></param>
        public void ResumeUpdates(IUpdateSuspender suspender)
        {
            if (suspender.ResumeTargetOnDispose)
            {
                Invalidate();
            }
        }

        private static void OnClipToBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
#if SILVERLIGHT
            var axisCanvas = d as AxisCanvas;
            if (axisCanvas != null)
            {
                //TODO: Add implementation for clipping in Sl if needed
                axisCanvas.Clip = (bool)e.NewValue ? axisCanvas.Clip : null;
            }
#endif
        }

        private static void OnRenderablePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var axisCanvas = ((FrameworkElement)d).Parent as AxisCanvas;
            if (axisCanvas != null)
            {
                axisCanvas.Invalidate();
            }
        }
    }
}
