// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// MouseManager.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Ecng.Xaml.Charting.ChartModifiers;
using Ecng.Xaml.Charting.Common.Extensions;
using Ecng.Xaml.Charting.Visuals.Events;

namespace Ecng.Xaml.Charting.Utility.Mouse
{
    /// <summary>
    /// Defines the interface to the MouseManager, a cross-platform helper class to propagate mouse events in both Silverlight and WPF 
    /// </summary>
    public interface IMouseManager
    {
        /// <summary>
        /// Subscribes to mouse events on the Source, propagating handlers to the Target
        /// </summary>
        /// <param name="source">The source of mouse events</param>
        /// <param name="target">The target to receive mouse event handlers</param>
        void Subscribe(IPublishMouseEvents source, IReceiveMouseEvents target);

        /// <summary>
        /// Unsubscribes the source from mouse events
        /// </summary>
        /// <param name="element">The source to unsubscribe</param>
        void Unsubscribe(IPublishMouseEvents element);

        /// <summary>
        /// Unsubscribes the element from mouse events
        /// </summary>
        /// <param name="element">The element to unsubscribe</param>
        void Unsubscribe(IReceiveMouseEvents element);
    }

    /// <summary>
    /// A cross-platform helper class to propagate mouse events in both Silverlight and WPF 
    /// </summary>
    public class MouseManager : IMouseManager
    {
        /// <summary>
        /// Defines the MouseEventGroup Attached Property
        /// </summary>
        public static readonly DependencyProperty MouseEventGroupProperty = DependencyProperty.RegisterAttached("MouseEventGroup", typeof(string), typeof(MouseManager), new PropertyMetadata(default(MasterSlaveChartModifier), OnMouseEventGroupPropertyChanged));

