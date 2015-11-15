// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MainGrid.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Utility.Mouse;
using Ecng.Xaml.Charting.Visuals.Events;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Defines the interface to the Maingrid, a root grid which hosts the <see cref="UltrachartSurface"/>
    /// </summary>
    public interface IMainGrid : IPublishMouseEvents, IHitTestable
    {
        void UnregisterEventsOnShutdown();
    }

    /// <summary>
    /// Defines the Maingrid, a root grid which hosts the <see cref="UltrachartSurface"/>
    /// </summary>
    public class MainGrid : Grid, IMainGrid
    {
        /// <summary>
        /// Occurs when an input device begins a manipulation on the <see cref="T:System.Windows.UIElement" />.
        /// </summary>
        public new event EventHandler<TouchManipulationEventArgs> TouchDown;

        /// <summary>
        /// Occurs when an input device changes position during manipulation.
        /// </summary>
        public new event EventHandler<TouchManipulationEventArgs> TouchMove;

        /// <summary>
        /// Occurs when a manipulation and inertia on the <see cref="T:System.Windows.UIElement" /> object is complete.
        /// </summary>
        public new event EventHandler<TouchManipulationEventArgs> TouchUp;

#if !SILVERLIGHT
        /// <summary>
        /// Occurs when the middle mouse button is pressed while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>.
        /// </summary>
        public event MouseButtonEventHandler MouseMiddleButtonDown;

        /// <summary>
        /// Occurs when the middle mouse button is released while the mouse pointer is over a <see cref="T:System.Windows.UIElement"/>. However, this event will only be raised if a caller marks the preceding <see cref="E:System.Windows.UIElement.MouseRightButtonDown"/> event as handled; see Remarks.
        /// </summary>
        public event MouseButtonEventHandler MouseMiddleButtonUp;

        private void PreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                var handler = MouseMiddleButtonUp;
                if (handler != null) handler(sender, e);
            }
        }

        private void PreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                var handler = MouseMiddleButtonDown;
                if (handler != null) handler(sender, e);
            }
        }
#endif

        readonly IList<TouchPoint> _downPoints = new List<TouchPoint>();
        readonly IList<TouchPoint> _upPoints = new List<TouchPoint>();
        readonly IList<TouchPoint> _movePoints = new List<TouchPoint>();
        private readonly RoutedEventHandler _loadedHandler;
        private readonly RoutedEventHandler _unloadedHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainGrid"/> class.
        /// </summary>
        public MainGrid()
        {
#if !SILVERLIGHT
            PreviewMouseDown += new MouseButtonEventHandler(PreviewMouseDownHandler);

            PreviewMouseUp += new MouseButtonEventHandler(PreviewMouseUpHandler);
#endif

            TouchFrameEventHandler handler = OnTouchFrameReported;

            _loadedHandler = (s, a) =>
            {
                Touch.FrameReported -= handler;
                Touch.FrameReported += handler;
            };

            _unloadedHandler = (s, a) =>
            {
                Touch.FrameReported -= handler;
            };

            Loaded += _loadedHandler;
            Unloaded += _unloadedHandler;
        }

        public void UnregisterEventsOnShutdown()
        {
            Touch.FrameReported -= OnTouchFrameReported;

#if !SILVERLIGHT
            // For some unknown reason, unsusbsribe to this event in SL = InvalidOperationException
            Loaded -= _loadedHandler;
            Unloaded -= _unloadedHandler;
#endif
        }

        private void TouchDownHandler(object sender, TouchManipulationEventArgs e)
        {
            var handler = TouchDown;

            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void TouchUpHandler(object sender, TouchManipulationEventArgs e)
        {
            var handler = TouchUp;

            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void TouchMoveHandler(object sender, TouchManipulationEventArgs e)
        {
            var handler = TouchMove;

            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void OnTouchFrameReported(object sender, TouchFrameEventArgs e)
        {
            TouchPointCollection points;
            try
            {
                points = e.GetTouchPoints(this);
            }
            catch (Exception)
            {
                return;
            }

            _downPoints.Clear();
            _upPoints.Clear();
            _movePoints.Clear();
            foreach (var point in points)
            {
                switch (point.Action)
                {
                    case TouchAction.Down:
                        _downPoints.Add(point);
                        break;

                    case TouchAction.Up:
                        _upPoints.Add(point);
                        break;

                    case TouchAction.Move:
                        _movePoints.Add(point);
                        break;
                }
            }

            if (_upPoints.Count > 0)
            {
                TouchUpHandler(sender, new TouchManipulationEventArgs(_upPoints));
            }

            if (_movePoints.Count > 0)
            {
                TouchMoveHandler(sender, new TouchManipulationEventArgs(_movePoints));
            }

            if (_downPoints.Count > 0)
            {
                TouchDownHandler(sender, new TouchManipulationEventArgs(_downPoints));
            }
        }

        /// <summary>
        /// Translates the point relative to the other hittestable element
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Point TranslatePoint(Point point, IHitTestable relativeTo)
        {
            return ElementExtensions.TranslatePoint(this, point, relativeTo);
        }

        /// <summary>
        /// Returns true if the Point is within the bounds of the current HitTestable element
        /// </summary>
        /// <param name="point">The point to test</param>
        /// <returns>true if the Point is within the bounds</returns>
        /// <remarks></remarks>
        public bool IsPointWithinBounds(Point point)
        {
            return HitTestableExtensions.IsPointWithinBounds(this, point);
        }

        /// <summary>
        /// Gets the bounds of the current HitTestable element relative to another HitTestable element
        /// </summary>
        /// <param name="relativeTo">The relative to.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        public Rect GetBoundsRelativeTo(IHitTestable relativeTo)
        {
            return ElementExtensions.GetBoundsRelativeTo(this, relativeTo);
        }
    }
}