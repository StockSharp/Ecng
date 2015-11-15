// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// ChartModifierSurface.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Windows.Media;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Common;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Defines the base interface to the Canvas that <see cref="ChartModifierBase"/> instances interact with
    /// </summary>
    public interface IChartModifierSurface : IHitTestable
    {
        /// <summary>
        /// Gets or sets whether UIElements added to the ModifierSurface should clip to bounds or not
        /// </summary>
        bool ClipToBounds { get; set; }

        /// <summary>
        /// Gets the collection of UIElement children drawn on the canvas
        /// </summary>
        ObservableCollection<UIElement> Children { get; }        
      
        /// <summary>
        /// Clears all children off the <see cref="IChartModifierSurface"/> 
        /// </summary>
        void Clear();

        /// <summary>
        /// Captures the mouse on the <see cref="IChartModifierSurface"/> canvas
        /// </summary>
        /// <returns></returns>
        bool CaptureMouse();

        /// <summary>
        /// Releases the mouse capture on the <see cref="IChartModifierSurface"/> canvas
        /// </summary>
        void ReleaseMouseCapture();
    }

    /// <summary>
    /// Defines the ChartModifierSurface, which acts as an overlay <see cref="Canvas"/> on top of the <see cref="UltrachartSurface"/> for drawing annotations, 
    /// </summary>
    [ContentProperty("Children")]
    public class ChartModifierSurface : ContentControl, IChartModifierSurface
    {
        private readonly Canvas _modifierCanvas = new Canvas();
        private readonly ObservableCollection<UIElement> _children;

        /// <summary>
        /// Defines the ClipToBounds DependencyProperty
        /// </summary>
        public static readonly 
            #if !SILVERLIGHT
            new
            #endif
            DependencyProperty ClipToBoundsProperty = DependencyProperty.Register("ClipToBounds", typeof(bool), typeof(ChartModifierSurface), new PropertyMetadata(false, OnClipToBoundsPropertyChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="ChartModifierSurface"/> class.
        /// </summary>
        /// <remarks></remarks>
        public ChartModifierSurface()
        {            
            _children = new ObservableCollection<UIElement>();
            _children.CollectionChanged += ChildrenCollectionChanged;

            Content = _modifierCanvas;

            //Important! In Silverlight Content doesn't fill all available parent space
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Stretch;
        }

        /// <summary>
        /// Gets or sets whether UIElements added to the ModifierSurface should clip to bounds or not
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

        /// <summary>
        /// Gets the collection of UIElement children drawn on the canvas over the top of the <see cref="UltrachartSurface"/>
        /// </summary>
        /// <remarks></remarks>
        public ObservableCollection<UIElement> Children
        {
            get { return _children; }
        }

        /// <summary>
        /// Clears all children off the <see cref="IChartModifierSurface"/>
        /// </summary>
        /// <remarks></remarks>
        public void Clear()
        {           
            Children.Clear();
        }

        /// <summary>
        /// Translates the point relative to the other <see cref="IHitTestable"/> element
        /// </summary>
        /// <param name="point">The input point relative to this <see cref="IHitTestable"/></param>
        /// <param name="relativeTo">The other <see cref="IHitTestable"/> to use when transforming the point</param>
        /// <returns>The transformed Point</returns>
        /// <remarks></remarks>
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            return ElementExtensions.TranslatePoint(this, point, relativeTo);
        }

        /// <summary>
        /// Returns true if the Point is within the bounds of the current <see cref="IHitTestable"/> element
        /// </summary>
        /// <param name="point">The point to test, translated relative to RootGrid</param>
        /// <returns>true if the Point is within the bounds</returns>
        /// <remarks></remarks>
        public bool IsPointWithinBounds(Point point)
        {
            return HitTestableExtensions.IsPointWithinBounds(this, point);
        }

        /// <summary>
        /// Gets the bounds of the current <see cref="IHitTestable"/> element relative to another <see cref="IHitTestable"/> element
        /// </summary>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
#if SILVERLIGHT
            var relativeVisual = relativeTo as UIElement;
#else
            var relativeVisual = relativeTo as Visual;
#endif
            if (relativeVisual == null)
                return Rect.Empty;

            return TransformToVisual(relativeVisual).TransformBounds(LayoutInformation.GetLayoutSlot(this));
        }

        private void ChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _modifierCanvas.Children.Clear();
                _children.ForEachDo(x => _modifierCanvas.Children.Add(x));
            }

            if (e.NewItems != null)
            {
                e.NewItems.Cast<UIElement>().ForEachDo(x => _modifierCanvas.Children.Add(x));
            }

            if (e.OldItems != null)
            {
                e.OldItems.Cast<UIElement>().ForEachDo(x => _modifierCanvas.Children.Remove(x));
            }
        }

        private static void OnClipToBoundsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ClipToBoundsHelper.SetClipToBounds(((ChartModifierSurface)d)._modifierCanvas, (bool)e.NewValue);
        }

        
        internal Canvas ModifierCanvas { get { return _modifierCanvas; } }

    }    
}