namespace Ecng.ComponentModel
{
	using System.Security;

	using Ecng.Serialization;

	/// <summary>
	/// The class that contains a login and password to access the services https://stocksharp.com .
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
				NotifyChanged(nameof(Email));
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
				NotifyChanged(nameof(Password));
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
				NotifyChanged(nameof(AutoLogon));
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
				.Set(nameof(AutoLogon), AutoLogon);
		}
	}
}