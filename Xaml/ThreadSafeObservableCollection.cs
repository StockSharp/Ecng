namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;

	/// <summary>
	/// Provides a threadsafe ObservableCollection of T.
	/// </summary>
	/// <remarks>http://sachabarber.net/?p=418</remarks>
	/// <typeparam name="T"></typeparam>
	public class ThreadSafeObservableCollection<T> : ObservableCollection<T>, ICollectionEx<T>
	{
		private readonly ReaderWriterLock _lock = new ReaderWriterLock();

		public bool RaiseAddRemoveEvents { get; set; }

		private GuiDispatcher _dispatcher = new GuiDispatcher();

		public GuiDispatcher Dispatcher
		{
			get { return _dispatcher; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_dispatcher = value;
			}
		}

		public void AddRange(IEnumerable<T> items)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);

				CheckReentrancy();
				
				((List<T>)Items).AddRange(items);

				OnPropertyChanged(new PropertyChangedEventArgs("Count"));
				OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

				if (RaiseAddRemoveEvents)
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList()));
				else
					// http://stackoverflow.com/questions/670577/observablecollection-doesnt-support-addrange-method-so-i-get-notified-for-each
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				
				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => AddRange(items));
			}
		}

		public void RemoveRange(IEnumerable<T> items)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);

				CheckReentrancy();

				((List<T>)Items).RemoveRange(items);

				OnPropertyChanged(new PropertyChangedEventArgs("Count"));
				OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

				if (RaiseAddRemoveEvents)
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items.ToList()));
				else
					// http://stackoverflow.com/questions/670577/observablecollection-doesnt-support-addrange-method-so-i-get-notified-for-each
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => RemoveRange(items));
			}
		}

		public void RemoveRange(int from, int count)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);

				CheckReentrancy();

				((List<T>)Items).RemoveRange(from, count);

				OnPropertyChanged(new PropertyChangedEventArgs("Count"));
				OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

				// http://stackoverflow.com/questions/670577/observablecollection-doesnt-support-addrange-method-so-i-get-notified-for-each
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => RemoveRange(from, count));
			}
		}

		protected override void ClearItems()
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);
				base.ClearItems();
				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(Clear);
			}
		}

		protected override void InsertItem(int index, T item)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);
				base.InsertItem(index, item);
				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => InsertItem(index, item));
			}
		}

		protected override void MoveItem(int oldIndex, int newIndex)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);
				base.MoveItem(oldIndex, newIndex);
				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => MoveItem(oldIndex, newIndex));
			}
		}

		protected override void RemoveItem(int index)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);
				base.RemoveItem(index);
				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => RemoveItem(index));
			}
		}

		protected override void SetItem(int index, T item)
		{
			if (_dispatcher.Dispatcher.CheckAccess())
			{
				var c = _lock.UpgradeToWriterLock(-1);
				base.SetItem(index, item);
				_lock.DowngradeFromWriterLock(ref c);
			}
			else
			{
				_dispatcher.AddAction(() => SetItem(index, item));
			}
		}
	}
}