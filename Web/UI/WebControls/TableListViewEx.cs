namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.ComponentModel;
	using System.Drawing.Design;
	using System.Web.UI;
	using System.Web.UI.Design.WebControls;
	using System.Web.UI.WebControls;

	using Ecng.Common;

	[ToolboxData("<{0}:TableListViewEx runat=\"server\"></{0}:TableListViewEx>")]
	public class TableListViewEx : ListViewEx
	{
		private sealed class TableListViewExItemTemplate : ITemplate
		{
			private readonly TableListViewEx _parent;
			private int _index;

			public TableListViewExItemTemplate(TableListViewEx parent)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;
			}

			void ITemplate.InstantiateIn(Control container)
			{
				var row = new TableRow();

				_parent.ApplyBodyRowStyle(row);

				foreach (TemplateField column in _parent.Columns)
				{
					var cell = new TableCell
					{
						Width = column.ItemStyle.Width,
						BackColor = column.ItemStyle.BackColor,
						ForeColor = column.ItemStyle.ForeColor,
					};

					_parent.ApplyBodyCellStyle(cell, column, _index);

					if (column.ItemTemplate != null)
						column.ItemTemplate.InstantiateIn(cell);

					row.Cells.Add(cell);
				}

				_index++;

				container.Controls.Add(row);
			}
		}

		protected virtual void ApplyBodyRowStyle(TableRow row)
		{
		}

		protected virtual void ApplyBodyCellStyle(TableCell cell, TemplateField column, int index)
		{
		}

		public TableListViewEx()
		{
			ItemTemplate = new TableListViewExItemTemplate(this);
			IsRenderHeader = true;
			IsRenderBody = true;
		}

		#region Columns

		private DataControlFieldCollection _columns;

		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Editor(typeof(DataControlFieldTypeEditor), typeof(UITypeEditor))]
		[MergableProperty(false)]
		[DefaultValue((string)null)]
		[Category("")]
		[Description("")]
		public virtual DataControlFieldCollection Columns
		{
			get
			{
				if (_columns == null)
				{
					_columns = new DataControlFieldCollection();

					_columns.FieldsChanged += delegate
					{
						if (Initialized)
							RequiresDataBinding = true;
					};

					if (IsTrackingViewState)
						((IStateManager)_columns).TrackViewState();
				}

				return _columns;
			}
		}

		#endregion

		[Bindable(true), Category("Appearance"), DefaultValue(null)]
		public string SortDirectionAscImage { get; set; }

		[Bindable(true), Category("Appearance"), DefaultValue(null)]
		public string SortDirectionDescImage { get; set; }

		[Bindable(true), Category("Appearance"), DefaultValue(true)]
		public bool IsRenderHeader { get; set; }

		[Bindable(true), Category("Appearance"), DefaultValue(true)]
		public bool IsRenderBody { get; set; }

		public string TableStyle { get; set; }

		protected virtual void ApplyHeaderSeparatorStyles(TableCell cell)
		{
		}

		protected virtual HyperLink GetHeaderCellLink(TemplateField column, Url url)
		{
			if (column == null)
				throw new ArgumentNullException(nameof(column));

			if (url == null)
				throw new ArgumentNullException(nameof(url));

			return new HyperLink { Text = column.HeaderText, NavigateUrl = url.ToString() };
		}

		protected virtual TableRow GetHeaderRow()
		{
			var row = new TableRow();

			var first = true;
			foreach (TemplateField column in Columns)
			{
				var cell = new TableCell { Width = column.ItemStyle.Width };

				if (column.SortExpression.IsEmpty())
					cell.Text = column.HeaderText;
				else
				{
					var url = Url.Current;

					var direction = url.QueryString.TryGetValue<SortDirection?>(SortDirectionField);

					Image directionImage;

					if	(
						(url.QueryString.TryGetValue<string>(SortExpressionField) == column.SortExpression) &&
						(!SortDirectionAscImage.IsEmpty() && !SortDirectionDescImage.IsEmpty())
						)
					{
						directionImage = new Image();

						if (direction == null || direction == SortDirection.Ascending)
							directionImage.ImageUrl = SortDirectionAscImage;
						else
							directionImage.ImageUrl = SortDirectionDescImage;
					}
					else
						directionImage = null;

					url.QueryString[SortExpressionField] = column.SortExpression;

					if (direction != null)
						direction = (direction == SortDirection.Ascending) ? SortDirection.Descending : SortDirection.Ascending;
					else
						direction = SortDirection.Ascending;

					url.QueryString[SortDirectionField] = direction;

					cell.Controls.Add(GetHeaderCellLink(column, url));

					if (directionImage != null)
					{
						cell.Controls.Add(new LiteralControl("&nbsp"));
						cell.Controls.Add(directionImage);
					}
				}

				if (!first)
					ApplyHeaderSeparatorStyles(cell);

				if (column.HeaderTemplate != null)
					column.HeaderTemplate.InstantiateIn(cell);

				row.Cells.Add(cell);

				first = false;
			}

			return row;
		}

		protected virtual void InitHeader(Table table)
		{
			var row = GetHeaderRow();
			table.Rows.Add(row);
		}

		//protected virtual void RenderHeader(HtmlTextWriter writer)
		//{
		//    var header = GetHeaderTable();
		//    header.RenderControl(writer);
		//}

		protected virtual void InitBody(Table table)
		{
			if (Controls.Count > 0)
			{
				foreach (Control ctrl in Controls[0].Controls)
					table.Rows.Add((TableRow)ctrl.Controls[0]);
			}
			else
			{
				var bodyRow = new TableRow();
				var bodyCell = new TableCell();

				bodyRow.Cells.Add(bodyCell);
				table.Rows.Add(bodyRow);
			}
		}

		//protected virtual void RenderBody(HtmlTextWriter writer)
		//{
		//    var body = GetBodyTable();
		//    body.RenderControl(writer);
		//}

		#region Control Members

		protected override void Render(HtmlTextWriter writer)
		{
			var table = new Table { Width = new Unit(100, UnitType.Percentage) };

			if (IsRenderHeader)
				InitHeader(table);

			if (!TableStyle.IsEmpty())
				table.CssClass = TableStyle;

			if (IsRenderBody)
				InitBody(table);

			table.RenderControl(writer);
		}

		#endregion
	}
}