        private static readonly IDictionary<string, IList<IReceiveMouseEvents>> _modifiersByGroup = new Dictionary<string, IList<IReceiveMouseEvents>>();
        private readonly IDictionary<IReceiveMouseEvents, MouseDelegates> _delegatesByElement = new Dictionary<IReceiveMouseEvents, MouseDelegates>();
        private readonly IDictionary<IReceiveMouseEvents, IPublishMouseEvents> _subscribersBySource = new Dictionary<IReceiveMouseEvents, IPublishMouseEvents>();
        private readonly IDictionary<object, Point> _previousMousePositions = new Dictionary<object, Point>();
        private DateTime _lastClickTime;
        private Point _lastClickPosition;        

#if SILVERLIGHT
        static uint GetDoubleClickTime() { return 400; }
#elif SAFECODE
        static uint GetDoubleClickTime() { return 400; }
#else
        [DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseManager" /> class.
        /// </summary>
        public MouseManager()
        {
            // e.GetPosition has been refactored into MousePositionProvider to enable
            // unit testing of this class by swapping for a stub/mock IMousePositionProvider
            MousePositionProvider = new MousePositionProvider();

            TouchPositionProvider = new TouchPositionProvider();
        }

        /// <summary>
        /// Sets the MouseEventGroup Attached Property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="modifierGroup">The modifier group.</param>
        public static void SetMouseEventGroup(DependencyObject element, string modifierGroup)
        {
            element.SetValue(MouseEventGroupProperty, modifierGroup);
        }

        /// <summary>
        /// Gets the MouseEventGroup Attached Property
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns></returns>
        public static string GetMouseEventGroup(DependencyObject element)
        {
            return (string)element.GetValue(MouseEventGroupProperty);
        }

        /// <summary>
        /// Subscribes to mouse events on the Source, propagating handlers to the Target
        /// </summary>
        /// <param name="source">The source of mouse events</param>
        /// <param name="target">The target to receive mouse event handlers</param>
        public void Subscribe(IPublishMouseEvents source, IReceiveMouseEvents target)
        {
            Guard.NotNull(source, "element");
            Guard.NotNull(target, "receiveMouseEvents");

            Unsubscribe(target);

            var mouseDelegates = new MouseDelegates();
            mouseDelegates.Target = target;

            ResetGroup(target);

            Action<object, TouchManipulationEventArgs, Action<ModifierTouchManipulationArgs, IReceiveMouseEvents, bool>> touchHandler = (s, e, raiseEvent) =>
            {
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierTouchManipulationArgs(e.TouchPoints, true, mouseDelegates.Target);

                targets.ForEachDo(t => raiseEvent(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
            };

            mouseDelegates.TouchDownDelegate = (s, e) => touchHandler(s, e, RaiseTouchDown);

            mouseDelegates.TouchMoveDelegate = (s, e) => touchHandler(s, e, RaiseTouchMove);

            mouseDelegates.TouchUpDelegate = (s, e) => touchHandler(s, e, RaiseTouchUp);

            mouseDelegates.MouseLeftDownDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.Left, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                var timeBetweenClicks = DateTime.UtcNow - _lastClickTime;
                if (timeBetweenClicks < TimeSpan.FromMilliseconds(GetDoubleClickTime()) &&
                    timeBetweenClicks > TimeSpan.FromMilliseconds(1) &&
                    (PointUtil.Distance(mouseClickPoint, _lastClickPosition)  < 5))
                {
                    targets.ForEachDo(t => RaiseMouseDoubleClick(args, t, t.Equals(mouseDelegates.Target)));
                    return;
                }

                targets.ForEachDo(t => RaiseMouseDown(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
                _lastClickTime = DateTime.UtcNow;
                _lastClickPosition = mouseClickPoint;
            };

            mouseDelegates.MouseLeftUpDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.Left, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseUp(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
            };

            mouseDelegates.MouseMoveDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.None, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseMove(args, t, t.Equals(mouseDelegates.Target)));

#if !SILVERLIGHT
                e.Handled = args.Handled;
#endif
            };

            mouseDelegates.MouseRightDownDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.Right, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseDown(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
            };

            mouseDelegates.MouseRightUpDelegate = (s, e) =>
                {
                    Point mouseClickPoint = GetPosition(source, e);
                    var targets = GetTargets(mouseDelegates.Target);
                    var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.Right, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                    targets.ForEachDo(t => RaiseMouseUp(args, t, t.Equals(mouseDelegates.Target)));

                    e.Handled = args.Handled;
                };

            mouseDelegates.MouseWheelDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.None, MouseExtensions.GetCurrentModifier(), e.Delta, true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseWheel(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
            };

            mouseDelegates.MouseLeaveDelegate = (s, e) => 
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.None, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseLeave(args, t, t.Equals(mouseDelegates.Target)));
                
#if !SILVERLIGHT
                e.Handled = args.Handled;
#endif
            };

#if !SILVERLIGHT
            mouseDelegates.MouseMiddleDownDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.Middle, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseDown(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
            };

            mouseDelegates.MouseMiddleUpDelegate = (s, e) =>
            {
                Point mouseClickPoint = GetPosition(source, e);
                var targets = GetTargets(mouseDelegates.Target);
                var args = new ModifierMouseArgs(mouseClickPoint, MouseButtons.Middle, MouseExtensions.GetCurrentModifier(), true, mouseDelegates.Target);

                targets.ForEachDo(t => RaiseMouseUp(args, t, t.Equals(mouseDelegates.Target)));

                e.Handled = args.Handled;
            };
#endif
            source.TouchDown += mouseDelegates.TouchDownDelegate;
            source.TouchMove += mouseDelegates.TouchMoveDelegate;
            source.TouchUp += mouseDelegates.TouchUpDelegate;

            source.MouseLeftButtonDown += mouseDelegates.MouseLeftDownDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseLeftDownDelegate, eh => source.MouseLeftButtonDown -= eh);
            source.MouseLeftButtonUp += mouseDelegates.MouseLeftUpDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseLeftUpDelegate, eh => source.MouseLeftButtonDown -= eh);

