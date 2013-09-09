namespace Ecng.Xaml.Grids
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using Wintellect.PowerCollections;

	public partial class FormattingWindow
	{
		public static RoutedCommand AddRuleCommand = new RoutedCommand();
		public static RoutedCommand RemoveRuleCommand = new RoutedCommand();
		public static RoutedCommand ApplyRulesCommand = new RoutedCommand();

		private readonly ObservableCollection<FormatRule> _columnFormatRules = new ObservableCollection<FormatRule>();

		public FormattingWindow()
		{
			InitializeComponent();
			ColumnFormatRulesCtrl.ItemsSource = _columnFormatRules;
		}

		public Action Applied;

		private MultiDictionary<DataGridColumn, FormatRule> _formatRules;
		private readonly MultiDictionary<DataGridColumn, FormatRule> _formatRulesCopy = new MultiDictionary<DataGridColumn, FormatRule>(false);

		public MultiDictionary<DataGridColumn, FormatRule> FormatRules
		{
			get { return _formatRules; }
			set
			{
				_formatRules = value;
				Copy(_formatRules, _formatRulesCopy);
			}
		}

		private static void Copy(IEnumerable<KeyValuePair<DataGridColumn, ICollection<FormatRule>>> source, MultiDictionary<DataGridColumn, FormatRule> dest)
		{
			dest.Clear();
			dest.AddRange(source.Select(p => new KeyValuePair<DataGridColumn, ICollection<FormatRule>>(p.Key, p.Value.Select(r => r.Clone()).ToArray())));
		}

		public ObservableCollection<DataGridColumn> Columns
		{
			get { return (ObservableCollection<DataGridColumn>)ColumnsCtrl.ItemsSource; }
			set { ColumnsCtrl.ItemsSource = value; }
		}

		private void ColumnsCtrl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			_columnFormatRules.Clear();
			_columnFormatRules.AddRange(_formatRulesCopy[SelectedColumn]);
		}

		public DataGridColumn SelectedColumn
		{
			get { return (DataGridColumn)ColumnsCtrl.SelectedItem; }
			set { ColumnsCtrl.SelectedItem = value; }
		}

		private void AddRuleExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			var rule = new FormatRule();
			_formatRulesCopy.Add(SelectedColumn, rule);
			_columnFormatRules.Add(rule);
		}

		private void AddRuleCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void RemoveRuleExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			_formatRulesCopy.Remove(SelectedColumn, _columnFormatRules.Last());
			_columnFormatRules.Remove(_columnFormatRules.Last());
		}

		private void RemoveRuleCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = _columnFormatRules.Count > 0;
		}

		private void ApplyRulesExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			Copy(_formatRulesCopy, _formatRules);
			Applied.SafeInvoke();
		}

		private void ApplyRulesCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}
	}
}