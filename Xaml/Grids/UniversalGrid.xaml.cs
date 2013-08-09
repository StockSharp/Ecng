namespace Ecng.Xaml.Grids
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Controls.Primitives;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Interop;
	using Ecng.Interop.Dde;
	using Ecng.Serialization;
	using Ecng.Xaml.Converters;

	using Ookii.Dialogs.Wpf;

	using Wintellect.PowerCollections;

	public partial class UniversalGrid : INotifyPropertyChanged, IPersistable
	{
		private DataGridColumnHeader _contextColumnHeader;
		private DataGridCell _contextCell;
		private DataGridCell _dragCell;
		private DataGridColumn _contextColumn;
		private GridLength _prevLength = GridLength.Auto;
		private readonly XlsDdeClient _ddeClient = new XlsDdeClient(new DdeSettings());
		private readonly BlockingQueue<IList<object>> _ddeQueue = new BlockingQueue<IList<object>>(); 

		public static RoutedCommand AddRuleCommand = new RoutedCommand();
		public static RoutedCommand RemoveRuleCommand = new RoutedCommand();
		public static RoutedCommand ApplyRulesCommand = new RoutedCommand();

		public UniversalGrid()
		{
			InitializeComponent();

			ColumnFormatRules = new ObservableCollection<FormatRule>();

			var groupingMembers = new SynchronizedList<string>();
			groupingMembers.Added += Group;
			groupingMembers.Removed += UnGroup;
			groupingMembers.Cleared += UnGroup;
			GroupingMembers = groupingMembers;

			GroupingMemberConverters = new SynchronizedDictionary<string, IValueConverter>();

			FormatRulesPanel.DataContext = this;

			_ddeQueue.Close();
		}

		#region Dependency properties

		public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(IEnumerable), typeof(UniversalGrid), 
			new PropertyMetadata(DataPropertyChangedCallback));

		public IEnumerable Data
		{
			get { return (IEnumerable)GetValue(DataProperty); }
			set { SetValue(DataProperty, value); }
		}

		private static void DataPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			var grid = sender as UniversalGrid;
			if (grid == null)
				return;

			grid.ApplyFormatRules();

			var notifyCollectionChanged = grid.UnderlyingGrid.DataContext as INotifyCollectionChanged;
			if (notifyCollectionChanged != null)
				notifyCollectionChanged.CollectionChanged -= grid.OnDataChanged;

			grid.UnderlyingGrid.DataContext = e.NewValue;

			notifyCollectionChanged = grid.UnderlyingGrid.DataContext as INotifyCollectionChanged;
			if (notifyCollectionChanged != null)
				notifyCollectionChanged.CollectionChanged += grid.OnDataChanged;
		}

		public static readonly DependencyProperty ColumnFormatRulesProperty = DependencyProperty.Register("ColumnFormatRules", typeof(ObservableCollection<FormatRule>), typeof(UniversalGrid), new PropertyMetadata());

		protected ObservableCollection<FormatRule> ColumnFormatRules
		{
			get { return (ObservableCollection<FormatRule>)GetValue(ColumnFormatRulesProperty); }
			private set { SetValue(ColumnFormatRulesProperty, value); }
		}

		public static readonly DependencyProperty ShowHeaderInGroupTitleProperty = DependencyProperty.Register("ShowHeaderInGroupTitle", typeof(bool), typeof(UniversalGrid), new PropertyMetadata(true));

		public bool ShowHeaderInGroupTitle
		{
			get { return (bool)GetValue(ShowHeaderInGroupTitleProperty); }
			set { SetValue(ShowHeaderInGroupTitleProperty, value); }
		}

		public static readonly DependencyProperty RowStyleProperty = DependencyProperty.Register("RowStyle", typeof(Style), typeof(UniversalGrid));

		public Style RowStyle
		{
			get { return (Style)GetValue(RowStyleProperty); }
			set { SetValue(RowStyleProperty, value); }
		}

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
			var autoScroll = (bool)e.NewValue;

			if (autoScroll)
				GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(ctrl.ScrollToEnd);
			else
				GuiDispatcher.GlobalDispatcher.RemovePeriodicalAction(ctrl.ScrollToEnd);

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

		public object SelectedItem
		{
			get
			{
				//При SelectionUnit="Cell" SelectedItem не работает 
				//http://stackoverflow.com/questions/4714325/wpf-datagrid-selectionchanged-event-isnt-raised-when-selectionunit-cell
				return UnderlyingGrid.CurrentItem;
			}
		}

		#region Grouping

		public ICollection<string> GroupingMembers { get; private set; }

		public IDictionary<string, IValueConverter> GroupingMemberConverters { get; private set; }

		private bool _isGroupingActive;

		public bool IsGroupingActive
		{
			get { return _isGroupingActive; }
			private set
			{
				_isGroupingActive = value;
				PropertyChanged.SafeInvoke(this, "IsGroupingActive");
			}
		}

		private void GroupMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			var groupingMember = (string)item.Tag;

			if (item.IsChecked)
				GroupingMembers.Add(groupingMember);
			else
				GroupingMembers.Remove(groupingMember);
		}

		private void Group(string member)
		{
			var column = GetColumn(member);
			if (column == null)
				return;

			var collectionView = (CollectionView)CollectionViewSource.GetDefaultView(Data);
			if (collectionView.GroupDescriptions == null)
				return;

			var converter = GroupingMemberConverters.TryGetValue(column.SortMemberPath);
			collectionView.GroupDescriptions.Add(converter == null 
				? new PropertyGroupDescriptionEx(column.SortMemberPath, column.Header.ToString()) 
				: new PropertyGroupDescriptionEx(column.SortMemberPath, column.Header.ToString(), converter));

			column.Visibility = Visibility.Collapsed;
			IsGroupingActive = true;
		}

		private void UnGroup(string member)
		{
			var column = GetColumn(member);
			if (column == null)
				return;

			var collectionView = (CollectionView)CollectionViewSource.GetDefaultView(Data);
			if (collectionView.GroupDescriptions == null)
				return;

			collectionView.GroupDescriptions.RemoveWhere(g => ((PropertyGroupDescriptionEx)g).PropertyName == member);

			column.Visibility = Visibility.Visible;
			IsGroupingActive = false;
		}

		private void UnGroup()
		{
			var collectionView = (CollectionView)CollectionViewSource.GetDefaultView(Data);
			if (collectionView.GroupDescriptions == null)
				return;

			collectionView.GroupDescriptions.Clear();

			UnderlyingGrid.Columns.ForEach(c => c.Visibility = Visibility.Visible);
			IsGroupingActive = false;
		}

		#endregion

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

		public ObservableCollection<DataGridColumn> Columns
		{
			get { return UnderlyingGrid.Columns; }
		}

		private DataGridColumn SelectedColumn { get; set; }

		private void UnderlyingGrid_Loaded(object sender, RoutedEventArgs e)
		{
			GenerateMenuItems();
		}

		private void UnderlyingGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			_contextCell = null;
			_contextColumnHeader = null;
			_contextColumn = null;

			var dependencyObject = (DependencyObject)e.OriginalSource;

			while ((dependencyObject != null) && !(dependencyObject is DataGridCell) && !(dependencyObject is DataGridColumnHeader))
			{
				dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
			}

			if (dependencyObject is DataGridColumnHeader)
			{
				_contextColumnHeader = dependencyObject as DataGridColumnHeader;
				_contextColumn = _contextColumnHeader.Column;
			}

			if (dependencyObject is DataGridCell)
			{
				_contextCell = dependencyObject as DataGridCell;
				_contextColumn = _contextCell.Column;
				CellMouseRightButtonUp.SafeInvoke(_contextCell, e);
			}

			//PropertyChanged.SafeInvoke(this, "IsColumnSelected");
		}

		private void UnderlyingGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var cell = GetCell(e);

			if (cell == null)
				return;

			if (CanDrag == null || CanDrag(cell))
				_dragCell = cell;
		}

		private void UnderlyingGrid_PreviewMouseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Released)
				_dragCell = null;
		}

		private void UnderlyingGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (_dragCell == null)
				return;

			var dropCell = GetCell(e);

			if (dropCell == null)
				return;

			if (Dropping == null || Dropping(_dragCell, dropCell))
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
		}

		private void UnderlyingGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			var cells = UnderlyingGrid.SelectedCells;

			SelectedColumn = cells.IsEmpty() ? null : cells.First().Column;

			if (FormatRulesPanel.Visibility == Visibility.Visible)
			{
				FormatRulesPanel.IsEnabled = SelectedColumn != null;

				if (FormatRulesPanel.IsEnabled)
					ShowFormatRules();
				else
					ColumnFormatRules.Clear();
			}

			SelectionChanged.SafeInvoke(this);
		}

		private void GenerateMenuItems()
		{
			var menu = (ContextMenu) FindResource("DataGridColumnHeaderContextMenu");

			var items = ((MenuItem)menu.Items[4]).Items;
			var groupItems = ((MenuItem)menu.Items[0]).Items;

			((MenuItem)menu.Items[1]).SetBindings(MenuItem.IsCheckedProperty, this, "ShowHeaderInGroupTitle");
			((MenuItem)menu.Items[2]).SetBindings(MenuItem.IsCheckedProperty, this, "AutoScroll");

			if (items.Count != 0)
				return;

			foreach (var item in UnderlyingGrid.Columns)
			{
				items.Add(CreateColumnMenuItem(item));
				groupItems.Add(CreateGroupMenuItem(item));
			}
		}

		private MenuItem CreateColumnMenuItem(DataGridColumn item)
		{
			var menuItem = new MenuItem
			{
				Header = item.Header,
				IsCheckable = true,
				Tag = item.SortMemberPath,
			};

			menuItem.SetBindings(MenuItem.IsCheckedProperty, item, "Visibility", BindingMode.TwoWay, new VisibilityToBoolConverter());

			menuItem.Checked += ShowCheckedColumn;
			menuItem.Unchecked += HideUncheckedColumn;

			return menuItem;
		}

		private MenuItem CreateGroupMenuItem(DataGridColumn item)
		{
			var menuItem = new MenuItem
			{
				Header = item.Header,
				IsChecked = GroupingMembers.Contains(item.SortMemberPath),
				IsCheckable = true,
				Tag = item.SortMemberPath,
			};

			menuItem.Checked += GroupMenu_Click;
			menuItem.Unchecked += GroupMenu_Click;

			return menuItem;
		}

		private void HideUncheckedColumn(object sender, RoutedEventArgs e)
		{
			var column = GetColumn(((MenuItem)sender).Tag);
			column.Visibility = Visibility.Collapsed;
			PropertyChanged.SafeInvoke(column, "Visibility");
		}

		private void ShowCheckedColumn(object sender, RoutedEventArgs e)
		{
			var column = GetColumn(((MenuItem)sender).Tag);
			column.Visibility = Visibility.Visible;
			PropertyChanged.SafeInvoke(column, "Visibility");
		}

		private DataGridColumn GetColumn(object member)
		{
			return UnderlyingGrid.Columns.Single(c => c.SortMemberPath == (string)member);
		}

		private static DataGridCell GetCell(RoutedEventArgs e)
		{
			var dependencyObject = (DependencyObject)e.OriginalSource;

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

		public event EventHandler<EventArgs> SelectionChanged;

		private void Format_Click(object sender, RoutedEventArgs e)
		{
			if (FormatRulesPanel.Visibility == Visibility.Collapsed)
			{
				ShowFormatRules();
				FormatRulesPanel.Visibility = Visibility.Visible;
				HidableColumnDef.Width = _prevLength;
			}
			else
			{
				FormatRulesPanel.Visibility = Visibility.Collapsed;
				_prevLength = HidableColumnDef.Width;
				HidableColumnDef.Width = GridLength.Auto;
			}
		}

		private void ShowFormatRules()
		{
			ColumnFormatRules.Clear();
			ColumnFormatRules.AddRange(FormatRules[SelectedColumn]);
		}

		private void AddRuleExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var rule = new FormatRule();

			FormatRules.Add(_contextColumn, rule);
			ColumnFormatRules.Add(rule);
		}

		private void AddRuleCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void RemoveRuleExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			FormatRules.Remove(SelectedColumn, ColumnFormatRules.Last());
			ColumnFormatRules.Remove(ColumnFormatRules.Last());
		}

		private void RemoveRuleCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = ColumnFormatRules != null && ColumnFormatRules.Count > 0;
		}

		private void ApplyRulesExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			ApplyRules();
			PropertyChanged.SafeInvoke(this, "FormatRules");
		}

		private void ApplyRulesCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
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
			var selectedColumn = SelectedColumn;

			foreach (var column in UnderlyingGrid.Columns)
			{
				SelectedColumn = column;
				ApplyRules();
			}

			SelectedColumn = selectedColumn;
		}

		private void ApplyRules()
		{
			var column = SelectedColumn;

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
					new Setter(Control.BackgroundProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockBackgroundConverter,
					}),
					new Setter(Control.ForegroundProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockForegroundConverter,
					}),
					new Setter(Control.FontFamilyProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockFontFamilyConverter,
					}),
					new Setter(Control.FontSizeProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockFontSizeConverter,
					}),
					new Setter(Control.FontStretchProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockFontStretchConverter,
					}),
					new Setter(Control.FontStyleProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockFontStyleConverter,
					}),
					new Setter(Control.FontWeightProperty, new Binding
					{
						RelativeSource = RelativeSource.Self,
						Converter = textBlockFontWeightConverter,
					})
				});

				style.Triggers.Add(new DataTrigger
				{
					Binding = new Binding("IsSelected")
					{
						RelativeSource = new RelativeSource
						{
							Mode = RelativeSourceMode.FindAncestor,
							AncestorType = typeof(DataGridRow)
						}
					},
					Value = true,
					Setters = { new Setter(Control.ForegroundProperty, SystemColors.HighlightTextBrush) }
				});

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

				style.Triggers.AddRange(new[]
				{
					(TriggerBase) new DataTrigger
					{
						Binding = new Binding("IsSelected")
						{
							RelativeSource = new RelativeSource
							{
								Mode = RelativeSourceMode.FindAncestor,
								AncestorType = typeof (DataGridRow)
							}
						},
						Value = true,
						Setters = {new Setter(TextBlock.ForegroundProperty, SystemColors.HighlightTextBrush)}
					},
					new MultiDataTrigger
					{
						Conditions =
						{
							new Condition(new Binding("IsSelected")
							{
								RelativeSource = new RelativeSource
								{
									Mode = RelativeSourceMode.FindAncestor,
									AncestorType = typeof (DataGridRow)
								}
							}, true),
							new Condition(new Binding("IsKeyboardFocusWithin")
							{
								RelativeSource = new RelativeSource
								{
									Mode = RelativeSourceMode.FindAncestor,
									AncestorType = typeof (DataGrid)
								}
							}, false)
						},
						Setters = { new Setter(TextBlock.ForegroundProperty, SystemColors.ControlTextBrush) }
					}
				});

				((DataGridTextColumn)column).ElementStyle = style;
			}

			//_gridCollectionView.Refresh();
		}

		private void UnderlyingGrid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			//var dataContext = e.Row.DataContext;

			//if (dataContext == null)
			//    return;

			//var t = dataContext.GetType();
			//var p = t.GetProperty(_contextColumn.SortMemberPath);
			//var v = (decimal)p.GetValue(dataContext, null);

			//e.Row.Background = GetFormatterValue(v, rule => rule.BackgroundColor) ?? new SolidColorBrush(Colors.White);
		}

		private void UnderlyingGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
		{
			//e.Row.Background = null;
		}

		private void UnderlyingGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			if ((e.ExtentHeight - e.VerticalOffset - e.ViewportHeight).Abs() <= double.Epsilon)
			{
				AutoScroll = true;
			}
			else if (e.ExtentHeightChange < double.Epsilon)
			{
				AutoScroll = false;
			}

			//if (e.ExtentHeightChange > 0.0)
			//	((ScrollViewer)e.OriginalSource).ScrollToEnd();
		}

		private void ScrollToEnd()
		{
			var scroll = UnderlyingGrid.FindVisualChild<ScrollViewer>();
			if (scroll != null)
				scroll.ScrollToEnd();
		}

		public void Load(SettingsStorage storage)
		{
			GroupingMembers.Clear();
			GroupingMembers.AddRange(storage.GetValue("GroupingMembers", Enumerable.Empty<string>()));

			ShowHeaderInGroupTitle = storage.GetValue("ShowHeaderInGroupTitle", true);
			AutoScroll = storage.GetValue("AutoScroll", false);

			var ddeSettings = storage.GetValue<SettingsStorage>("DdeSettings");
			if (ddeSettings != null)
				_ddeClient.Settings.Load(ddeSettings);

			FormatRules.Clear();

			var index = 0;
			foreach (var colStorage in storage.GetValue<SettingsStorage[]>("Columns"))
			{
				var column = Columns[index];

				column.SortDirection = colStorage.GetValue<ListSortDirection?>("SortDirection");
				column.Width = new DataGridLength(colStorage.GetValue("WidthValue", column.Width.Value), colStorage.GetValue("WidthType", column.Width.UnitType));
				column.Visibility = colStorage.GetValue<Visibility>("Visibility");

				var displayIndex = colStorage.GetValue<int>("DisplayIndex");
				if (displayIndex > -1 && displayIndex < Columns.Count)
					column.DisplayIndex = displayIndex;

				var rules = colStorage.GetValue<SettingsStorage[]>("FormatRules");
				if (rules != null)
				{
					FormatRules.AddMany(column, rules.Select(r => r.Load<FormatRule>()));
				}

				index++;
			}

			ApplyFormatRules();
		}

		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Columns", Columns.Select(column =>
			{
				var colStorage = new SettingsStorage();

				colStorage.SetValue("SortDirection", column.SortDirection);
				colStorage.SetValue("WidthType", column.Width.UnitType);
				colStorage.SetValue("WidthValue", column.Width.Value);
				colStorage.SetValue("Visibility", column.Visibility);
				colStorage.SetValue("DisplayIndex", column.DisplayIndex);

				var rules = FormatRules.TryGetValue(column);
				if (rules != null)
				{
					colStorage.SetValue("FormatRules", rules.Select(r => r.Save()).ToArray());
				}

				return colStorage;
			}).ToArray());

			storage.SetValue("AutoScroll", AutoScroll);
			storage.SetValue("GroupingMembers", GroupingMembers.ToArray());
			storage.SetValue("ShowHeaderInGroupTitle", ShowHeaderInGroupTitle);
			storage.SetValue("DdeSettings", _ddeClient.Settings.Save());
		}

		private string GetText(string separator)
		{
			var text = new StringBuilder();

			text
				.Append(UnderlyingGrid.Columns.Select(c => c.Header as string).Join(separator))
				.AppendLine();

			foreach (var i in UnderlyingGrid.Items)
			{
				var item = i;

				text
					.Append(UnderlyingGrid.Columns.Select(column =>
					{
						var tb = column.GetCellContent(item) as TextBlock;
						return tb != null ? tb.Text : string.Empty;
					}).Join(separator))
					.AppendLine();
			}

			return text.ToString();
		}

		private void ExportClipBoardText_OnClick(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(GetText("\t"));
		}

		private void ExportClipBoardImage_OnClick(object sender, RoutedEventArgs e)
		{
			UnderlyingGrid.GetImage().CopyToClipboard();
		}

		private void ExportCsv_OnClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				RestoreDirectory = true,
				Filter = @"CSV files (*.csv)|*.csv|All files (*.*)|*.*",
			};

			if (dlg.ShowDialog(this.GetWindow()) == true)
				File.WriteAllText(dlg.FileName, GetText(";"));
		}

		private void ExportExcel_OnClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				RestoreDirectory = true,
				Filter = @"xls files (*.xls)|*.xls|All files (*.*)|*.*",
			};

			if (dlg.ShowDialog(this.GetWindow()) == true)
			{
				using (var worker = new ExcelWorker())
				{
					var colIndex = 0;

					foreach (var column in UnderlyingGrid.Columns)
					{
						worker.SetCell(colIndex, 0, column.Header);
						colIndex++;
					}

					var rowIndex = 1;

					foreach (var item in UnderlyingGrid.Items)
					{
						colIndex = 0;

						foreach (var column in UnderlyingGrid.Columns)
						{
							var tb = column.GetCellContent(item) as TextBlock;
							worker.SetCell(colIndex, rowIndex, tb != null ? tb.Text : string.Empty);
							colIndex++;
						}

						rowIndex++;
					}

					worker.Save(dlg.FileName);
				}
			}
		}

		private void ExportPng_OnClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaSaveFileDialog
			{
				RestoreDirectory = true,
				Filter = @"Image files (*.png)|*.png|All files (*.*)|*.*",
			};

			if (dlg.ShowDialog(this.GetWindow()) == true)
				UnderlyingGrid.GetImage().SaveImage(dlg.FileName);
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

					var list = Data as IList;

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
								UnderlyingGrid.Columns.Select(c => c.Header).ToList()
							};

							rows.AddRange(from object item in UnderlyingGrid.Items select ToRow(item));

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
			return UnderlyingGrid
				.Columns
				.Select(column => column.GetCellContent(item) as TextBlock)
				.Select(tb => tb != null ? tb.Text : string.Empty)
				.Cast<object>()
				.ToList();
		}

		private void OnDataChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_ddeQueue.IsClosed)
				return;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					if (e.NewItems != null)
					{
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
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}