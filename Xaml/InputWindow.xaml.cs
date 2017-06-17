namespace Ecng.Xaml
{
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Localization;

	public partial class InputWindow
	{
		public static readonly RoutedCommand OkCommand = new RoutedCommand();
		public static readonly RoutedCommand CancelCommand = new RoutedCommand();

		public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(nameof(Message), typeof(string), typeof(InputWindow),
			new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

		public string Message
		{
			get => (string)GetValue(MessageProperty);
			set => SetValue(MessageProperty, value);
		}

		public static string WaitInput(Window owner, string title, string message)
		{
			var window = new InputWindow
			{
				Title = title,
				Message = message,
				Owner = owner
			};

			return window.ShowDialog() == true
				? window.TextBoxName.Text
				: null;
		}

		private InputWindow()
		{
			InitializeComponent();

			Cancel.Content = ((string)Cancel.Content).Translate();
		}

		private void Ok_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;//!string.IsNullOrWhiteSpace(TextBoxName.Text);
		}

		private void Ok_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void Cancel_OnExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void SaveLayoutWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			TextBoxName.Focus();
		}
	}
}
