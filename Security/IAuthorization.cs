namespace Ecng.Security
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// ���������, ����������� ������ �������� ������� ����������.
	/// </summary>
	public interface IAuthorization
	{
		/// <summary>
		/// ��������� ����� � ������ �� ������������.
		/// </summary>
		/// <param name="login">�����.</param>
		/// <param name="password">������.</param>
		/// <returns>������������� ������.</returns>
		Guid ValidateCredentials(string login, string password);
	}

	/// <summary>
	/// ������ �������� ������� ����������, ��������������� ������ ���� �����������.
	/// </summary>
	public class AnonymousAuthorization : IAuthorization
	{
		/// <summary>
		/// ������� <see cref="AnonymousAuthorization"/>.
		/// </summary>
		public AnonymousAuthorization()
		{
		}

		/// <summary>
		/// ��������� ����� � ������ �� ������������.
		/// </summary>
		/// <param name="login">�����.</param>
		/// <param name="password">������.</param>
		/// <returns>������������� ������.</returns>
		public virtual Guid ValidateCredentials(string login, string password)
		{
			return Guid.NewGuid();
		}
	}

	/// <summary>
	/// ������ �������� ������� ����������, ���������� �� Windows �����������.
	/// </summary>
	public class WindowsAuthorization : IAuthorization
	{
		/// <summary>
		/// ������� <see cref="WindowsAuthorization"/>.
		/// </summary>
		public WindowsAuthorization()
		{
		}

		/// <summary>
		/// ��������� ����� � ������ �� ������������.
		/// </summary>
		/// <param name="login">�����.</param>
		/// <param name="password">������.</param>
		/// <returns>������������� ������.</returns>
		public virtual Guid ValidateCredentials(string login, string password)
		{
			if (!WindowsIdentityManager.Validate(login, password))
				throw new UnauthorizedAccessException("������������ {0} �� ������ ��� ��� ������� ������������ ������.".Put(login));

			return Guid.NewGuid();
		}
	}
}