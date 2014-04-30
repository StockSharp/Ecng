namespace Ecng.Security
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Интерфейс, описывающий модуль проверки доступа соединения.
	/// </summary>
	public interface IAuthorization
	{
		/// <summary>
		/// Проверить логин и пароль на правильность.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <returns>Идентификатор сессии.</returns>
		Guid ValidateCredentials(string login, string password);
	}

	/// <summary>
	/// Модуль проверки доступа соединения, предоставляющий доступ всем соединениям.
	/// </summary>
	public class AnonymousAuthorization : IAuthorization
	{
		/// <summary>
		/// Создать <see cref="AnonymousAuthorization"/>.
		/// </summary>
		public AnonymousAuthorization()
		{
		}

		/// <summary>
		/// Проверить логин и пароль на правильность.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <returns>Идентификатор сессии.</returns>
		public virtual Guid ValidateCredentials(string login, string password)
		{
			return Guid.NewGuid();
		}
	}

	/// <summary>
	/// Модуль проверки доступа соединения, основанный на Windows авторизации.
	/// </summary>
	public class WindowsAuthorization : IAuthorization
	{
		/// <summary>
		/// Создать <see cref="WindowsAuthorization"/>.
		/// </summary>
		public WindowsAuthorization()
		{
		}

		/// <summary>
		/// Проверить логин и пароль на правильность.
		/// </summary>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		/// <returns>Идентификатор сессии.</returns>
		public virtual Guid ValidateCredentials(string login, string password)
		{
			if (!WindowsIdentityManager.Validate(login, password))
				throw new UnauthorizedAccessException("Пользователь {0} не найден или был передан неправильный пароль.".Put(login));

			return Guid.NewGuid();
		}
	}
}