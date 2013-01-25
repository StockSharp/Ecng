namespace Ecng.Xaml
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Reflection;

	/// <summary>
	/// http://blogs.microsoft.co.il/blogs/justguy/archive/2009/01/19/wpf-combobox-with-checkboxes-as-items-it-will-even-update-on-the-fly.aspx
	/// </summary>
	public partial class CheckComboBox
	{
		private sealed class Node : NotifiableObject
		{
			private readonly object _item;
			private readonly CheckComboBox _parent;
			private readonly string _title;

			public Node(object item, CheckComboBox parent)
			{
				if (item == null)
					throw new ArgumentNullException("item");

				if (parent == null)
					throw new ArgumentNullException("parent");

				_item = item;
				_parent = parent;

				_title = _parent.DisplayMemberPath.IsEmpty()
					? item.ToString()
					: item.GetValue<object, VoidType, string>(_parent.DisplayMemberPath, null);
			}

			public object Item
			{
				get { return _item; }
			}

			public string Title
			{
				get { return _title; }
			}

			private bool _isSelected;

			public bool IsSelected
			{
				get { return _isSelected; }
				set
				{
					_isSelected = value;
					NotifyChanged("IsSelected");
				}
			}
		}

		private sealed class ObservableNodeList : ObservableCollection<Node>
		{
			public IEnumerable<Node> SelectedItems
			{
				get { return Items.Where(n => n.IsSelected); }
			}

			public override string ToString()
			{
				return SelectedItems.Select(n => n.Title).Join(",");
			}
		}

		private readonly ObservableNodeList _source = new ObservableNodeList();

		public CheckComboBox()
		{
			InitializeComponent();
			CheckableCombo.ItemsSource = _source;
		}

		#region ItemsSource

		/// <summary>
		/// Gets or sets a collection used to generate the content of the ComboBox.
		/// </summary>
		public object ItemsSource
		{
			get { return GetValue(ItemsSourceProperty); }
			set
			{
				if (value is INotifyCollectionChanged)
				{
					((INotifyCollectionChanged)value).CollectionChanged += CheckComboBox_CollectionChanged;
				}

				_source.Clear();
				_source.AddRange(((IEnumerable)value).Cast<object>().Select(item => new Node(item, this)));

				SetValue(ItemsSourceProperty, value);
				SetText();
			}
		}

		private void CheckComboBox_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				_source.RemoveRange(_source.Where(n => e.OldItems.Contains(n.Item)));
			}

			if (e.NewItems != null)
			{
				_source.AddRange(e.NewItems.Cast<object>().Select(item => new Node(item, this)));
			}
		}

		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(object), typeof(CheckComboBox), new UIPropertyMetadata(null));

		#endregion

		#region Text

		/// <summary>
		/// Gets or sets the text displayed in the ComboBox.
		/// </summary>
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), typeof(CheckComboBox), new UIPropertyMetadata(string.Empty));

		#endregion

		#region DefaultText

		/// <summary>
		/// Gets or sets the text displayed in the ComboBox if there are no selected items.
		/// </summary>
		public string DefaultText
		{
			get { return (string)GetValue(DefaultTextProperty); }
			set
			{
				SetValue(DefaultTextProperty, value);
				SetText();
			}
		}

		// Using a DependencyProperty as the backing store for DefaultText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DefaultTextProperty =
			 DependencyProperty.Register("DefaultText", typeof(string), typeof(CheckComboBox), new UIPropertyMetadata(string.Empty));

		#endregion

		private readonly List<object> _selectedItems = new List<object>();

		public IEnumerable SelectedItems
		{
			get { return _selectedItems; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				foreach (var i in value)
				{
					var item = i;

					var node = _source.FirstOrDefault(n => n.Item == item);

					if (node != null)
					{
						_selectedItems.Add(item);
						node.IsSelected = true;
					}
				}
			}
		}

		#region SelectionChanged

		public static readonly RoutedEvent SelectionChangedEvent =
			EventManager.RegisterRoutedEvent("SelectionChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CheckComboBox));

		// Provide CLR accessors for the event
		public event RoutedEventHandler SelectionChanged
		{
			add { AddHandler(SelectionChangedEvent, value); }
			remove { RemoveHandler(SelectionChangedEvent, value); }
		}

		#endregion

		#region DisplayMemberPath

		public string DisplayMemberPath
		{
			get { return (string)GetValue(DisplayMemberPathProperty); }
			set { SetValue(DisplayMemberPathProperty, value); }
		}

		public static readonly DependencyProperty DisplayMemberPathProperty =
			 DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(CheckComboBox), new UIPropertyMetadata(string.Empty));

		#endregion

		/// <summary>
		/// Whenever a CheckBox is checked, change the text displayed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CheckBox_Click(object sender, RoutedEventArgs e)
		{
			SetText();
			_selectedItems.Clear();
			_selectedItems.AddRange(_source.SelectedItems.Select(n => n.Item));

			RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
		}

		/// <summary>
		/// Set the text property of this control (bound to the ContentPresenter of the ComboBox).
		/// </summary>
		private void SetText()
		{
			Text = _source.ToString();

			// set DefaultText if nothing else selected
			if (Text.IsEmpty())
			{
				Text = DefaultText;
			}
		}
	}
}