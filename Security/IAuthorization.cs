namespace Ecng.Security
{
	using System;
	using System.Net;
	using System.Security;

	using Ecng.Common;

	/// <summary>
	/// Defines the interface to an authorization module.
	/// </summary>
	public interface IAuthorization
	{
		///// <summary>
		///// Save user.
		///// </summary>
		///// <param name="login">Login.</param>
		///// <param name="password">Password.</param>
		///// <param name="possibleAddresses">Possible addresses.</param>
		//void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses);

		///// <summary>
		///// Change password.
		///// </summary>
		///// <param name="login">Login.</param>
		///// <param name="oldPassword">Old password.</param>
		///// <param name="newPassword">New password.</param>
		//void ChangePassword(string login, SecureString oldPassword, SecureString newPassword);

		///// <summary>
		///// Delete user by login.
		///// </summary>
		///// <param name="login">Login.</param>
		///// <returns>Returns <see langword="true"/>, if user was deleted, otherwise return <see langword="false"/>.</returns>
		//bool DeleteUser(string login);

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
	/// Authorization module granted access for everyone.
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

		///// <inheritdoc />
		//public virtual void SaveUser(string login, SecureString password, IEnumerable<IPAddress> possibleAddresses)
		//{
		//	throw new NotSupportedException();
		//}

		///// <inheritdoc />
		//public void ChangePassword(string login, SecureString oldPassword, SecureString newPassword)
		//{
		//	throw new NotSupportedException();
		//}

		///// <inheritdoc />
		//public virtual bool DeleteUser(string login)
		//{
		//	throw new NotSupportedException();
		//}
	}

	/// <summary>
	/// The connection access check module which provides access by simple login and password set.
	/// </summary>
	public class SimpleAuthorization : AnonymousAuthorization
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleAuthorization"/>.
		/// </summary>
		public SimpleAuthorization()
		{
		}

		/// <summary>
		/// Login.
		/// </summary>
		public string Login { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		public SecureString Password { get; set; }

		/// <inheritdoc />
		public override Guid ValidateCredentials(string login, SecureString password, IPAddress clientAddress)
		{
			if (Login.IsEmpty())
				return base.ValidateCredentials(login, password, clientAddress);
			else if (login.EqualsIgnoreCase(Login) && password != null && Password != null && password.IsEqualTo(Password))
				return Guid.NewGuid();

			throw new UnauthorizedAccessException();
		}
	}
}