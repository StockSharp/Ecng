namespace Ecng.Xaml.DevExp
{
	using System.Security;
	using System.Windows;

	using DevExpress.Xpf.Editors;

	using Ecng.Common;

	public partial class SecretEdit
	{
		private const string _fakeMask = "5mmdfxfo56";

		private bool _suspendChanges;

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Secret"/>.
		/// </summary>
		public static readonly DependencyProperty SecretProperty =
			DependencyProperty.Register(nameof(Secret), typeof(SecureString), typeof(SecretEdit), new PropertyMetadata(default(SecureString),
				(o, args) =>
				{
					var picker = (SecretEdit)o;
					var secret = (SecureString)args.NewValue;

					picker.SetPassword(secret);
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
			if (PasswordCtrl.Password == _fakeMask) 
				return;

			try
			{
				_suspendChanges = true;

				Secret = PasswordCtrl.Password.To<SecureString>();
			}
			finally
			{
				_suspendChanges = false;
			}
		}

		private void SetPassword(SecureString secret)
		{
			if (_suspendChanges)
				return;
			
			PasswordCtrl.Password = !secret.IsEmpty() 
				? _fakeMask // заполняем поле пароля звездочками
				: null;
		}
	}
}
