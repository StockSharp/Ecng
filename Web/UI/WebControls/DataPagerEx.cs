namespace Ecng.Web.UI.WebControls
{
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using Ecng.Common;

	public class DataPagerEx : DataPager
	{
		#region ForumPager.ctor()

		public DataPagerEx()
		{
			QueryStringField = "page";
			base.Fields.Add(new NumericPagerField());
		}

		#endregion

		public int MaxRowCount { get; set; }

		protected override void Render(HtmlTextWriter writer)
		{
			var pageCount = ((double)TotalRowCount / PageSize).Ceiling();

			if (pageCount > 1)
				base.Render(writer);
		}

		protected override void OnTotalRowCountAvailable(object sender, PageEventArgs e)
		{
			var maxRowCount = (MaxRowCount != 0) ? MaxRowCount : e.TotalRowCount;
			base.OnTotalRowCountAvailable(sender, new PageEventArgs(e.StartRowIndex, e.MaximumRows, maxRowCount));
		}
	}
}