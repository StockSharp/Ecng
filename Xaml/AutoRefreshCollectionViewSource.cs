namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Data;

	using Ecng.Common;

	//http://social.msdn.microsoft.com/Forums/vstudio/en-US/d7eda358-ca16-4164-8773-fd92527c7795/collectionviewsource-sort-not-reflecting-automatically-after-observablecollection-item-property
	public class AutoRefreshCollectionViewSource : CollectionViewSource
	{
		protected override void OnSourceChanged(object oldSource, object newSource)
		{
			if (oldSource != null)
				UnsubscribeSourceEvents(oldSource);

			if (newSource != null)
				SubscribeSourceEvents(newSource);

			base.OnSourceChanged(oldSource, newSource);
		}

		private void UnsubscribeSourceEvents(object source)
		{

			if (source is INotifyCollectionChanged notify)
				notify.CollectionChanged -= OnSourceCollectionChanged;

			if (source is IEnumerable items)
				UnsubscribeItemsEvents(items);
		}

		private void SubscribeSourceEvents(object source)
		{

			if (source is INotifyCollectionChanged notify)
				notify.CollectionChanged += OnSourceCollectionChanged;

			if (source is IEnumerable items)
				SubscribeItemsEvents(items);
		}

		private void UnsubscribeItemEvents(object item)
		{

			if (item is INotifyPropertyChanged notify)
				notify.PropertyChanged -= OnItemPropertyChanged;
		}

		private void SubscribeItemEvents(object item)
		{

			if (item is INotifyPropertyChanged notify)
				notify.PropertyChanged += OnItemPropertyChanged;
		}

		private void UnsubscribeItemsEvents(IEnumerable items)
		{
			foreach (var item in items)
				UnsubscribeItemEvents(item);
		}

		private void SubscribeItemsEvents(IEnumerable items)
		{
			foreach (var item in items)
				SubscribeItemEvents(item);
		}

		private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Reset)
				throw new InvalidOperationException("The action {0} is not supported by {1}".Put(e.Action, GetType()));
			
			if (e.NewItems != null)
				SubscribeItemsEvents(e.NewItems);

			if (e.OldItems != null)
				UnsubscribeItemsEvents(e.OldItems);
		}

		private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!IsViewRefreshNeeded(e.PropertyName))
				return;

			var view = View;

			if (view == null)
				return;

			var current = view.CurrentItem;

			if (view is IEditableCollectionView editableCollectionView)
			{
				editableCollectionView.EditItem(sender);
				editableCollectionView.CommitEdit();
			}
			else
				view.Refresh();

			view.MoveCurrentTo(current);
		}

		private bool IsViewRefreshNeeded(string propertyName)
		{
			return SortDescriptions.Any(sort => string.Equals(sort.PropertyName, propertyName)) || GroupDescriptions.OfType<PropertyGroupDescription>().Any(g => string.Equals(g.PropertyName, propertyName));
		}
	}
}