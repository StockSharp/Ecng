namespace Ecng.Collections
{
	using System.Collections.Generic;

	/// <summary>
	/// ����������� ������ <see cref="IEnumerable{T}"/> � ���������� ������ ���������� ���������.
	/// </summary>
	/// <typeparam name="T">��� ��������.</typeparam>
	public interface IEnumerableEx<T> : IEnumerable<T>
	{
		/// <summary>
		/// ���������� ���������.
		/// </summary>
		int Count { get; }
	}
}