namespace Ecng.Security
{
	using System;
	using System.Security;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// Defines the interface to an autorization module.
	/// </summary>
	public interface IAuthorization
	{
		/// <summary>
		/// Check login and password.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session identifier.</returns>
		Guid ValidateCredentials(string login, SecureString password);
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

		/// <summary>
		/// Check login and password.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session identifier.</returns>
		public virtual Guid ValidateCredentials(string login, SecureString password)
		{
			return Guid.NewGuid();
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

		/// <summary>
		/// Check login and password.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <returns>Session identifier.</returns>
		public virtual Guid ValidateCredentials(string login, SecureString password)
		{
			if (!WindowsIdentityManager.Validate(login, password.To<string>()))
				throw new UnauthorizedAccessException("User {0} not found or password is incorrect.".Translate().Put(login));

			return Guid.NewGuid();
		}
	}
}