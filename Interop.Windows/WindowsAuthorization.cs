namespace Ecng.Interop
{
	using System;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Security;

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
				throw new UnauthorizedAccessException($"User {login} not found or password is incorrect.");

			return Guid.NewGuid().To<string>();
		}
	}
}