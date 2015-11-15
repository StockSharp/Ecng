// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// AnchorPointAnnotation.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals.Events;

namespace Ecng.Xaml.Charting.Visuals.Annotations
{
    /// <summary>
    /// An Anchor-Point annotation is an <see cref="IAnnotation"/> which only has one X1,Y1 point. 
    /// This annotation may be anchored around the coordinate using various alignmnets. See the <see cref="HorizontalAnchorPoint"/> and <see cref="VerticalAnchorPoint"/> properties
    /// for more information
    /// </summary>
    public abstract class AnchorPointAnnotation : AnnotationBase, IAnchorPointAnnotation
    {
        event EventHandler<TouchManipulationEventArgs> IPublishMouseEvents.TouchDown
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventHandler<TouchManipulationEventArgs> IPublishMouseEvents.TouchMove
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        event EventHandler<TouchManipulationEventArgs> IPublishMouseEvents.TouchUp
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Defines the HorizontalAnchorPoint DependencyProperty
        /// </summary>
        public static readonly DependencyProperty HorizontalAnchorPointProperty = DependencyProperty.Register("HorizontalAnchorPoint", typeof(HorizontalAnchorPoint), typeof(AnchorPointAnnotation), new PropertyMetadata(HorizontalAnchorPoint.Left, OnAnchorPointChanged));
        /// <summary>
        /// Defines the VerticalAnchorPointProperty DependencyProperty
        /// </summary>
        public static readonly DependencyProperty VerticalAnchorPointProperty = DependencyProperty.Register("VerticalAnchorPoint", typeof(VerticalAnchorPoint), typeof(AnchorPointAnnotation), new PropertyMetadata(VerticalAnchorPoint.Top, OnAnchorPointChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="AnchorPointAnnotation" /> class.
        /// </summary>
        protected AnchorPointAnnotation()
        {
            IsResizable = false;
        }

        /// <summary>
        /// Gets or sets the <see cref="HorizontalAnchorPoint" />.
        /// The value of Left means the X1,Y1 coordinate of the annotation is on the Left horizontally.
        /// The value of Center means the X1,Y1 coordinate of the annotation is at the center horizontally.
        /// The value of Right means the X1,Y1 coordinate of the annotation is at the right horizontally.
        /// </summary>
        public HorizontalAnchorPoint HorizontalAnchorPoint
        {
            get { return (HorizontalAnchorPoint)GetValue(HorizontalAnchorPointProperty); }
            set { SetValue(HorizontalAnchorPointProperty, value); }
        }

        /// <summary>
        /// Gets or sets the <see cref="VerticalAnchorPoint" />.
        /// The value of Top means the X1,Y1 coordinate of the annotation is on the Top vertically.
        /// The value of Center means the X1,Y1 coordinate of the annotation is at the center vertically.
        /// The value of Bottom means the X1,Y1 coordinate of the annotation is at the Bottom vertically.
        /// </summary>
        public VerticalAnchorPoint VerticalAnchorPoint
        {
            get { return (VerticalAnchorPoint)GetValue(VerticalAnchorPointProperty); }
            set { SetValue(VerticalAnchorPointProperty, value); }
        }

        /// <summary>
        /// Gets the computed VerticalOffset in pixels to apply to this annotation when placing
        /// </summary>
        public double VerticalOffset
        {
            get
            {
                if (AnnotationRoot != null)
                {
                    if (VerticalAnchorPoint == VerticalAnchorPoint.Top)
                        return 0;
                    if (VerticalAnchorPoint == VerticalAnchorPoint.Center)
                        return AnnotationRoot.ActualHeight*0.5;
                    if (VerticalAnchorPoint == VerticalAnchorPoint.Bottom)
                        return AnnotationRoot.ActualHeight;
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets the computed HorizontalOffset in pixels to apply to this annotation when placing
        /// </summary>
        public double HorizontalOffset
        {
            get
            {
                if (AnnotationRoot != null)
                {
                    if (HorizontalAnchorPoint == HorizontalAnchorPoint.Left)
                        return 0;
                    if (HorizontalAnchorPoint == HorizontalAnchorPoint.Center)
                        return AnnotationRoot.ActualWidth*0.5;
                    if (HorizontalAnchorPoint == HorizontalAnchorPoint.Right)
                        return AnnotationRoot.ActualWidth;
                }

                return 0;
            }
        }

        private static void OnAnchorPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = ((AnchorPointAnnotation)d);

            var dispatcherInstance = annotation.Dispatcher;

            Action action = () => OnRenderablePropertyChanged(d, e);

#if !SILVERLIGHT
            dispatcherInstance.BeginInvoke(action, DispatcherPriority.DataBind);
#else
            dispatcherInstance.BeginInvoke(action);
#endif
        }

        /// <summary>
        /// Applies <see cref="HorizontalOffset"/> and <see cref="VerticalOffset"/> to annotationCoordinates
        /// </summary>
        /// <param name="annotationCoordinates"></param>
        /// <returns></returns>
        protected AnnotationCoordinates GetAnchorAnnotationCoordinates(AnnotationCoordinates annotationCoordinates)
        {
            annotationCoordinates.X1Coord -= HorizontalOffset;
            annotationCoordinates.Y1Coord -= VerticalOffset;

            annotationCoordinates.X2Coord -= HorizontalOffset;
            annotationCoordinates.Y2Coord -= VerticalOffset;
            
            return annotationCoordinates;
        }

        /// <summary>
        /// Gets the <see cref="Cursor" /> to use for the annotation when selected
        /// </summary>
        /// <returns></returns>
        protected override Cursor GetSelectedCursor()
        {
#if SILVERLIGHT
            return Cursors.Hand;
#else
            return Cursors.SizeAll;
#endif
        }
    }
}