namespace Ecng.Xaml
{
	using System.Windows;

	using MessageBox = Xceed.Wpf.Toolkit.MessageBox;

	using Ecng.Common;

	public class MessageBoxBuilder
	{
		private Window _owner;
		private string _text;
		private string _caption;
		private MessageBoxButton _button = MessageBoxButton.OK;
		private MessageBoxImage _icon = MessageBoxImage.None;
		private string _description;

		public MessageBoxBuilder Owner(Window owner)
		{
			_owner = owner;
			return this;
		}

		public MessageBoxBuilder Text(string text)
		{
			_text = text;
			return this;
		}

		public MessageBoxBuilder Caption(string caption)
		{
			_caption = caption;
			return this;
		}

		public MessageBoxBuilder Description(string description)
		{
			_description = description;
			return this;
		}

		public MessageBoxBuilder Error()
		{
			return Icon(MessageBoxImage.Error);
		}

		public MessageBoxBuilder Warning()
		{
			return Icon(MessageBoxImage.Warning);
		}

		public MessageBoxBuilder Info()
		{
			return Icon(MessageBoxImage.Information);
		}

		public MessageBoxBuilder Question()
		{
			return Icon(MessageBoxImage.Question);
		}

		public MessageBoxBuilder Icon(MessageBoxImage icon)
		{
			_icon = icon;
			return this;
		}

		public MessageBoxBuilder YesNo()
		{
			return Button(MessageBoxButton.YesNo);
		}

		public MessageBoxBuilder OkCancel()
		{
			return Button(MessageBoxButton.OKCancel);
		}

		public MessageBoxBuilder Button(MessageBoxButton button)
		{
			_button = button;
			return this;
		}

		public MessageBoxResult Show()
		{
			var caption = _caption;

			if (caption == null)
			{
				caption = TypeHelper.ApplicationName;

				if (_description != null)
					caption += " - " + _description;
			}

			return _owner == null ? MessageBox.Show(_text, caption, _button, _icon) : MessageBox.Show(_owner, _text, caption, _button, _icon);
		}
	}
}