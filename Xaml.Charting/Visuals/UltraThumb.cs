// *************************************************************************************
// ULTRACHART™ © Copyright ulc software Services Ltd. 2011-2014. All rights reserved.
//  
// Web: http://www.ultrachart.com
// Support: support@ultrachart.com
// 
// UltrachartScrollbar.cs is part of Ultrachart, a High Performance WPF & Silverlight Chart. 
// For full terms and conditions of the license, see http://www.ultrachart.com/ultrachart-eula/
// 
// This source code is protected by international copyright law. Unauthorized
// reproduction, reverse-engineering, or distribution of all or any portion of
// this source code is strictly prohibited.
// 
// This source code contains confidential and proprietary trade secrets of
// ulc software Services Ltd., and should at no time be copied, transferred, sold,
// distributed or made available without express written permission.
// *************************************************************************************using System.Diagnostics;

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Ecng.Xaml.Charting.Visuals
{
    /// <summary>
    /// Custom simple commom Thumb implementation
    /// </summary>
    public class UltraThumb : Control
    {
        private bool _isDragging;
        private Point _originThumbPoint; 
        private Point _previousCoordPosition;

        /// <summary>
        /// Occurs one or more times as the mouse changes position when a <see cref="UltraThumb"/> control has mouse capture. 
        /// </summary>
        public event DragDeltaEventHandler UltraDragDelta;

        /// <summary>
        /// Default constructor initialize <see cref="UltraThumb"/>
        /// </summary>
        public UltraThumb()
        {
            DefaultStyleKey = typeof (UltraThumb);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!_isDragging)
            {
                _isDragging = true;
                e.Handled = true;

                _originThumbPoint = e.GetPosition(this);

                CaptureMouse();
            }
            else
            {
                // This is weird, Thumb shouldn't get MouseLeftButtonDown event while dragging.
                // This may be the case that something ate MouseLeftButtonUp event, so Thumb never had a chance to
                // reset IsDragging property
                Debug.Assert(false, "Got MouseLeftButtonDown event while dragging!");
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDragging)
            {
                Point thumbCoordPosition = e.GetPosition(this);

                // We will fire DragDelta event only when the mouse is really moved
                if (thumbCoordPosition != _previousCoordPosition)
                {
                    _previousCoordPosition = thumbCoordPosition;

                    OnUltraDragDelta(thumbCoordPosition.X - _originThumbPoint.X, thumbCoordPosition.Y - _originThumbPoint.Y);
                }
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            _isDragging = false;
            ReleaseMouseCapture();

            _originThumbPoint.X = 0;
            _originThumbPoint.Y = 0;
        }

        protected virtual void OnUltraDragDelta(double horizontalChange, double verticalChange)
        {
            if (UltraDragDelta != null)
            {
                UltraDragDelta(this, new DragDeltaEventArgs(horizontalChange, verticalChange));
            }
        }
    }
}