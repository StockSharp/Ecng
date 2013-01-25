namespace Ecng.Collections
{
	using System.Collections.Generic;

	/// <summary>
	/// Расширенная версия <see cref="IEnumerable{T}"/> с получением общего количества элементов.
	/// </summary>
	/// <typeparam name="T">Тип элемента.</typeparam>
	public interface IEnumerableEx<T> : IEnumerable<T>
	{
		/// <summary>
		/// Количество элементов.
		/// </summary>
		int Count { get; }
	}
}