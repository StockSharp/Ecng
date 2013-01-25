namespace Ecng.Collections
{
	using System;
	using System.Collections.Generic;

	public interface INotifyList<TItem> : IList<TItem>
	{
		/// <summary>
		/// Событие, вызываемое перед нового элемента.
		/// </summary>
		/// <remarks>
		/// Данное событие используется для проверки валидности добавляемого элемента.
		/// </remarks>
		event Action<TItem> Adding;

		/// <summary>
		/// Событие о добавлении нового элемента.
		/// </summary>
		event Action<TItem> Added;

		/// <summary>
		/// Событие, вызываемое перед удалением элемента.
		/// </summary>
		/// <remarks>
		/// Данное событие используется для проверки возможности удаление элемента.
		/// </remarks>
		event Action<TItem> Removing;

		/// <summary>
		/// Событие об удалении элемента.
		/// </summary>
		event Action<TItem> Removed;

		/// <summary>
		/// Событие удаления всех элементов.
		/// </summary>
		event Action Clearing;

		/// <summary>
		/// Событие удаления всех элементов.
		/// </summary>
		event Action Cleared;

		/// <summary>
		/// Событие вставки элементов.
		/// </summary>
		event Action<int, TItem> Inserting;

		/// <summary>
		/// Событие вставки элементов.
		/// </summary>
		event Action<int, TItem> Inserted;

		/// <summary>
		/// Событие изменения коллекции.
		/// </summary>
		event Action Changed;
	}
}