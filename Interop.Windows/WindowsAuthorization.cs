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

		///// <inheritdoc />
		//public virtual void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses)
		//{
		//	if (WindowsIdentityManager.Validate(login, password))
		//	{
		//		if (!WindowsIdentityManager.ChangePassword(login, null, password))
		//			throw new InvalidOperationException("User {0} not found.".Translate().Put(login));
		//	}
		//	else
		//		WindowsIdentityManager.CreateUser(login, password);
		//}

		///// <inheritdoc />
		//public void ChangePassword(string login, SecureString oldPassword, SecureString newPassword)
		//{
		//	if (!WindowsIdentityManager.Validate(login, oldPassword))
		//		throw new InvalidOperationException("User {0} not found.".Translate().Put(login));

		//	if (!WindowsIdentityManager.ChangePassword(login, oldPassword, newPassword))
		//		throw new InvalidOperationException("User {0} not found or password is incorrect.".Translate().Put(login));
		//}

		///// <inheritdoc />
		//public virtual bool DeleteUser(string login)
		//{
		//	return WindowsIdentityManager.DeleteUser(login);
		//}

		/// <inheritdoc />
		public virtual Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			if (!WindowsIdentityManager.Validate(login, password))
				throw new UnauthorizedAccessException("User {0} not found or password is incorrect.".Translate().Put(login));

			return Guid.NewGuid();
		}
	}
}