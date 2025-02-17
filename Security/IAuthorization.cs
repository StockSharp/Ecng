namespace Ecng.Security
{
	using System;
	using System.Net;
	using System.Security;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	/// <summary>
	/// Defines the interface to an authorization module.
	/// </summary>
	public interface IAuthorization
	{
		/// <summary>
		/// Check login and password.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="clientAddress">Remote network address.</param>
		/// <returns>Session identifier.</returns>
		/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
		/// <returns><see cref="ValueTask{T}"/></returns>
		ValueTask<string> ValidateCredentials(string login, SecureString password, IPAddress clientAddress, CancellationToken cancellationToken);
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
		public virtual ValueTask<string> ValidateCredentials(string login, SecureString password, IPAddress clientAddress, CancellationToken cancellationToken)
			=> new(Guid.NewGuid().To<string>());
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
		public override ValueTask<string> ValidateCredentials(string login, SecureString password, IPAddress clientAddress, CancellationToken cancellationToken)
		{
			if (Login.IsEmpty())
				return base.ValidateCredentials(login, password, clientAddress, cancellationToken);
			else if (login.EqualsIgnoreCase(Login) && password != null && Password != null && password.IsEqualTo(Password))
				return new(Guid.NewGuid().To<string>());

			throw new UnauthorizedAccessException();
		}
	}

	/// <summary>
	/// <see cref="IAuthorization"/> do not allow access.
	/// </summary>
	public class UnauthorizedAuthorization : IAuthorization
	{
		ValueTask<string> IAuthorization.ValidateCredentials(string login, SecureString password, IPAddress clientAddress, CancellationToken cancellationToken)
			=> throw new UnauthorizedAccessException();
	}
}