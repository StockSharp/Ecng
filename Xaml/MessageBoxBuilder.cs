namespace Ecng.Xaml
{
	using System;
	using System.Windows;

	using Ecng.Common;

	public interface IMessageBoxHandler
	{
		MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options);
		MessageBoxResult Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options);
	}

	public class MessageBoxBuilder
	{
		public class WpfMessageBoxHandler : IMessageBoxHandler
		{
			MessageBoxResult IMessageBoxHandler.Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
			{
				return MessageBox.Show(text, caption, button, icon, defaultResult, options);
			}

			MessageBoxResult IMessageBoxHandler.Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
			{
				return MessageBox.Show(owner, text, caption, button, icon, defaultResult, options);
			}
		}

		private static IMessageBoxHandler _defaultHandler = new WpfMessageBoxHandler();

		public static IMessageBoxHandler DefaultHandler
		{
			get { return _defaultHandler; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_defaultHandler = value;
			}
		}

		private Window _owner;
		private string _text;
		private string _caption;
		private MessageBoxButton _button = MessageBoxButton.OK;
		private MessageBoxImage _icon = MessageBoxImage.None;
		private MessageBoxResult _defaultResult = MessageBoxResult.None;
		private MessageBoxOptions _options = MessageBoxOptions.None;
		private IMessageBoxHandler _handler = DefaultHandler;

		public MessageBoxBuilder Handler(IMessageBoxHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));

			_handler = handler;
			return this;
		}

		public MessageBoxBuilder Owner(DependencyObject owner)
		{
			_owner = owner.GetWindow();
			return this;
		}

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

		public MessageBoxBuilder Error(Exception error)
		{
			if (error == null)
				throw new ArgumentNullException(nameof(error));

			return Text(error.ToString()).Icon(MessageBoxImage.Error);
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

		public MessageBoxBuilder Options(MessageBoxOptions options)
		{
			_options = options;
			return this;
		}

		public MessageBoxBuilder DefaultResult(MessageBoxResult defaultResult)
		{
			_defaultResult = defaultResult;
			return this;
		}

		public MessageBoxResult Show()
		{
			var caption = _caption;

			if (caption == null)
			{
				caption = TypeHelper.ApplicationName;
			}

			return _owner == null
				? _handler.Show(_text, caption, _button, _icon, _defaultResult, _options)
				: _handler.Show(_owner, _text, caption, _button, _icon, _defaultResult, _options);
		}
	}
}