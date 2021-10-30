namespace Ecng.Interop
{
	using System;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Security;
	using Ecng.Localization;

	/// <summary>
	/// Authorization module based on Windows user storage.
	/// </summary>
	public class WindowsAuthorization : IAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsAuthorization"/>.
		/// </summary>
		public WindowsAuthorization()
		{
		}

		/// <inheritdoc />
		public virtual string ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			if (!WindowsIdentityManager.Validate(login, password))
				throw new UnauthorizedAccessException("User {0} not found or password is incorrect.".Translate().Put(login));

			return Guid.NewGuid().To<string>();
		}
	}
}