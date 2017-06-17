namespace Ecng.Xaml.DevExp
{
	using System.Security;
	using System.Windows;

	using DevExpress.Xpf.Editors;

	using Ecng.Common;

	public partial class SecretEdit
	{
		private const string _fakeMask = "5mmdfxfo56";

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Secret"/>.
		/// </summary>
		public static readonly DependencyProperty SecretProperty =
			DependencyProperty.Register(nameof(Secret), typeof(SecureString), typeof(SecretEdit), new PropertyMetadata(default(SecureString),
				(o, args) =>
				{
					var picker = (SecretEdit)o;
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

		public SecretEdit()
		{
			InitializeComponent();
		}

		private void BaseEdit_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
		{
			if (PasswordCtrl.Password != _fakeMask)
				Secret = PasswordCtrl.Password.To<SecureString>();
		}
	}
}
