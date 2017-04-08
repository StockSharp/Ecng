namespace Ecng.Security
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// Defines the interface to an autorization module.
	/// </summary>
	public interface IAuthorization
	{
		/// <summary>
		/// Get all available users.
		/// </summary>
		IEnumerable<Tuple<string, IEnumerable<IPAddress>>> AllUsers { get; }

		/// <summary>
		/// Save user.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="possibleAddresses">Possible addresses.</param>
		void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses);

		/// <summary>
		/// Delete user by login.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <returns>Returns <see langword="true"/>, if user was deleted, otherwise return <see langword="false"/>.</returns>
		bool DeleteUser(string login);

		/// <summary>
		/// Check login and password.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="clientAddress">Remote network address.</param>
		/// <returns>Session identifier.</returns>
		Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress);
	}

	/// <summary>
	/// Autorization module granted access for everyone.
	/// </summary>
	public class AnonymousAuthorization : IAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AnonymousAuthorization"/>.
		/// </summary>
		public AnonymousAuthorization()
		{
		}

		/// <inheritdoc />
		public virtual Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			return Guid.NewGuid();
		}

		/// <inheritdoc />
		public virtual IEnumerable<Tuple<string, IEnumerable<IPAddress>>> AllUsers => Enumerable.Empty<Tuple<string, IEnumerable<IPAddress>>>();

		/// <inheritdoc />
		public virtual void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses)
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public virtual bool DeleteUser(string login)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>
	/// Autorization module based on Windows user storage.
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
		public virtual IEnumerable<Tuple<string, IEnumerable<IPAddress>>> AllUsers => Enumerable.Empty<Tuple<string, IEnumerable<IPAddress>>>();

		/// <inheritdoc />
		public virtual void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses)
		{
			if (WindowsIdentityManager.Validate(login, password))
			{
				if (!WindowsIdentityManager.ChangePassword(login, null, password))
					throw new InvalidOperationException("User {0} not found.".Translate().Put(login));
			}
			else
				WindowsIdentityManager.CreateUser(login, password);
		}

		/// <inheritdoc />
		public virtual bool DeleteUser(string login)
		{
			return WindowsIdentityManager.DeleteUser(login);
		}

		/// <inheritdoc />
		public virtual Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			if (!WindowsIdentityManager.Validate(login, password))
				throw new UnauthorizedAccessException("User {0} not found or password is incorrect.".Translate().Put(login));

			return Guid.NewGuid();
		}
	}
}