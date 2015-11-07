namespace Ecng.Xaml.Grids
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Linq;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;
	using System.Windows.Documents;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Interop.Dde;
	using Ecng.Localization;
	using Ecng.Serialization;
	using Ecng.Xaml.Converters;

	using MoreLinq;

	using Ookii.Dialogs.Wpf;

	using Wintellect.PowerCollections;

	public class UniversalGrid : DataGrid, INotifyPropertyChanged, IPersistable
	{
		//private DataGridColumnHeader _contextColumnHeader;
		private DataGridCell _contextCell;
		private DataGridCell _dragCell;
		private readonly XlsDdeClient _ddeClient = new XlsDdeClient(new DdeSettings());
		private readonly BlockingQueue<IList<object>> _ddeQueue = new BlockingQueue<IList<object>>();

		//static UniversalGrid()
		//{
		//	DefaultStyleKeyProperty.OverrideMetadata(typeof(UniversalGrid), new FrameworkPropertyMetadata(typeof(UniversalGrid)));
		//}

		public UniversalGrid()
		{
			//InitializeComponent();

			AutoGenerateColumns = false;
			CanUserAddRows = false;
			IsReadOnly = true;
			SelectionMode = DataGridSelectionMode.Single;
			SelectionUnit = DataGridSelectionUnit.FullRow;
			EnableColumnVirtualization = EnableRowVirtualization = true;
			HorizontalGridLinesBrush = Brushes.DarkGray;
			VerticalGridLinesBrush = Brushes.DarkGray;
			RowHeaderWidth = 0;

			Loaded += UniversalGrid_Loaded;

			var groupingColumns = new SynchronizedList<DataGridColumn>();
			groupingColumns.Added += Group;
			groupingColumns.Removed += UnGroup;
			groupingColumns.Clearing += UnGroup;
			GroupingColumns = groupingColumns;

			GroupingColumnConverters = new SynchronizedDictionary<string, IValueConverter>();

			_ddeQueue.Close();

			ContextMenu = new ContextMenu();

			ContextMenu.Items.Add(new MenuItem { Header = "Grouping".Translate() });
			ContextMenu.Items.Add(new MenuItem { Header = "Show column header in grouping".Translate(), IsCheckable = true });
			ContextMenu.Items.Add(new MenuItem { Header = "Auto scroll".Translate(), IsCheckable = true });
			ContextMenu.Items.Add(new Separator());
			ContextMenu.Items.Add(new MenuItem { Header = "Available columns".Translate() });
			ContextMenu.Items.Add(new Separator());

			var formatMi = new MenuItem { Header = "Format".Translate() };
			formatMi.Click += Format_Click;
			ContextMenu.Items.Add(formatMi);

			ContextMenu.Items.Add(new Separator());

			var selectMi = new MenuItem { Header = "Export".Translate() };
			ContextMenu.Items.Add(selectMi);

			var clipboardTxtMi = new MenuItem { Header = "Clipboard (as csv)".Translate() };
			clipboardTxtMi.Click += ExportClipBoardText_OnClick;
			selectMi.Items.Add(clipboardTxtMi);

			var clipboardImageMi = new MenuItem { Header = "Clipboard (as image)".Translate() };
			clipboardImageMi.Click += ExportClipBoardImage_OnClick;
			selectMi.Items.Add(clipboardImageMi);

			var exportCsvMi = new MenuItem { Header = "CSV file...".Translate() };
			exportCsvMi.Click += ExportCsv_OnClick;
			selectMi.Items.Add(exportCsvMi);

			var exportExcelMi = new MenuItem { Header = "Excel file...".Translate() };
			exportExcelMi.Click += ExportExcel_OnClick;
			selectMi.Items.Add(exportExcelMi);

			var exportPngMi = new MenuItem { Header = "PNG file...".Translate() };
			exportPngMi.Click += ExportPng_OnClick;
			selectMi.Items.Add(exportPngMi);

			var exportDdeMi = new MenuItem { Header = "DDE...".Translate() };
			exportDdeMi.Click += ExportDde_OnClick;
			selectMi.Items.Add(exportDdeMi);

			var dict = new ResourceDictionary
			{
				Source = new Uri("pack://application:,,,/Ecng.Xaml;component/Grids/UniversalGridRes.xaml")
			};

			GroupStyle.Add(new GroupStyle
			{
				ContainerStyle = (Style)dict["GroupHeaderStyle"],
				Panel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(DataGridRowsPresenter)))
			});

			ColumnHeaderStyle = (Style)dict["ColumnHeaderStyle"];

			HorizontalGridLinesBrush = Brushes.LightGray;
			VerticalGridLinesBrush = Brushes.LightGray;

			Columns.CollectionChanged += ColumnsOnCollectionChanged;
		}

		private void ColumnsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (!_loaded)
				return;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
				{
					if (e.NewItems == null)
						return;

					e.NewItems.Cast<DataGridColumn>().ForEach(GenerateColumnMenu);
					break;
				}
				case NotifyCollectionChangedAction.Remove:
				{
					if (e.OldItems == null)
						return;

					var items = ((MenuItem)ContextMenu.Items[4]).Items;
					var groupItems = ((MenuItem)ContextMenu.Items[0]).Items;

					e.OldItems.Cast<DataGridColumn>().ForEach(c =>
					{
						var mi = items.OfType<MenuItem>().FirstOrDefault(i => i.Tag == c);

						if (mi != null)
						{
							mi.Checked -= ShowCheckedColumn;
							mi.Unchecked -= HideUncheckedColumn;

							items.Remove(mi);
						}

						var groupMenuItem = groupItems.OfType<MenuItem>().FirstOrDefault(i => i.Tag == c);

						if (groupMenuItem != null)
						{
							groupMenuItem.Checked -= GroupMenu_Click;
							groupMenuItem.Unchecked -= GroupMenu_Click;

							groupItems.Remove(groupMenuItem);
						}
					});
					break;
				}
			}
		}

		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			ApplyFormatRules();

			var notifyCollectionChanged = oldValue as INotifyCollectionChanged;
			if (notifyCollectionChanged != null)
				notifyCollectionChanged.CollectionChanged -= OnDataChanged;

			notifyCollectionChanged = newValue as INotifyCollectionChanged;
			if (notifyCollectionChanged != null)
				notifyCollectionChanged.CollectionChanged += OnDataChanged;

			if (_isGroupingPending)
				GroupingColumns.ForEach(Group);

			base.OnItemsSourceChanged(oldValue, newValue);
		}

		#region Dependency properties

		public static readonly DependencyProperty ShowHeaderInGroupTitleProperty = DependencyProperty.Register("ShowHeaderInGroupTitle", typeof(bool), typeof(UniversalGrid), new PropertyMetadata(true));

		public bool ShowHeaderInGroupTitle
		{
			get { return (bool)GetValue(ShowHeaderInGroupTitleProperty); }
			set { SetValue(ShowHeaderInGroupTitleProperty, value); }
		}

		//public static readonly DependencyProperty RowStyleProperty = DependencyProperty.Register("RowStyle", typeof(Style), typeof(UniversalGrid));

		//public Style RowStyle
		//{
		//	get { return (Style)GetValue(RowStyleProperty); }
		//	set { SetValue(RowStyleProperty, value); }
		//}

		public static readonly DependencyProperty IsGroupsExpandedProperty = DependencyProperty.Register("IsGroupsExpanded", typeof(bool), typeof(UniversalGrid), new PropertyMetadata(true));

		public bool IsGroupsExpanded
		{
			get { return (bool)GetValue(IsGroupsExpandedProperty); }
			set { SetValue(IsGroupsExpandedProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="AutoScroll"/>.
		/// </summary>
		public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register("AutoScroll", typeof(bool), typeof(UniversalGrid), new PropertyMetadata(false, AutoScrollChanged));

		private static void AutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d.FindLogicalChild<UniversalGrid>();

			ctrl.PropertyChanged.SafeInvoke(ctrl, "AutoScroll");
		}

		/// <summary>
		/// Автоматически скролировать контрол на последнюю добавленную строку. По умолчанию false.
		/// </summary>
		public bool AutoScroll
		{
			get { return (bool)GetValue(AutoScrollProperty); }
			set { SetValue(AutoScrollProperty, value); }
		}

		#endregion

		//public object SelectedItem
		//{
		//	get
		//	{
		//		//При SelectionUnit="Cell" SelectedItem не работает 
		//		//http://stackoverflow.com/questions/4714325/wpf-datagrid-selectionchanged-event-isnt-raised-when-selectionunit-cell
		//		return CurrentItem;
		//	}
		//}

		#region Grouping

		public ICollection<DataGridColumn> GroupingColumns { get; private set; }

		public IDictionary<string, IValueConverter> GroupingColumnConverters { get; private set; }

		private void GroupMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var groupingColumn = (DataGridColumn)item.Tag;

			if (item.IsChecked)
				GroupingColumns.Add(groupingColumn);
			else
				GroupingColumns.Remove(groupingColumn);
		}

		private bool _isGroupingPending;

		private void ChangeView(Action<IList<GroupDescription>> handler)
		{
			var view = (CollectionView)CollectionViewSource.GetDefaultView(ItemsSource);

			_isGroupingPending = view == null || view.GroupDescriptions == null;

			if (_isGroupingPending)
				return;

			handler(view.GroupDescriptions);
			PropertyChanged.SafeInvoke(this, "GroupingColumns");
		}

		private void Group(DataGridColumn column)
		{
			if (column == null)
				throw new ArgumentNullException("column");

			column.Visibility = Visibility.Collapsed;

			ChangeView(desc =>
			{
				var converter = GroupingColumnConverters.TryGetValue(column.SortMemberPath);
				desc.Add(converter == null
					? new PropertyGroupDescriptionEx(column.SortMemberPath, column.Header.ToString())
					: new PropertyGroupDescriptionEx(column.SortMemberPath, column.Header.ToString(), converter));
			});
		}

		private void UnGroup(DataGridColumn column)
		{
			if (column == null)
				throw new ArgumentNullException("column");

			column.Visibility = Visibility.Visible;
			ChangeView(desc => desc.RemoveWhere(g => ((PropertyGroupDescriptionEx)g).PropertyName == column.SortMemberPath));
		}

		private void UnGroup()
		{
			ChangeView(desc => desc.Clear());
			GroupingColumns.ForEach(c => c.Visibility = Visibility.Visible);
		}

		#endregion

		public void SetSort(DataGridColumn column, ListSortDirection sortDirection)
		{
			var dataView = CollectionViewSource.GetDefaultView(ItemsSource);

			column.SortDirection = sortDirection;

			dataView.SortDescriptions.Clear();
			dataView.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));
			
			dataView.Refresh();
		}

		public void RefreshSort()
		{
			var dataView = CollectionViewSource.GetDefaultView(ItemsSource);

			if (dataView == null)
				return;

			dataView.Refresh();
		}

		private readonly MultiDictionary<DataGridColumn, FormatRule> _formatRules = new MultiDictionary<DataGridColumn, FormatRule>(false);

		public MultiDictionary<DataGridColumn, FormatRule> FormatRules
		{
			get { return _formatRules; }
		}

		public Func<DataGridCell, bool> CanDrag;
		public Func<DataGridCell, DataGridCell, bool> Dropping;
		public Action<DataGridCell, MouseButtonEventArgs> CellMouseLeftButtonUp;
		public Action<DataGridCell, MouseButtonEventArgs> CellMouseRightButtonUp;
		public event Action<Exception> ErrorHandler; 

		//public ObservableCollection<DataGridColumn> Columns
		//{
		//	get { return UnderlyingGrid.Columns; }
		//}

		private DataGridColumn SelectedColumn { get; set; }

		private bool _loaded;

		private void UniversalGrid_Loaded(object sender, RoutedEventArgs e)
		{
			GenerateMenuItems();

			_loaded = true;
		}

		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			_contextCell = null;
			//_contextColumnHeader = null;
			//_contextColumn = null;

			var dependencyObject = (DependencyObject)e.OriginalSource;

			while ((dependencyObject != null) && !(dependencyObject is DataGridCell) && !(dependencyObject is DataGridColumnHeader))
			{
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}

			//if (dependencyObject is DataGridColumnHeader)
			//{
			//	_contextColumnHeader = dependencyObject as DataGridColumnHeader;
			//	_contextColumn = _contextColumnHeader.Column;
			//}

			if (dependencyObject is DataGridCell)
			{
				_contextCell = dependencyObject as DataGridCell;
				//_contextColumn = _contextCell.Column;
				CellMouseRightButtonUp.SafeInvoke(_contextCell, e);
			}

			//PropertyChanged.SafeInvoke(this, "IsColumnSelected");

			base.OnMouseRightButtonDown(e);
		}

		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			var cell = GetCell(e);

			if (cell == null)
				return;

			var evt = CanDrag;
			if (evt != null && evt(cell))
				_dragCell = cell;

			base.OnPreviewMouseLeftButtonDown(e);
		}

		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Released)
				_dragCell = null;

			base.OnPreviewMouseMove(e);
		}

		protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (_dragCell == null)
				return;

			var dropCell = GetCell(e);

			if (dropCell == null)
				return;

			var evt = Dropping;
			if (evt != null && evt(_dragCell, dropCell))
			{
				var dragValue = _dragCell.GetValueFromCell();
				var dropValue = dropCell.GetValueFromCell();

				if (dragValue != null && dropValue != null && dragValue.GetType() == dropValue.GetType())
				{
					var obj = dropCell.DataContext;

					dropCell.SetValueToCell(dragValue);
					dropCell.DataContext = null;
					dropCell.DataContext = obj;
				}
			}
			else
			{
				CellMouseLeftButtonUp.SafeInvoke(dropCell, e);
			}

			_dragCell = null;

			base.OnPreviewMouseRightButtonDown(e);
		}

		protected override void OnSelectedCellsChanged(SelectedCellsChangedEventArgs e)
		{
			var cells = SelectedCells;

			SelectedColumn = cells.IsEmpty() ? null : cells.First().Column;
			//SelectionChanged.SafeInvoke(this);

			base.OnSelectedCellsChanged(e);
		}

		protected override void OnSorting(DataGridSortingEventArgs eventArgs)
		{
			PropertyChanged.SafeInvoke(this, "Sorting");
			base.OnSorting(eventArgs);
		}

		protected override void OnColumnReordered(DataGridColumnEventArgs e)
		{
			PropertyChanged.SafeInvoke(this, "Reordered");
			base.OnColumnReordered(e);
		}

		private void GenerateMenuItems()
		{
			var menu = ContextMenu;

			var items = ((MenuItem)menu.Items[4]).Items;

			((MenuItem)menu.Items[1]).SetBindings(MenuItem.IsCheckedProperty, this, "ShowHeaderInGroupTitle");
			((MenuItem)menu.Items[2]).SetBindings(MenuItem.IsCheckedProperty, this, "AutoScroll");

			if (items.Count != 0)
				return;

			foreach (var column in Columns)
			{
				GenerateColumnMenu(column);
			}
		}

		private void GenerateColumnMenu(DataGridColumn column)
		{
			var items = ((MenuItem)ContextMenu.Items[4]).Items;
			var groupItems = ((MenuItem)ContextMenu.Items[0]).Items;

			var menuItem = new MenuItem
			{
				Header = column.Header,
				IsCheckable = true,
				Tag = column,
			};

			menuItem.SetBindings(MenuItem.IsCheckedProperty, column, "Visibility", BindingMode.TwoWay, new VisibilityToBoolConverter());

			menuItem.Checked += ShowCheckedColumn;
			menuItem.Unchecked += HideUncheckedColumn;

			items.Add(menuItem);

			var groupMenuItem = new MenuItem
			{
				Header = column.Header,
				IsChecked = GroupingColumns.Contains(column),
				IsCheckable = true,
				Tag = column,
			};

			groupMenuItem.Checked += GroupMenu_Click;
			groupMenuItem.Unchecked += GroupMenu_Click;
			groupItems.Add(groupMenuItem);
		}

		private void HideUncheckedColumn(object sender, RoutedEventArgs e)
		{
			var column = (DataGridColumn)((MenuItem)sender).Tag;
			column.Visibility = Visibility.Collapsed;
			PropertyChanged.SafeInvoke(column, "Visibility");
		}

		private void ShowCheckedColumn(object sender, RoutedEventArgs e)
		{
			var column = (DataGridColumn)((MenuItem)sender).Tag;
			column.Visibility = Visibility.Visible;
			PropertyChanged.SafeInvoke(column, "Visibility");
		}

		private static DataGridCell GetCell(RoutedEventArgs e)
		{
			var dependencyObject = (DependencyObject)e.OriginalSource;

			if (dependencyObject is Hyperlink || dependencyObject is Run)
				return null;

			while ((dependencyObject != null) && !(dependencyObject is DataGridCell))
			{
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}

			if (dependencyObject != null)
			{
				return dependencyObject as DataGridCell;
			}

			return null;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		//public event EventHandler<EventArgs> SelectionChanged;

		private void Format_Click(object sender, RoutedEventArgs e)
		{
			Action applied = () =>
			{
				ApplyFormatRules();
				PropertyChanged.SafeInvoke(this, "FormatRules");
			};

			var wnd = new FormattingWindow
			{
				FormatRules = FormatRules,
				Columns = Columns,
				SelectedColumn = SelectedColumn,
				Applied = applied
			};

			if (wnd.ShowModal(this))
				applied();
		}

		private T GetFormatterValue<T>(DataGridColumn column, object cellValue, Func<FormatRule, T> getPart)
		{
			if (getPart == null)
				throw new ArgumentNullException("getPart");

			var value = getPart(FormatRule.Default);

			foreach (var rule in FormatRules[column])
			{
				bool isMatched;

				switch (rule.Condition)
				{
					case ComparisonOperator.Equal:
						isMatched = cellValue.Compare(rule.Value) == 0;
						break;
					case ComparisonOperator.NotEqual:
						isMatched = cellValue.Compare(rule.Value) != 0;
						break;
					case ComparisonOperator.Greater:
						isMatched = cellValue.Compare(rule.Value) == 1;
						break;
					case ComparisonOperator.GreaterOrEqual:
						isMatched = cellValue.Compare(rule.Value) >= 0;
						break;
					case ComparisonOperator.Less:
						isMatched = cellValue.Compare(rule.Value) == -1;
						break;
					case ComparisonOperator.LessOrEqual:
						isMatched = cellValue.Compare(rule.Value) <= 0;
						break;
					case ComparisonOperator.Any:
						isMatched = true;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (isMatched)
					value = getPart(rule);
			}

			return value;
		}

		private readonly Dictionary<DataGridColumn, Style> _columnStyles = new Dictionary<DataGridColumn, Style>(); 

		public void ApplyFormatRules()
		{
			foreach (var c in Columns)
			{
				if (!FormatRules.ContainsKey(c))
					continue;

				var column = c;

				var valueBackgroundConverter = new FormatConverter<Brush>(v => GetFormatterValue(column, v, rule => rule.Background));
				var textBlockBackgroundConverter = new FormatConverter<Brush>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Background));
				var valueForegroundConverter = new FormatConverter<Brush>(v => GetFormatterValue(column, v, rule => rule.Foreground));
				var textBlockForegroundConverter = new FormatConverter<Brush>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Foreground));

				var valueFontFamilyConverter = new FormatConverter<FontFamily>(v => GetFormatterValue(column, v, rule => rule.Font.Family));
				var textBlockFontFamilyConverter = new FormatConverter<FontFamily>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Font.Family));
				var valueFontWeightConverter = new FormatConverter<FontWeight>(v => GetFormatterValue(column, v, rule => rule.Font.Weight));
				var textBlockFontWeightConverter = new FormatConverter<FontWeight>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Font.Weight));
				var valueFontSizeConverter = new FormatConverter<double>(v => GetFormatterValue(column, v, rule => rule.Font.Size));
				var textBlockFontSizeConverter = new FormatConverter<double>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Font.Size));
				var valueFontStyleConverter = new FormatConverter<FontStyle>(v => GetFormatterValue(column, v, rule => rule.Font.Style));
				var textBlockFontStyleConverter = new FormatConverter<FontStyle>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Font.Style));
				var valueFontStretchConverter = new FormatConverter<FontStretch>(v => GetFormatterValue(column, v, rule => rule.Font.Stretch));
				var textBlockFontStretchConverter = new FormatConverter<FontStretch>(v => GetFormatterValue(column, ((DataGridCell)v).GetValueFromCell(), rule => rule.Font.Stretch));

				if (column is DataGridTemplateColumn)
				{
					var style = _columnStyles.SafeAdd(column, key => column.CellStyle).XamlClone();

					style.Setters.AddRange(new[]
					{
						new Setter(BackgroundProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockBackgroundConverter,
						}),
						new Setter(ForegroundProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockForegroundConverter,
						}),
						new Setter(FontFamilyProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockFontFamilyConverter,
						}),
						new Setter(FontSizeProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockFontSizeConverter,
						}),
						new Setter(FontStretchProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockFontStretchConverter,
						}),
						new Setter(FontStyleProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockFontStyleConverter,
						}),
						new Setter(FontWeightProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Converter = textBlockFontWeightConverter,
						})
					});

					style.AddSelectionTriggers();

					column.CellStyle = style;
				}
				else if (column is DataGridTextColumn)
				{
					var style = _columnStyles.SafeAdd(column, key => ((DataGridTextColumn)column).ElementStyle).XamlClone();

					style.Setters.AddRange(new[]
					{
						new Setter(TextBlock.BackgroundProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueBackgroundConverter,
						}),
						new Setter(TextBlock.ForegroundProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueForegroundConverter,
						}),
						new Setter(TextBlock.FontFamilyProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueFontFamilyConverter,
						}),
						new Setter(TextBlock.FontSizeProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueFontSizeConverter,
						}),
						new Setter(TextBlock.FontStretchProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueFontStretchConverter,
						}),
						new Setter(TextBlock.FontStyleProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueFontStyleConverter,
						}),
						new Setter(TextBlock.FontWeightProperty, new Binding
						{
							RelativeSource = RelativeSource.Self,
							Path = new PropertyPath("Text"),
							Converter = valueFontWeightConverter,
						})
					});

					style.AddSelectionTriggers();

					((DataGridTextColumn)column).ElementStyle = style;
				}
			}
		}

		//protected override void OnLoadingRow(DataGridRowEventArgs e)
		//{
		//	var dataContext = e.Row.DataContext;

		//	if (dataContext == null)
		//		return;

		//	var t = dataContext.GetType();
		//	var p = t.GetProperty(_contextColumn.SortMemberPath);
		//	var v = (decimal)p.GetValue(dataContext, null);

		//	e.Row.Background = GetFormatterValue(v, rule => rule.BackgroundColor) ?? new SolidColorBrush(Colors.White);

		//	base.OnLoadingRow(e);
		//}

		//protected override void OnUnloadingRow(DataGridRowEventArgs e)
		//{
		//	e.Row.Background = null;
		//	base.OnUnloadingRow(e);
		//}

		private void ScrollToEnd()
		{
			var scroll = this.FindVisualChild<ScrollViewer>();
			if (scroll != null)
				scroll.ScrollToVerticalOffset(double.PositiveInfinity);
		}

		public virtual void Load(SettingsStorage storage)
		{
			ShowHeaderInGroupTitle = storage.GetValue("ShowHeaderInGroupTitle", true);
			AutoScroll = storage.GetValue("AutoScroll", false);

			var ddeSettings = storage.GetValue<SettingsStorage>("DdeSettings");
			if (ddeSettings != null)
				_ddeClient.Settings.Load(ddeSettings);

			FormatRules.Clear();

			var index = 0;
			foreach (var colStorage in storage.GetValue<SettingsStorage[]>("Columns"))
			{
				var column = SerializableColumns[index];

				column.SortDirection = colStorage.GetValue<ListSortDirection?>("SortDirection");
				column.Width = new DataGridLength(colStorage.GetValue("WidthValue", column.Width.Value), colStorage.GetValue("WidthType", column.Width.UnitType));
				column.Visibility = colStorage.GetValue<Visibility>("Visibility");

				var displayIndex = colStorage.GetValue<int>("DisplayIndex");
				if (displayIndex > -1 && displayIndex < SerializableColumns.Count)
					column.DisplayIndex = displayIndex;

				var rules = colStorage.GetValue<SettingsStorage[]>("FormatRules");
				if (rules != null)
				{
					FormatRules.AddMany(column, rules.Select(r => r.Load<FormatRule>()));
				}

				index++;
			}

			var colDict = Columns.ToDictionary(c => c.SortMemberPath, c => c);

			GroupingColumns.Clear();
			GroupingColumns.AddRange(storage.GetValue("GroupingColumns", storage.GetValue("GroupingMembers", Enumerable.Empty<string>()))
				.Select(colDict.TryGetValue)
				.Where(c => c != null));

			ApplyFormatRules();
		}

		protected virtual IList<DataGridColumn> SerializableColumns
		{
			get { return Columns; }
		}

		public virtual void Save(SettingsStorage storage)
		{
			storage.SetValue("Columns", SerializableColumns.Select(column =>
			{
				var colStorage = new SettingsStorage();

				colStorage.SetValue("SortDirection", column.SortDirection);
				colStorage.SetValue("WidthType", column.Width.UnitType);
				colStorage.SetValue("WidthValue", column.Width.Value);
				colStorage.SetValue("Visibility", column.Visibility);
				colStorage.SetValue("DisplayIndex", column.DisplayIndex);

				var rules = FormatRules.TryGetValue(column);
				if (rules != null)
					colStorage.SetValue("FormatRules", rules.Select(r => r.Save()).ToArray());

				return colStorage;
			}).ToArray());

			storage.SetValue("AutoScroll", AutoScroll);
			storage.SetValue("GroupingColumns", GroupingColumns.Select(c => c.SortMemberPath).ToArray());
			storage.SetValue("ShowHeaderInGroupTitle", ShowHeaderInGroupTitle);
			storage.SetValue("DdeSettings", _ddeClient.Settings.Save());
		}

		private DataGridColumn[] ExportColumns
		{
			get
			{
				return Columns
					.Where(c => c.Visibility == Visibility.Visible)
					.OrderBy(c => c.DisplayIndex)
					.ToArray();
			}
		}

		private static IEnumerable<string> GetCellValues(object item, IEnumerable<DataGridColumn> columns)
		{
			return columns.Select(column =>
			{
				var value = item.GetPropValue(column.SortMemberPath);
				return value != null ? value.ToString() : string.Empty;
			});
		}

		protected override void OnCopyingRowClipboardContent(DataGridRowClipboardEventArgs e)
		{
			e.ClipboardRowContent.Clear();
			e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, null, GetCellValues(e.Item, ExportColumns).Join("\t")));
		}

		private void ExportClipBoardText_OnClick(object sender, RoutedEventArgs e)
		{
			var text = new StringBuilder();

			var columns = ExportColumns;

			text
				.Append(columns.Select(c => c.Header as string).Join("\t"))
				.AppendLine();

			foreach (var i in Items)
			{
				var item = i;

				text
					.Append(GetCellValues(item, columns).Join("\t"))
					.AppendLine();
			}

			Clipboard.SetText(text.ToString());
		}

		private void ExportClipBoardImage_OnClick(object sender, RoutedEventArgs e)
		{
			this.GetImage().CopyToClipboard();
		}

		private void ExportCsv_OnClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				RestoreDirectory = true,
				Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
				DefaultExt = "csv"
			};

			if (dlg.ShowDialog(this.GetWindow()) == true)
			{
				using (var writer = new CsvFileWriter(dlg.FileName))
				{
					var columns = ExportColumns;

					writer.WriteRow(columns.Select(c => c.Header as string));

					foreach (var item in Items)
					{
						writer.WriteRow(GetCellValues(item, columns));
					}
				}
			}
		}

		private void ExportExcel_OnClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				RestoreDirectory = true,
				Filter = @"xls files (*.xls)|*.xls|All files (*.*)|*.*",
				DefaultExt = "xls"
			};

			if (dlg.ShowDialog(this.GetWindow()) != true)
				return;

			using (var worker = new ExcelWorker())
			{
				var colIndex = 0;

				foreach (var column in Columns)
				{
					worker.SetCell(colIndex, 0, column.Header);
					colIndex++;
				}

				var rowIndex = 1;

				foreach (var item in Items)
				{
					colIndex = 0;

					foreach (var column in Columns)
					{
						var tb = column.GetCellContent(item) as TextBlock;
						worker.SetCell(colIndex, rowIndex, (object) (tb != null ? tb.Text : string.Empty));
						colIndex++;
					}

					rowIndex++;
				}

				worker.Save(dlg.FileName, false);
			}
		}

		private void ExportPng_OnClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				RestoreDirectory = true,
				Filter = @"Image files (*.png)|*.png|All files (*.*)|*.*",
				DefaultExt = "png"
			};

			if (dlg.ShowDialog(this.GetWindow()) == true)
				this.GetImage().SaveImage(dlg.FileName);
		}

		private readonly SyncObject _ddeLock = new SyncObject();
		private bool _isDdeThreadExited;

		private void ExportDde_OnClick(object sender, RoutedEventArgs e)
		{
			new DdeSettingsWindow
			{
				DdeClient = _ddeClient,
				StartedAction = () =>
				{
					_ddeQueue.Open();
					_isDdeThreadExited = false;

					ThreadingHelper
						.Thread(() =>
						{
							try
							{
								while (true)
								{
									IList<object> row;

									if (!_ddeQueue.TryDequeue(out row))
										break;

									_ddeClient.Poke(new[] { row });
								}
							}
							catch (Exception ex)
							{
								ErrorHandler.SafeInvoke(ex);
							}

							lock (_ddeLock)
							{
								_isDdeThreadExited = true;
								_ddeLock.Pulse();
							}
						})
						.Name("UG DDE")
						.Launch();

					var list = ItemsSource;

					if (list == null)
						return;

					foreach (var item in list)
						_ddeQueue.Enqueue(ToRow(item));
				},
				StoppedAction = () =>
				{
					_ddeQueue.Close();

					lock (_ddeLock)
					{
						if (_isDdeThreadExited)
							return;

						_ddeLock.Wait();
					}
				},
				FlushAction = () =>
				{
					try
					{
						using (var client = new XlsDdeClient(_ddeClient.Settings))
						{
							client.Start();

							var rows = new List<IList<object>>
							{
								Columns.Select(c => c.Header).ToList()
							};

							rows.AddRange(from object item in Items select ToRow(item));

							client.Poke(rows);
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.ToString());
					}
				}
			}.ShowModal(this);
		}

		private IList<object> ToRow(object item)
		{
			return Columns
				.Select(column => column.GetCellContent(item) as TextBlock)
				.Select(tb => tb != null ? tb.Text : string.Empty)
				.Cast<object>()
				.ToList();
		}

		private void OnDataChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems != null)
					{
						if(AutoScroll)
							ScrollToEnd();

						if (_ddeQueue.IsClosed)
							break;

						foreach (var newItem in e.NewItems)
							_ddeQueue.Enqueue(ToRow(newItem));
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					break;
				case NotifyCollectionChangedAction.Replace:
					break;
				case NotifyCollectionChangedAction.Move:
					break;
				case NotifyCollectionChangedAction.Reset:
				{
					if (AutoScroll)
						ScrollToEnd();

					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}