namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	using Ecng.Common;

	public class UniqueObservableCollection<T> : ObservableCollection<T>
	{
		public UniqueObservableCollection()
		{
		}

		public UniqueObservableCollection(IEnumerable<T> collection)
			: base(collection)
		{
		}

		public UniqueObservableCollection(List<T> list)
			: base(list)
		{
		}

		protected override void InsertItem(int index, T item)
		{
			if (Items.Contains(item))
				throw new ArgumentException("Элемент '{0}' уже добавлен в коллекцию.".Put(item));
			
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, T item)
		{
			var i = IndexOf(item);
			if (i >= 0 && i != index)
				throw new ArgumentException("Элемент '{0}' уже добавлен в коллекцию.".Put(item));
			
			base.SetItem(index, item);
		} 
	}
}
