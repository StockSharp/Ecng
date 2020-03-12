namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System.ComponentModel;
	using System.Web.UI;
	using System.Web.UI.WebControls;
	using System.Reflection;

	using Ecng.Reflection;

	#endregion

	[ToolboxData("<{0}:ListViewEx runat=\"server\"></{0}:ListViewEx>")]
	public class ListViewEx : ListView
	{
		private sealed class ListViewExLayoutTemplate : ITemplate
		{
			#region ITemplate Members

			void ITemplate.InstantiateIn(Control container)
			{
				container.Controls.Add(new PlaceHolder { ID = "itemPlaceholder" });
			}

			#endregion
		}

		#region Private Fields

		private const string _defaultSortExpressionField = "se";
		private const string _defaultSortDirectionField = "sd";

		//private static readonly FastInvoker<ListView, VoidType, string> _getSortExpression;
		private static readonly FastInvoker<ListView, string, VoidType> _setSortExpression;

		//private static readonly FastInvoker<ListView, VoidType, SortDirection> _getSortDirection;
		private static readonly FastInvoker<ListView, SortDirection, VoidType> _setSortDirection;

		#endregion

		static ListViewEx()
		{
			_setSortExpression = FastInvoker<ListView, string, VoidType>.Create(typeof(ListView).GetMember<PropertyInfo>("SortExpressionInternal"), false);
			_setSortDirection = FastInvoker<ListView, SortDirection, VoidType>.Create(typeof(ListView).GetMember<PropertyInfo>("SortDirectionInternal"), false);
		}

		#region ListViewEx.ctor()

		public ListViewEx()
		{
			base.LayoutTemplate = new ListViewExLayoutTemplate();
		}

		#endregion

		#region SortExpressionField

		private string _sortExpressionField = _defaultSortExpressionField;

		[Category("Appearance")]
		[DefaultValue(_defaultSortExpressionField)]
		public string SortExpressionField
		{
			get => _sortExpressionField;
			set => _sortExpressionField = value;
		}

		#endregion

		#region SortDirectionField

		private string _sortDirectionField = _defaultSortDirectionField;

		[Category("Appearance")]
		[DefaultValue(_defaultSortDirectionField)]
		public string SortDirectionField
		{
			get => _sortDirectionField;
			set => _sortDirectionField = value;
		}

		#endregion

		#region SortExpr

		private string _sortExpr = string.Empty;

		[Category("Appearance")]
		[DefaultValue("")]
		public string SortExpr
		{
			get => _sortExpr;
			set => _sortExpr = value;
		}

		#endregion

		#region SortDir

		private SortDirection _sortDir = SortDirection.Ascending;

		[Category("Appearance")]
		[DefaultValue(SortDirection.Ascending)]
		public SortDirection SortDir
		{
			get => _sortDir;
			set => _sortDir = value;
		}

		#endregion

		protected override DataSourceSelectArguments CreateDataSourceSelectArguments()
		{
			var qs = WebHelper.CurrentUrl.QueryString;
			_setSortExpression.SetValue(this, qs.TryGetValue(SortExpressionField, SortExpr));
			_setSortDirection.SetValue(this, qs.TryGetValue(SortDirectionField, SortDir));
			return base.CreateDataSourceSelectArguments();
		}
	}
}