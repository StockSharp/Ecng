namespace Ecng.Xaml
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Input;
	using System.Windows.Interop;
	using System.Windows.Media;
	using System;

	[TemplatePart(Name = "PART_Presenter", Type = typeof(ContentPresenter))]
	[TemplatePart(Name = "PART_Gripper", Type = typeof(Thumb))]
	public class ResizableContentControl : ContentControl
	{
		private Thumb _gripper;
		private Size _controlSize;
		private Point _currentPosition;

		public static readonly DependencyProperty CanAutoSizeProperty = 
			DependencyProperty.Register(nameof(CanAutoSize), typeof(bool), typeof(ResizableContentControl), new FrameworkPropertyMetadata(true));
		
		public static readonly DependencyProperty GripperBackgroundProperty = 
			DependencyProperty.Register(nameof(GripperBackground), typeof(Brush), typeof(ResizableContentControl), new FrameworkPropertyMetadata(Brushes.Transparent));
		
		public static readonly DependencyProperty GripperForegroundProperty = 
			DependencyProperty.Register(nameof(GripperForeground), typeof(Brush), typeof(ResizableContentControl), new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(184, 180, 162))));
		
		public static readonly DependencyProperty ResizeModeProperty = 
			DependencyProperty.Register(nameof(ResizeMode), typeof(ControlResizeMode), typeof(ResizableContentControl), new FrameworkPropertyMetadata(ControlResizeMode.Both));

		public bool CanAutoSize
		{
			get => (bool)GetValue(CanAutoSizeProperty);
			set => SetValue(CanAutoSizeProperty, value);
		}

		public Brush GripperBackground
		{
			get => (Brush)GetValue(GripperBackgroundProperty);
			set => SetValue(GripperBackgroundProperty, value);
		}

		public Brush GripperForeground
		{
			get => (Brush)GetValue(GripperForegroundProperty);
			set => SetValue(GripperForegroundProperty, value);
		}

		public ControlResizeMode ResizeMode
		{
			get => (ControlResizeMode)GetValue(ResizeModeProperty);
			set => SetValue(ResizeModeProperty, value);
		}

		private Thumb Gripper => GetTemplateChild("PART_Gripper") as Thumb;

		static ResizableContentControl()
		{
			IsTabStopProperty.OverrideMetadata(typeof(ResizableContentControl), new FrameworkPropertyMetadata(false));
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizableContentControl), new FrameworkPropertyMetadata(typeof(ResizableContentControl)));
			MinHeightProperty.OverrideMetadata(typeof(ResizableContentControl), new FrameworkPropertyMetadata(4.0));
			MinWidthProperty.OverrideMetadata(typeof(ResizableContentControl), new FrameworkPropertyMetadata(4.0));
			FocusableProperty.OverrideMetadata(typeof(ResizableContentControl), new FrameworkPropertyMetadata(false));
		}

		public ResizableContentControl()
		{
		}

		public ResizableContentControl(object content) : this()
		{
			Content = content;
		}

		private void GripperDragDelta(object sender, DragDeltaEventArgs e)
		{
			var size1 = new Size(double.PositiveInfinity, double.PositiveInfinity);
			var size2 = new Size(MinWidth, MinHeight);
			var frameworkElement = Content as FrameworkElement;
			
			while (frameworkElement is ContentPresenter)
				frameworkElement = ((ContentPresenter)frameworkElement).Content as FrameworkElement;
			
			if (frameworkElement != null)
			{
				var rect = frameworkElement.TransformToAncestor(this).TransformBounds(new Rect(new Point(0.0, 0.0), frameworkElement.RenderSize));
				var num1 = Math.Max(0.0, RenderSize.Width - rect.Width);
				var num2 = Math.Max(0.0, RenderSize.Height - rect.Height);
				size2.Width = Math.Max(size2.Width, frameworkElement.MinWidth + num1);
				size2.Height = Math.Max(size2.Height, frameworkElement.MinHeight + num2);
				
				if (!double.IsNaN(frameworkElement.MaxWidth) && frameworkElement.MaxWidth < 100000.0)
					size1.Width = Math.Min(size1.Width, frameworkElement.MaxWidth + num1);
				
				if (!double.IsNaN(frameworkElement.MaxHeight) && frameworkElement.MaxHeight < 100000.0)
					size1.Height = Math.Min(size1.Height, frameworkElement.MaxHeight + num2);
			}

			if (BrowserInteropHelper.IsBrowserHosted)
			{
				if (ResizeMode == ControlResizeMode.Both || ResizeMode == ControlResizeMode.Horizontal)
					Width = Math.Min(size1.Width, Math.Max(size2.Width, RenderSize.Width + e.HorizontalChange));
				
				if (ResizeMode != ControlResizeMode.Both && ResizeMode != ControlResizeMode.Vertical)
					return;
				
				Height = Math.Min(size1.Height, Math.Max(size2.Height, RenderSize.Height + e.VerticalChange));
			}
			else
			{
				var point = PointToScreen(Mouse.GetPosition(this));
				
				if (ResizeMode == ControlResizeMode.Both || ResizeMode == ControlResizeMode.Horizontal)
					Width = Math.Min(size1.Width, Math.Max(size2.Width, _controlSize.Width + (FlowDirection == FlowDirection.LeftToRight ? 1.0 : -1.0) * (point.X - _currentPosition.X)));
				
				if (ResizeMode != ControlResizeMode.Both && ResizeMode != ControlResizeMode.Vertical)
					return;
				
				Height = Math.Min(size1.Height, Math.Max(size2.Height, _controlSize.Height + (point.Y - _currentPosition.Y)));
			}
		}

		private void GripperDragStarted(object sender, DragStartedEventArgs e)
		{
			if (BrowserInteropHelper.IsBrowserHosted)
				return;
			
			_currentPosition = PointToScreen(Mouse.GetPosition(this));
			_controlSize = RenderSize;
		}

		private void GripperMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!CanAutoSize)
				return;
			
			AutoSize();
		}

		public void AutoSize()
		{
			Width = double.NaN;
			Height = double.NaN;
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			if (_gripper != null)
			{
				_gripper.DragDelta -= GripperDragDelta;
				_gripper.DragStarted -= GripperDragStarted;
				_gripper.MouseDoubleClick -= GripperMouseDoubleClick;
			}
			_gripper = Gripper;
			
			if (_gripper == null)
				return;

			_gripper.DragDelta += GripperDragDelta;
			_gripper.DragStarted += GripperDragStarted;
			_gripper.MouseDoubleClick += GripperMouseDoubleClick;
		}
	}

	public enum ControlResizeMode
	{
		None,
		Horizontal,
		Vertical,
		Both,
	}
}
