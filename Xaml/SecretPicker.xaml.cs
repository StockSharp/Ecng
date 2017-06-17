namespace Ecng.Xaml
{
	using System.Security;
	using System.Windows;

	using Ecng.Common;

	/// <summary>
	/// Редактор для <see cref="SecureString"/>.
	/// </summary>
	public partial class SecretPicker
	{
		private const string _fakeMask = "5mmdfxfo56";

		/// <summary>
		/// Создать <see cref="SecretPicker"/>.
		/// </summary>
		public SecretPicker()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Secret"/>.
		/// </summary>
		public static readonly DependencyProperty SecretProperty =
			DependencyProperty.Register(nameof(Secret), typeof(SecureString), typeof(SecretPicker), new PropertyMetadata(default(SecureString),
				(o, args) =>
				{
					var picker = (SecretPicker)o;
					var secret = (SecureString)args.NewValue;

					if (picker.PasswordCtrl.Password.IsEmpty() && secret != null && secret.Length > 0)
					{
						// заполняем поле пароля звездочками
						picker.PasswordCtrl.Password = _fakeMask;
					}
				}));

		/// <summary>
		/// Секрет.
		/// </summary>
		public SecureString Secret
		{
			get => (SecureString)GetValue(SecretProperty);
			set => SetValue(SecretProperty, value);
		}

		private void PasswordCtrl_OnPasswordChanged(object sender, RoutedEventArgs e)
		{
			if (PasswordCtrl.Password != _fakeMask)
				Secret = PasswordCtrl.Password.To<SecureString>();
		}
	}
}