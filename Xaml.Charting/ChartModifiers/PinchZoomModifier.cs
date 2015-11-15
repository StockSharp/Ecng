// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// PinchZoomModifier.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
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
using System.Windows.Threading;
using Ecng.Xaml.Charting.Visuals;

namespace Ecng.Xaml.Charting.ChartModifiers
{
    /// <summary>
    /// The <see cref="PinchZoomModifier"/> provides zooming of the <see cref="UltrachartSurface"/> with the pinch gesture
    /// </summary>
    public class PinchZoomModifier : RelativeZoomModifierBase
    {
        private const int MinAllowedManipulatorsAmount = 2;

        private bool _isDragging;

        private Point _center;

        private readonly Dictionary<int, Point> _points, _startPoints;
        private double _dist, _distX, _distY;

        private readonly DispatcherTimer _bufferTimer;
        private bool _bufferState, _bufferAny;

        private Point _bufferPoint;
        private double _bufferX, _bufferY;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinchZoomModifier"/> class.
        /// </summary>
        public PinchZoomModifier()
        {
            GrowFactor = 0.01;

            _points = new Dictionary<int, Point>();
            _startPoints = new Dictionary<int, Point>();
            _bufferTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };

            _bufferTimer.Tick += PerformZoomBuffer;
        }

        /// <summary>
        /// If True, Dragging is in progress
        /// </summary>        
        public bool IsDragging
        {
            get { return _isDragging; }
        }

        /// <summary>
        /// Gets or sets the value of IsUniform property, showing whether the aspect of the chart is preserved while zooming in or out.
        /// </summary>
        public bool IsUniform { get; set; }

        private void PerformZoomBuffer(object state, EventArgs eventArgs)
        {
            _bufferTimer.Stop();

            _bufferState = false;
            if (_bufferAny)
            {
                PerformZoom(_bufferPoint, _bufferX, _bufferY);
            }

            _bufferAny = false;

            _bufferX = _bufferY = 0;
            _bufferPoint = new Point();
        }

        /// <summary>
        /// Performs a zoom around the <paramref name="mousePoint" /> by the specified X and Y factor
        /// </summary>
        /// <param name="mousePoint">The mouse point.</param>
        /// <param name="xValue">The x zoom factor.</param>
        /// <param name="yValue">The y zoom factor.</param>
        protected override void PerformZoom(Point mousePoint, double xValue, double yValue)
        {
            _bufferState = true;
            _bufferAny = false;
            _bufferTimer.Start();

            base.PerformZoom(mousePoint, xValue, yValue);
        }

        /// <summary>
        /// Called when a Multi-Touch Down interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchDown(ModifierTouchManipulationArgs e)
        {
            foreach (var manip in e.Manipulators)
            {
                var mousePoint = GetPointRelativeTo(manip.Position, ModifierSurface);

                if (mousePoint.X >= 0 && mousePoint.X <= ModifierSurface.ActualWidth &&
                    mousePoint.Y >= 0 && mousePoint.Y <= ModifierSurface.ActualHeight)
                {
                    if (!_points.ContainsKey(manip.TouchDevice.Id))
                    {
                        _points.Add(manip.TouchDevice.Id, mousePoint);
                        _startPoints.Add(manip.TouchDevice.Id, mousePoint);
                    }
                }
            }

            if (_points.Count >= MinAllowedManipulatorsAmount)
            {
                _isDragging = true;

                _distX = _points.Max(it => it.Value.X) - _points.Min(it => it.Value.X);
                _distY = _points.Max(it => it.Value.Y) - _points.Min(it => it.Value.Y);
                _dist = Math.Sqrt(_distX * _distX + _distY * _distY);

                _center = GetCenter();
            }

            base.OnModifierTouchDown(e);
        }

        /// <summary>
        /// Called when a Multi-Touch Move interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchMove(ModifierTouchManipulationArgs e)
        {
            if (!IsDragging) return;

            foreach (var manip in e.Manipulators)
            {
                if (!_points.ContainsKey(manip.TouchDevice.Id)) continue;

                var id = manip.TouchDevice.Id;

                var current = GetPointRelativeTo(manip.Position, ModifierSurface);

                _points[id] = current;
            }

            var distX = _points.Max(it => it.Value.X) - _points.Min(it => it.Value.X);
            var distY = _points.Max(it => it.Value.Y) - _points.Min(it => it.Value.Y);
            var dist = Math.Sqrt(distX * distX + _distY * _distY);

            var diffX = distX - _distX;
            var diffY = distY - _distY;
            var diff = dist - _dist;

            if (_startPoints.Count >= MinAllowedManipulatorsAmount)
            {
                _center = GetCenter();

                var fraction = -Normalize(diff);
                var xFraction = IsUniform ? fraction : -Normalize(diffY);
                var yFraction = IsUniform ? fraction : -Normalize(diffX);

                ContinueZooming(_center, xFraction, yFraction);

                _dist = dist;
                _distX = distX;
                _distY = distY;
            }

            base.OnModifierTouchMove(e);
        }

        private Point GetCenter()
        {
            double xCenter = _startPoints.Average(it => it.Value.X);
            double yCenter = _startPoints.Average(it => it.Value.Y);

            return new Point(xCenter, yCenter);
        }

        private double Normalize(double value)
        {
            return Math.Min(1, Math.Max(-1, value));
        }

        private void ContinueZooming(Point mousePoint, double xValue, double yValue)
        {
            if (_bufferState)
            {
                _bufferPoint = mousePoint;
                _bufferX += xValue;
                _bufferY += yValue;
                _bufferAny = true;
            }
            else
            {
                PerformZoom(mousePoint, xValue, yValue);
            }
        }

        /// <summary>
        /// Called when a Multi-Touch Up interaction occurs on the parent <see cref="UltrachartSurface" />
        /// </summary>
        /// <param name="e">Arguments detailing the manipulation operation</param>
        public override void OnModifierTouchUp(ModifierTouchManipulationArgs e)
        {
            foreach (var manip in e.Manipulators)
            {
                var id = manip.TouchDevice.Id;

                if (_points.ContainsKey(id))
                {
                    _points.Remove(id);
                    _startPoints.Remove(id);
                }
            }

            if (!_startPoints.Any())
            {
                _isDragging = false;
            }
        }
    }
}
