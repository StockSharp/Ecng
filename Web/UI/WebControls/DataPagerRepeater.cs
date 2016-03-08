namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.Linq;
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using Ecng.Common;

	// http://www.codeproject.com/Articles/45163/Extend-Repeater-to-support-DataPager
	[ToolboxData("<{0}:DataPagerRepeater runat=\"server\" PersistentDataSource=true></{0}:DataPagerRepeater>")]
	public class DataPagerRepeater : Repeater, IPageableItemContainer
	{
		public int MaximumRows => ViewState["_maximumRows"] != null
			? (int)ViewState["_maximumRows"]
			: -1;

		public int StartRowIndex => ViewState["_startRowIndex"] != null
			? (int)ViewState["_startRowIndex"]
			: -1;

		public int TotalRows => ViewState["_totalRows"] != null
			? (int)ViewState["_totalRows"]
			: -1;

		public bool PersistentDataSource
		{
			get
			{
				return ViewState["PersistentDataSource"] == null || (bool)ViewState["PersistentDataSource"];
			}
			set
			{
				ViewState["PersistentDataSource"] = value;
			}
		}

		protected override void LoadViewState(object savedState)
		{
			base.LoadViewState(savedState);

			if (Page.IsPostBack)
			{
				if (PersistentDataSource && ViewState["DataSource"] != null)
				{
					DataSource = ViewState["DataSource"];
					DataBind();
				}
			}
		}

		public void SetPageProperties(int startRowIndex, int maximumRows, bool databind)
		{
			ViewState["_startRowIndex"] = startRowIndex;
			ViewState["_maximumRows"] = maximumRows;

			if (TotalRows > -1)
			{
				TotalRowCountAvailable?.Invoke(this, new PageEventArgs((int)ViewState["_startRowIndex"], (int)ViewState["_maximumRows"], TotalRows));
			}
		}

		protected override void OnDataPropertyChanged()
		{
			if (MaximumRows != -1)
			{
				this.RequiresDataBinding = true;
			}
			else
				base.OnDataPropertyChanged();
		}

		protected override void RenderChildren(HtmlTextWriter writer)
		{
			if (MaximumRows != -1)
			{
				foreach (RepeaterItem item in this.Items)
				{
					if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
					{
						item.Visible = false;

						if (item.ItemIndex >= (int)ViewState["_startRowIndex"] &&
						    item.ItemIndex <= ((int)ViewState["_startRowIndex"] +
						                       (int)ViewState["_maximumRows"]))
						{
							item.Visible = true;
						}
					}
					else
					{
						item.Visible = true;
					}
				}
			}

			base.RenderChildren(writer);
		}

		public override void DataBind()
		{
			base.DataBind();

			if (MaximumRows != -1)
			{
				ViewState["_totalRows"] = GetData().Cast<object>().Count();

				if (PersistentDataSource)
					ViewState["DataSource"] = DataSource;

				SetPageProperties(StartRowIndex, MaximumRows, true);
			}
		}

		public event EventHandler<PageEventArgs> TotalRowCountAvailable;
	}
}