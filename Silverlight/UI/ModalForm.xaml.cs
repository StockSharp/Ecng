namespace Ecng.UI
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;

	public partial class ModalForm
	{
		private const int _offset = 5;
		private const int _headerSize = 30;

		private bool _captured;
		private Point<int> _beginPos;

		public ModalForm()
		{
			InitializeComponent();
		}

		public event EventHandler<EventArgs> Clicked;
		public event EventHandler<ModalFormClosedEventArgs> Closed;

		private UserControl _contentElem;

		public UserControl ContentElem
		{
			get { return _contentElem; }
			set
			{
				if (value == null)
					throw new ArgumentNullException();

				this.ContentCanvas.Children.Add(value);
				value.SetLocation(new Point<int>(10, 10));
				this.SetSize(value.GetSize() + new Size<int>(40, 80));
				_contentElem = value;
			}
		}

		private ModalButtons _buttons;

		public ModalButtons Buttons
		{
			get { return _buttons; }
			set
			{
				switch (value)
				{
					case ModalButtons.OkCancel:
						break;
					case ModalButtons.Ok:
						this.Close.Visibility = this.CancelBorder.Visibility = Visibility.Collapsed;
						break;
					case ModalButtons.None:
						this.Close.Visibility = this.CancelBorder.Visibility = this.OkBorder.Visibility = Visibility.Collapsed;
						break;
					case ModalButtons.Close:
						this.CancelBorder.Visibility = this.OkBorder.Visibility = Visibility.Collapsed;
						break;
					default:
						throw new ArgumentOutOfRangeException("value");
				}

				_buttons = value;
			}
		}

		public void Click()
		{
			Clicked.SafeInvoke(this);
		}

		public void CloseContent()
		{
			if (CanClose(true))
				this.ContentCanvas.Children.Remove(_contentElem);
		}

		public bool ContainsContent(UserControl elem)
		{
			return this.ContentCanvas.Children.Contains(elem);
		}

		private void LayoutRoot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Click();

			_beginPos = e.GetPositionEx(this);

			if (new Rectangle<int>(_offset, _offset, (int)base.Width - _headerSize, _headerSize).Contains(_beginPos))
				_captured = ((Grid)sender).CaptureMouse();
		}

		private void LayoutRoot_MouseMove(object sender, MouseEventArgs e)
		{
			if (_captured)
			{
				var endPos = e.GetPositionEx(this);
				this.SetLocation(this.GetLocation() + (endPos - _beginPos));
			}
		}

		private void LayoutRoot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// if mouse down event was on the ModalForm
			if (_beginPos != null)
			{
				_captured = false;
				((Grid)sender).ReleaseMouseCapture();

				if (new Rectangle<int>((int)base.Width - _headerSize, _offset, _headerSize, _headerSize).Contains(_beginPos))
				{
					//var args = new EventArgs();
					if (CanClose(false))
						Closed.SafeInvoke(this, new ModalFormClosedEventArgs(false));

					//if (!args.IsCancel && _contentElem != null)
					//	CloseContent();
				}
			}
		}

		private bool CanClose(bool isOk)
		{
			if (isOk)
			{
				var content = this.ContentElem as IModalContent;
				return content != null ? content.CanClose : true;
			}
			else
				return true;
		}

		private void Cancel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (CanClose(false))
				Closed.SafeInvoke(this, new ModalFormClosedEventArgs(false));
		}

		private void OK_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (CanClose(true))
				Closed.SafeInvoke(this, new ModalFormClosedEventArgs(true));
		}
	}
}