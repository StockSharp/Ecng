namespace Ecng.Collections
{
	using System.Collections;
	using System.Collections.Generic;

	/// <summary>
	/// Расширенная версия <see cref="IEnumerable"/> с получением общего количества элементов.
	/// </summary>
	public interface IEnumerableEx : IEnumerable
	{
		/// <summary>
		/// Количество элементов.
		/// </summary>
		int Count { get; }
	}

	/// <summary>
	/// Расширенная версия <see cref="IEnumerable{T}"/> с получением общего количества элементов.
	/// </summary>
	/// <typeparam name="T">Тип элемента.</typeparam>
	public interface IEnumerableEx<out T> : IEnumerable<T>, IEnumerableEx
	{
	}
}