namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// ����������� ������ <see cref="IEnumerable"/> � ���������� ������ ���������� ���������.
	/// </summary>
	public interface IEnumerableEx : IEnumerable
	{
		/// <summary>
		/// ���������� ���������.
		/// </summary>
		int Count { get; }
	}

	/// <summary>
	/// ����������� ������ <see cref="IEnumerable{T}"/> � ���������� ������ ���������� ���������.
	/// </summary>
	/// <typeparam name="T">��� ��������.</typeparam>
	public interface IEnumerableEx<out T> : IEnumerable<T>, IEnumerableEx
	{
	}
}