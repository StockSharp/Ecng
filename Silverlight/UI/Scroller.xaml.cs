namespace Ecng.UI
{
	using System;
	using System.Collections.Generic;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Shapes;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	public partial class Scroller
	{
		private sealed class ScrollerSlotList : BaseCollection<ScrollerSlot>
		{
			private readonly Scroller _scroller;

			internal ScrollerSlotList(Scroller scroller)
				: base(false)
			{
				_scroller = scroller;
			}

			protected override void OnAdded(ScrollerSlot item)
			{
				_scroller.AddSlot(item);
				base.OnAdded(item);
			}

			protected override void OnRemoved(ScrollerSlot item)
			{
				_scroller.RemoveSlot(item);
				base.OnRemoved(item);
			}

			protected override void OnCleared()
			{
				_scroller.ClearSlots();
				base.OnCleared();
			}
		}

		//private const int _topOffset = 0;
		private const int _stepSize = 32;

		private bool _isScrollUp;
		private Brush _prevBrush;

		public Scroller()
		{
			InitializeComponent();
			_slots = new ScrollerSlotList(this);
			this.SlotSize = 64;
		}

		#region SelectionBrush

		private static Brush SelectionBrush
		{
			get { return new SolidColorBrush(Colors.Red); }
		}

		#endregion

		#region UnSelectionBrush

		private static Brush UnselectionBrush
		{
			get { return new SolidColorBrush(Colors.Green); }
		}

		#endregion

		public int SlotSize { get; set; }

		public event EventHandler<EventArgs> SlotHovered;
		public event EventHandler<EventArgs> SlotSelected;
		public event EventHandler<EventArgs> SlotClicked;

		#region Slots

		private readonly ScrollerSlotList _slots;

		public IList<ScrollerSlot> Slots
		{
			get { return _slots; }
		}

		#endregion

		private ScrollerSlot _hoveredSlot;

		public ScrollerSlot HoveredSlot
		{
			get { return _hoveredSlot; }
			private set
			{
				_hoveredSlot = value;
				SlotHovered.SafeInvoke(this);
			}
		}

		#region SelectedSlot

		private ScrollerSlot _selectedSlot;

		public ScrollerSlot SelectedSlot
		{
			get { return _selectedSlot; }
			set
			{
				if (_selectedSlot != null)
					_selectedSlot.Rect.Stroke = UnselectionBrush;

				if (value != null)
					value.Rect.Stroke = SelectionBrush;

				_selectedSlot = value;
				SlotSelected.SafeInvoke(this);
			}
		}

		#endregion

		public static readonly DependencyProperty StretchSlotsProperty = DependencyProperty.Register("StretchSlots", typeof(Boolean), typeof(Scroller), new PropertyMetadata(false, OnStretchSlotsChanged));

		private static void OnStretchSlotsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
		}

		public bool StretchSlots
		{
			get { return (bool)GetValue(StretchSlotsProperty); }
			set { SetValue(StretchSlotsProperty, value); }
		}

		public void ResetBodyTop()
		{
			this.BodyTop = 0;
		}

		#region BodyTop

		private int BodyTop
		{
			get
			{
				return (int)(double)this.Body.GetValue((this.Orientation == Orientation.Horizontal) ? Canvas.LeftProperty : Canvas.TopProperty);
			}
			set
			{
				this.Body.SetValue((this.Orientation == Orientation.Horizontal) ? Canvas.LeftProperty : Canvas.TopProperty, (double)value);
			}
		}

		#endregion

		public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(Orientation), typeof(Scroller), new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

		private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var scroller = (Scroller)d;

			switch ((Orientation)e.NewValue)
			{
				case Orientation.Vertical:
					break;
				case Orientation.Horizontal:
					scroller.LayoutRoot.RowDefinitions.Clear();
					scroller.LayoutRoot.ColumnDefinitions.Clear();
					scroller.LayoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
					scroller.LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
					scroller.LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					scroller.LayoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
					scroller.Prev.SetColumn(0);
					scroller.Prev.SetRow(0);
					scroller.BodyRect.SetColumn(1);
					scroller.BodyRect.SetRow(0);
					scroller.BodyContainer.SetColumn(1);
					scroller.BodyContainer.SetRow(0);
					scroller.Next.SetColumn(2);
					scroller.Next.SetRow(0);
					scroller.BodyRect.Height = scroller.BodyRect.Width;
					scroller.BodyRect.Width = double.NaN;
					scroller.BodyContainerGeometry.Rect = new Rect(0, 0, scroller.BodyContainerGeometry.Rect.Height + 16, scroller.BodyContainerGeometry.Rect.Width);
					scroller.LayoutRoot.Width = scroller.LayoutRoot.Height;
					scroller.LayoutRoot.Height = double.NaN;
					break;
				default:
					throw new ArgumentOutOfRangeException("e");
			}
		}

		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		private void AddSlot(ScrollerSlot slot)
		{
			if (slot == null)
				throw new ArgumentNullException("slot");

			var fill = new ImageBrush { ImageSource = slot.ImageSource, Stretch = StretchSlots ? Stretch.Fill : Stretch.None };
			//fill.ImageSource.SetUrl(slot.ImageUrl);

			int offset = this.SlotSize * this.Body.Children.Count;
			var rect = new Rectangle { Stroke = UnselectionBrush, StrokeThickness = 1, Fill = fill };
			rect.SetBounds(new Rectangle<int>(new Point<int>(2 + (this.Orientation == Orientation.Horizontal ? offset : 0), 2 + (this.Orientation == Orientation.Vertical ? offset : 0)), new Size<int>(this.SlotSize, this.SlotSize)));
			rect.MouseEnter += (sender, e) =>
			{
				//Debug.WriteLine("selected");

				var selectedRect = (Rectangle)sender;

				if (this.SelectedSlot == null || !Equals(selectedRect, this.SelectedSlot.Rect))
				{
					_prevBrush = selectedRect.Stroke;
					selectedRect.Stroke = SelectionBrush;
				}

				this.HoveredSlot = this.Slots.First(item => item.Rect == sender);
			};
			rect.MouseLeave += (sender, e) =>
			{
				//Debug.WriteLine("unselected");

				var selectedRect = (Rectangle)sender;

				if (this.SelectedSlot == null || !Equals(selectedRect, this.SelectedSlot.Rect))
					selectedRect.Stroke = _prevBrush;

				this.HoveredSlot = null;
			};
			rect.MouseLeftButtonDown += (sender, e) =>
			{
				var clickedSlot = this.Slots.First(item => item.Rect == sender);
				var same = this.SelectedSlot == clickedSlot;
				this.SelectedSlot = clickedSlot;
				if (same)
					SlotClicked.SafeInvoke(this);
			};
			rect.SetToolTip(slot.Tooltip);

			this.Body.Children.Add(rect);

			slot.Rect = rect;
		}

		private void RemoveSlot(ScrollerSlot slot)
		{
			int index = this.Body.Children.IndexOf(slot.Rect);
			this.Body.Children.Remove(slot.Rect);

			for (int i = index; i < this.Body.Children.Count; i++)
			{
				var item = (FrameworkElement)this.Body.Children[i];
				var location = item.GetLocation() - new Point<int>((this.Orientation == Orientation.Horizontal ? this.SlotSize : 0), (this.Orientation == Orientation.Vertical ? this.SlotSize : 0));
				item.SetLocation(location);
			}

			if (this.SelectedSlot == slot)
				this.SelectedSlot = this.Slots.FirstOrDefault();
		}

		private void ClearSlots()
		{
			this.Body.Children.Clear();
		}

		private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			StartScroll((Button)sender);
		}

		private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			StopScroll((Button)sender);
		}

		private void OnMouseLeave(object sender, MouseEventArgs e)
		{
			StopScroll((Button)sender);
		}

		private void StartScroll(Button button)
		{
			_isScrollUp = (button == this.Next);

			Scroll();

			button.CaptureMouse();
			this.FirstScrollTimer.Begin();
		}

		private void StopScroll(Button button)
		{
			button.ReleaseMouseCapture();
			this.FirstScrollTimer.Stop();
			this.ScrollTimer.Stop();
		}

		private void ScrollTimer_Completed(object sender, EventArgs e)
		{
			//Debug.WriteLine("ScrollTimer_Completed");
			Scroll();
			this.ScrollTimer.Begin();
		}

		private void FirstScrollTimer_Completed(object sender, EventArgs e)
		{
			//Debug.WriteLine("FirstScrollTimer_Completed");
			Scroll();
			this.ScrollTimer.Begin();
		}

		private void Scroll()
		{
			if (_isScrollUp)
				ScrollUp();
			else
				ScrollDown();
		}

		private void ScrollDown()
		{
			if (this.BodyTop < 0)
				this.BodyTop += _stepSize;
		}

		private void ScrollUp()
		{
			int newBodyTop = -(_slots.Count - ((int)(this.Orientation == Orientation.Horizontal ? base.Width : base.Height) - 25) / this.SlotSize) * this.SlotSize;

			//Debug.WriteLine("NewBodyTop" + newBodyTop);
			//Debug.WriteLine("BodyTop" + this.BodyTop);

			if (this.BodyTop > newBodyTop)
				this.BodyTop -= _stepSize;
		}

		private void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var rect = this.BodyContainerGeometry.Rect;
			rect.Height = this.Orientation == Orientation.Horizontal ? this.LayoutRoot.ColumnDefinitions[1].ActualWidth : this.LayoutRoot.RowDefinitions[1].ActualHeight;
			this.BodyContainerGeometry.Rect = rect;
		}
	}
}