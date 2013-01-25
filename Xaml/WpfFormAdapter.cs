////////////////////////////////////////////////////////////////////////////////
// StickyWindows
// 
// Copyright (c) 2009 Riccardo Pietrucci
//
// This software is provided 'as-is', without any express or implied warranty.
// In no event will the author be held liable for any damages arising from 
// the use of this software.
// Permission to use, copy, modify, distribute and sell this software for any 
// purpose is hereby granted without fee, provided that the above copyright 
// notice appear in all copies and that both that copyright notice and this 
// permission notice appear in supporting documentation.
//
//////////////////////////////////////////////////////////////////////////////////


using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using System.Windows.Media;

namespace Ecng.Xaml
{
	using Ecng.Common;

	public class WpfFormAdapter : IFormAdapter
	{
		private readonly Window _window;
		private Point? _origin;

		public WpfFormAdapter(Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			_window = window;
		}

		#region IFormAdapter Members

		public IntPtr Handle
		{
			get
			{
				return _window.GetOwnerHandle();
			}
		}

		public Rectangle Bounds
		{
			get
			{
				// converti width ed height ad absolute
				var widthHeightPointConverted = FromRelativeToDevice(_window.ActualWidth, _window.ActualHeight, _window);

				// converti coordinate da relative a screen: la libreria lavora con quelle
				var origin = GetWindowOrigin();
				var pStart = PointToScreen(origin);
				var pEnd = PointToScreen(origin + new Size(widthHeightPointConverted.X.To<int>(), widthHeightPointConverted.Y.To<int>()));
				pEnd.Offset(-pStart.X, -pStart.Y); // ora pend rappresenta width + height

				// imposta
				return new Rectangle(pStart.X, pStart.Y, pEnd.X, pEnd.Y);
			}
			set
			{
				// converti width ed height a relative
				var widthHeightPointConverted = FromDeviceToRelative(value.Width, value.Height, _window);
				// converti coordinate da screen a relative: il video non si deve alterare!
				var origin = GetWindowOrigin();
				var screenPointRef = new Point(-origin.X + value.X, -origin.Y + value.Y);
				var pStart = PointFromScreen(new Point(screenPointRef.X, screenPointRef.Y));

				// imposta
				_window.Left += pStart.X;
				_window.Top += pStart.Y;
				_window.Width = widthHeightPointConverted.X;
				_window.Height = widthHeightPointConverted.Y;
			}
		}

		public Size MaximumSize
		{
			get { return new Size(_window.MaxWidth.To<int>(), _window.MinHeight.To<int>()); }
			set
			{
				_window.MaxWidth = value.Width;
				_window.MaxHeight = value.Height;
			}
		}

		public Size MinimumSize
		{
			get { return new Size(_window.MinWidth.To<int>(), _window.MinWidth.To<int>()); }
			set
			{
				_window.MinWidth = value.Width;
				_window.MinHeight = value.Height;
			}
		}

		public bool Capture
		{
			get { return _window.IsMouseCaptured; }
			set
			{
				IInputElement targetToCapture = value ? _window : null;
				Mouse.Capture(targetToCapture);
			}
		}

		public void Activate()
		{
			_window.Activate();
		}

		public Point PointToScreen(Point pointWin)
		{
			var p = new System.Windows.Point();
			var resultWpf = _window.PointToScreen(p).ToWin();
			var resultScaled = resultWpf + new Size(pointWin);
			return resultScaled;
		}

		#endregion

		#region Utility Methods

		private Point GetWindowOrigin()
		{
			// TODO: alla prima invocazione far andare in cache per migliorare perf ed evitare errori di approx
			//return new Point(-4, -28);
			if (!_origin.HasValue)
			{
				var currentWinPointConverted = FromRelativeToDevice(-_window.Left, -_window.Top, _window);
				var locationFromScreen = PointToScreen(currentWinPointConverted.ToWin());
				_origin = new Point(-locationFromScreen.X, -locationFromScreen.Y);
			}

			return _origin.Value;
		}

		private static System.Windows.Point FromDeviceToRelative(double x, double y, Visual workingVisual)
		{
			var widthHeightPoint = new Point(x.To<int>(), y.To<int>());
			var source = PresentationSource.FromVisual(workingVisual);
			return source.CompositionTarget.TransformFromDevice.Transform(widthHeightPoint.ToWpf());
		}

		private static System.Windows.Point FromRelativeToDevice(double x, double y, Visual workingVisual)
		{
			var widthHeightPoint = new Point(x.To<int>(), y.To<int>());
			var source = PresentationSource.FromVisual(workingVisual);
			return source.CompositionTarget.TransformToDevice.Transform(widthHeightPoint.ToWpf());
		}

		public Point PointFromScreen(Point pointWin)
		{
			return _window.PointFromScreen(pointWin.ToWpf()).ToWin();
		}

		#endregion
	}
}