            mouseDelegates.SynchronizedMouseMove = new RenderSynchronizedMouseMove(source);
            mouseDelegates.SynchronizedMouseMove.SynchronizedMouseMove += mouseDelegates.MouseMoveDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseMoveDelegate, eh => mouseDelegates.SynchronizedMouseMove.SynchronizedMouseMove -= eh);

            source.MouseRightButtonDown += mouseDelegates.MouseRightDownDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseRightDownDelegate, eh => source.MouseRightButtonDown -= eh);
            source.MouseRightButtonUp += mouseDelegates.MouseRightUpDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseRightUpDelegate, eh => source.MouseRightButtonUp -= eh);

            source.MouseWheel += mouseDelegates.MouseWheelDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseWheelDelegate, eh => source.MouseWheel -= eh);

            source.MouseLeave += mouseDelegates.MouseLeaveDelegate;

#if !SILVERLIGHT
            source.MouseMiddleButtonDown += mouseDelegates.MouseMiddleDownDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseMiddleDownDelegate, eh => source.MouseMiddleButtonDown -= eh);
            source.MouseMiddleButtonUp += mouseDelegates.MouseMiddleUpDelegate;// WeakEventHandler.Wrap(mouseDelegates.MouseMiddleUpDelegate, eh => source.MouseMiddleButtonUp -= eh);
