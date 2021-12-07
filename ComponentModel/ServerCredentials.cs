namespace Ecng.ComponentModel
{
	using System.Security;

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
				_email = value;
				NotifyChanged();
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
				_password = value;
				NotifyChanged();
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
				_token = value;
				NotifyChanged();
			}
		}

		private bool _autoLogon = true;

		/// <summary>
		/// Auto login.
		/// </summary>
		public bool AutoLogon
		{
			get => _autoLogon;
			set
			{
				_autoLogon = value;
				NotifyChanged();
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public virtual void Load(SettingsStorage storage)
		{
			Email = storage.GetValue<string>(nameof(Email));
			Password = storage.GetValue<SecureString>(nameof(Password));
			AutoLogon = storage.GetValue<bool>(nameof(AutoLogon));
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
				.Set(nameof(AutoLogon), AutoLogon)
				.Set(nameof(Token), Token);
		}
	}
}