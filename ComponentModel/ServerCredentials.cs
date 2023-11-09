namespace Ecng.ComponentModel
{
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The class that contains a login and password.
	/// </summary>
	public class ServerCredentials : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServerCredentials"/>.
		/// </summary>
		public ServerCredentials()
		{
		}

		private string _email;

		/// <summary>
		/// Email.
		/// </summary>
		public string Email
		{
			get => _email;
			set
			{
				if (_email == value)
					return;

				_email = value;
				NotifyChanged();
				NotifyAutoLogon();
			}
		}

		private SecureString _password;

		/// <summary>
		/// Password.
		/// </summary>
		public SecureString Password
		{
			get => _password;
			set
			{
				if (_password.IsEqualTo(value))
					return;

				_password = value;
				NotifyChanged();
				NotifyAutoLogon();
			}
		}

		private SecureString _token;

		/// <summary>
		/// Token.
		/// </summary>
		public SecureString Token
		{
			get => _token;
			set
			{
				if (_token.IsEqualTo(value))
					return;

				_token = value;
				NotifyChanged();
				NotifyAutoLogon();
			}
		}

		/// <summary>
		/// Auto login.
		/// </summary>
		public bool AutoLogon => !Token.IsEmpty() || (!Email.IsEmpty() && !Password.IsEmpty());

		private void NotifyAutoLogon() => NotifyChanged(nameof(AutoLogon));

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Email = storage.GetValue<string>(nameof(Email));
			Password = storage.GetValue<SecureString>(nameof(Password));
			Token = storage.GetValue<SecureString>(nameof(Token));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(Email), Email)
				.Set(nameof(Password), Password)
				.Set(nameof(Token), Token);
		}
	}
}