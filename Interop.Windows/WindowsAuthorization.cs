namespace Ecng.Interop
{
	using System;
	using System.DirectoryServices.AccountManagement;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Security;

	/// <summary>
	/// Authorization module based on Windows user storage.
	/// </summary>
	public class WindowsAuthorization : IAuthorization
	{
		/// <inheritdoc />
		public virtual string ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			using var context = new PrincipalContext(ContextType.Machine, null);

			if (!context.ValidateCredentials(login, password.UnSecure()))
				throw new UnauthorizedAccessException($"User {login} not found or password is incorrect.");

			return Guid.NewGuid().To<string>();
		}
	}
}