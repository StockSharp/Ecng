namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	public class DependentObservableCollection<TItem, TDisplay> : ThreadSafeObservableCollection<TDisplay>
	{
		private static readonly bool _needConvert = typeof(TItem) != typeof(TDisplay);

		private enum ActionTypes
		{
			Add,
			Remove,
			Clear,
			Wait
		}

		private class CollectionAction
		{
			public ActionTypes Type { get; private set; }
			public TItem Item { get; private set; }

			public SyncObject SyncRoot { get; set; }

			public CollectionAction(ActionTypes type, TItem item)
			{
				Type = type;
				Item = item;
			}
		}

		private bool _isTimerStarted;
		private readonly Func<TItem, TDisplay> _converter;
		private readonly SynchronizedQueue<CollectionAction> _pendingActions = new SynchronizedQueue<CollectionAction>();
		private readonly SynchronizedDictionary<TItem, TDisplay> _convertedValues = new SynchronizedDictionary<TItem, TDisplay>();
		private int _safeCount;

		public DependentObservableCollection(INotifyList<TItem> underlyingList, Func<TItem, TDisplay> converter)
		{
			if (underlyingList == null)
				throw new ArgumentNullException("underlyingList");

			if (converter == null)
				throw new ArgumentNullException("converter");

			underlyingList.Added += i => AddAction(ActionTypes.Add, i);
			underlyingList.Inserted += (idx, i) => AddAction(ActionTypes.Add, i);
			underlyingList.Cleared += () => AddAction(ActionTypes.Clear, default(TItem));
			underlyingList.Removed += i => AddAction(ActionTypes.Remove, i);

			underlyingList.ForEach(i => AddAction(ActionTypes.Add, i));

			_converter = converter;
		}

		private int _maxCount = -1;

		public int MaxCount
		{
			get { return _maxCount; }
			set
			{
				if (value < -1 || value == 0)
					throw new ArgumentOutOfRangeException();

				_maxCount = value;
			}
		}

		public void Wait()
		{
			var syncRoot = new SyncObject();
			AddAction(new CollectionAction(ActionTypes.Wait, default(TItem)) { SyncRoot = syncRoot });
			syncRoot.Wait();
		}

		private void AddAction(ActionTypes type, TItem item)
		{
			AddAction(new CollectionAction(type, item));
		}

		private void AddAction(CollectionAction item)
		{
			lock (_pendingActions.SyncRoot)
			{
				_pendingActions.Add(item);

				if (_isTimerStarted)
					return;

				_isTimerStarted = true;

				ThreadingHelper
					.Timer(OnFlush)
					.Interval(TimeSpan.FromMilliseconds(300), new TimeSpan(-1));
			}
		}

		private void OnFlush()
		{
			var pendingAdd = new List<TDisplay>();
			var pendingRemove = new List<TDisplay>();

			CollectionAction[] actions;

			lock (_pendingActions.SyncRoot)
			{
				_isTimerStarted = false;
				actions = _pendingActions.CopyAndClear();
			}

			foreach (var action in actions)
			{
				switch (action.Type)
				{
					case ActionTypes.Add:
					{
						TDisplay display;

						if (_needConvert)
						{
							display = _converter(action.Item);
							_convertedValues.Add(action.Item, display);
						}
						else
						{
							display = action.Item.To<TDisplay>();
						}

						pendingAdd.Add(display);
						break;
					}
					case ActionTypes.Remove:
					{
						TDisplay display;

						if (_needConvert)
						{
							if (!_convertedValues.TryGetValue(action.Item, out display))
								continue;
						}
						else
						{
							display = action.Item.To<TDisplay>();
						}

						if (pendingAdd.Contains(display))
							pendingAdd.Remove(display);
						else
							pendingRemove.Add(display);

						break;
					}
					case ActionTypes.Clear:
						pendingAdd.Clear();
						pendingRemove.Clear();
						_convertedValues.Clear();
						Clear();
						break;
					case ActionTypes.Wait:
						var syncRoot = action.SyncRoot;
						Dispatcher.AddAction(syncRoot.Pulse);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			_safeCount = _safeCount + pendingAdd.Count - pendingRemove.Count;

			Dispatcher.AddAction(() =>
			{
				if (pendingAdd.Count > 0)
					AddRange(pendingAdd);

				if (pendingRemove.Count > 0)
					RemoveRange(pendingRemove);
			});

			if (MaxCount == -1 || _safeCount <= 2 * MaxCount)
				return;

			if (_needConvert)
			{
				KeyValuePair<TItem, TDisplay>[] removePairs;

				lock (_convertedValues.SyncRoot)
				{
					removePairs = _convertedValues.Take(_safeCount - MaxCount).ToArray();
					_convertedValues.RemoveRange(removePairs);		
				}

				Dispatcher.AddAction(() => RemoveRange(removePairs.Select(p => p.Value)));
			}
			else
				Dispatcher.AddAction(() => RemoveRange(0, _safeCount - MaxCount));

			_safeCount -= MaxCount;
		}

		public TDisplay TryGet(TItem item)
		{
			return _convertedValues.TryGetValue(item);
		}
	}

	public class DependentObservableCollection<T> : DependentObservableCollection<T, T>
	{
		public DependentObservableCollection(INotifyList<T> underlyingList)
			: base(underlyingList, t => t)
		{
		}
	}
}