#endif

            DelegatesByElement.Add(target, mouseDelegates);
            _subscribersBySource.Add(target, source);
        }

        private Point GetPosition(IPublishMouseEvents source, MouseEventArgs e)
        {
            return MousePositionProvider.GetPosition(source, e);
        }

        private TouchPointCollection GetPosition(IPublishMouseEvents source, TouchFrameEventArgs e)
        {
            return TouchPositionProvider.GetPosition(source, e);
        }

        private void ResetGroup(IReceiveMouseEvents target)
        {
            //Reset MouseEventGroupProperty for target
            var dependencyObject = target as DependencyObject;
            if (dependencyObject != null)
             {
                 var group = (string)dependencyObject.GetValue(MouseEventGroupProperty);

                 dependencyObject.SetCurrentValue(MouseEventGroupProperty, String.Empty);
                 dependencyObject.SetCurrentValue(MouseEventGroupProperty, group);
            }
        }        

        /// <summary>
        /// Unsubscribes the source from subscribers
        /// </summary>
        /// <param name="source">The source to unsubscribe</param>
        public void Unsubscribe(IPublishMouseEvents source)
        {
            if (source == null) return;

            var subscribersToRemove = new List<IReceiveMouseEvents>();

            foreach (var pair in DelegatesByElement)
            {
                if (_subscribersBySource[pair.Key] == source)
                {
                    var mouseDelegates = pair.Value;

                    source.MouseLeftButtonDown -= mouseDelegates.MouseLeftDownDelegate;
                    source.MouseLeftButtonUp -= mouseDelegates.MouseLeftUpDelegate;
                    source.MouseMove -= mouseDelegates.MouseMoveDelegate;
                    source.MouseRightButtonDown -= mouseDelegates.MouseRightDownDelegate;
                    source.MouseRightButtonUp -= mouseDelegates.MouseRightUpDelegate;
                    source.MouseWheel -= mouseDelegates.MouseWheelDelegate;
                    source.MouseLeave -= mouseDelegates.MouseLeaveDelegate;

#if !SILVERLIGHT
                    source.MouseMiddleButtonDown -= mouseDelegates.MouseMiddleDownDelegate;
                    source.MouseMiddleButtonUp -= mouseDelegates.MouseMiddleUpDelegate;
#endif

                    foreach (string modifierGroup in _modifiersByGroup.Keys)
                    {
                        if (_modifiersByGroup[modifierGroup].Contains(mouseDelegates.Target))
                            _modifiersByGroup[modifierGroup].Remove(mouseDelegates.Target);
                    }

                    mouseDelegates.Target = null;
                    mouseDelegates.SynchronizedMouseMove.Dispose();
                    mouseDelegates.SynchronizedMouseMove = null;

                    /*                if (PreviousMousePositions.ContainsKey(element))
                                    {
                                        PreviousMousePositions.Remove(element);
                                    }*/

                    subscribersToRemove.Add(pair.Key);
                }
            }

            foreach (var subscriber in subscribersToRemove)
            {
                DelegatesByElement.Remove(subscriber);
                _subscribersBySource.Remove(subscriber);
            }
        }

        /// <summary>
        /// Unsubscribes the element from mouse events
        /// </summary>
        /// <param name="element">The element to unsubscribe</param>
        public void Unsubscribe(IReceiveMouseEvents element)
        {
            if (element == null) return;

            if (_subscribersBySource.ContainsKey(element))
            {
                var mouseDelegates = DelegatesByElement[element];
                var source = _subscribersBySource[element];

                source.MouseLeftButtonDown -= mouseDelegates.MouseLeftDownDelegate;
                source.MouseLeftButtonUp -= mouseDelegates.MouseLeftUpDelegate;
                source.MouseMove -= mouseDelegates.MouseMoveDelegate;
                source.MouseRightButtonDown -= mouseDelegates.MouseRightDownDelegate;
                source.MouseRightButtonUp -= mouseDelegates.MouseRightUpDelegate;
                source.MouseWheel -= mouseDelegates.MouseWheelDelegate;
                source.MouseLeave -= mouseDelegates.MouseLeaveDelegate;

#if !SILVERLIGHT
                source.MouseMiddleButtonDown -= mouseDelegates.MouseMiddleDownDelegate;
                source.MouseMiddleButtonUp -= mouseDelegates.MouseMiddleUpDelegate;
#endif

                foreach (string modifierGroup in _modifiersByGroup.Keys)
                {
                    if (_modifiersByGroup[modifierGroup].Contains(mouseDelegates.Target))
                        _modifiersByGroup[modifierGroup].Remove(mouseDelegates.Target);
                }

                mouseDelegates.Target = null;
                mouseDelegates.SynchronizedMouseMove.Dispose();
                mouseDelegates.SynchronizedMouseMove = null;

                DelegatesByElement.Remove(element);
                _subscribersBySource.Remove(element);
            }

/*            if (PreviousMousePositions.ContainsKey(element))
            {
                PreviousMousePositions.Remove(element);
            }*/
        }

        private static void OnMouseEventGroupPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chartModifier = (IReceiveMouseEvents)d;
            var oldGroup = e.OldValue as string;
            if (oldGroup != null && _modifiersByGroup.ContainsKey(oldGroup))
            {
                _modifiersByGroup[oldGroup].Remove(chartModifier);
            }

            var newGroup = e.NewValue as string;
            if (string.IsNullOrEmpty(newGroup))
            {
                chartModifier.MouseEventGroup = null;
                return;
            }

            if (!_modifiersByGroup.ContainsKey(newGroup))
            {
                _modifiersByGroup[newGroup] = new List<IReceiveMouseEvents>();
            }

            _modifiersByGroup[newGroup].Add(chartModifier);
            chartModifier.MouseEventGroup = newGroup;
        }

        private static void RaiseTouchDown(ModifierTouchManipulationArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierManipulationStarted", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierTouchDown(args);
        }

        private static void RaiseTouchMove(ModifierTouchManipulationArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierManipulationDelta", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierTouchMove(args);
        }

        private static void RaiseTouchUp(ModifierTouchManipulationArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierManipulationCompleted", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierTouchUp(args);
        }

        private static void RaiseMouseDown(ModifierMouseArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierMouseDown", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierMouseDown(args);
        }

        private void RaiseMouseUp(ModifierMouseArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierMouseUp", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierMouseUp(args);
        }

        private void RaiseMouseDoubleClick(ModifierMouseArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierDoubleClick", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierDoubleClick(args);
            _lastClickTime = DateTime.MinValue;
            _lastClickPosition = new Point(-1, -1);
        }

        private void RaiseMouseMove(ModifierMouseArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            //UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierMouseMove", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierMouseMove(args);
        }

        private void RaiseMouseLeave(ModifierMouseArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnModifierMouseLeave", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnMasterMouseLeave(args);
        }

        private void RaiseMouseWheel(ModifierMouseArgs args, IReceiveMouseEvents target, bool isMaster)
        {
            UltrachartDebugLogger.Instance.WriteLine("Raising {0}.OnMasterMouseLeave", target.GetType().Name);
            args.IsMaster = isMaster;
            target.OnModifierMouseWheel(args);
        }

        internal IEnumerable<IReceiveMouseEvents> GetTargets(IReceiveMouseEvents target)
        {
            var targetsFromGroup = Enumerable.Empty<IReceiveMouseEvents>();

            if(target != null)
            {
                if (target.MouseEventGroup == null)
                {
                    if (target.CanReceiveMouseEvents())
                    {
                        targetsFromGroup = new[] {target};
                    }
                }
                else
                {
                    targetsFromGroup = _modifiersByGroup[target.MouseEventGroup].Where(md => md.CanReceiveMouseEvents());   
                }
            }

            return targetsFromGroup;
        }

        
        internal IMousePositionProvider MousePositionProvider { get; set; }
        internal ITouchPositionProvider TouchPositionProvider { get; set; }

        internal IDictionary<IReceiveMouseEvents, MouseDelegates> DelegatesByElement { get { return _delegatesByElement; } }
        internal IDictionary<object, Point> PreviousMousePositions { get { return _previousMousePositions; } }
        internal IDictionary<string, IList<IReceiveMouseEvents>> ModifiersByGroup { get { return _modifiersByGroup; } }
        internal IDictionary<IReceiveMouseEvents, IPublishMouseEvents> SubscribersBySource { get { return _subscribersBySource; } }

    }

    /// <summary>
    /// Provides <see cref="TouchPointCollection"/> information from an <see cref="IPublishMouseEvents"/> source
    /// </summary>
    public interface ITouchPositionProvider
    {
        /// <summary>
        /// Provides <see cref="TouchPointCollection" /> information from an <see cref="IPublishMouseEvents" /> source
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="e">The <see cref="TouchFrameEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        TouchPointCollection GetPosition(IPublishMouseEvents source, TouchFrameEventArgs e);
    }

    internal class TouchPositionProvider : ITouchPositionProvider
    {
        /// <summary>
        /// Provides <see cref="TouchPointCollection" /> information from an <see cref="IPublishMouseEvents" /> source
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="e">The <see cref="TouchFrameEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        public TouchPointCollection GetPosition(IPublishMouseEvents source, TouchFrameEventArgs e)
        {
            return e.GetTouchPoints(source as UIElement);
        }
    }

    /// <summary>
    /// An interface to a provider which converts <see cref="MouseEventArgs"/> into <see cref="Point"/> coordinates. Used
    /// internally to Ultrachart and implemented with interface to enable mocking and testing
    /// </summary>
    public interface IMousePositionProvider
    {
        /// <summary>
        /// Gets the mouse position from the <see cref="MouseEventArgs"/> as a <see cref="Point"/> (pixel coordinates relative to <see cref="IPublishMouseEvents">source</see>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <returns>The mouse position as a <see cref="Point"/></returns>
        Point GetPosition(IPublishMouseEvents source, MouseEventArgs e);
    }

    internal class MousePositionProvider : IMousePositionProvider
    {
        public Point GetPosition(IPublishMouseEvents sender, MouseEventArgs e)
        {
            return e.GetPosition(sender as UIElement);
        }
    }
}