namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.Globalization;
	using System.Reflection;
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using Ecng.Common;
	using Ecng.Reflection;

	public class DataPagerEx : DataPager
	{
		private class NumericPagerFieldEx : NumericPagerField
		{
			private int _maximumRows;
			private int _startRowIndex;
			private int _totalRowCount;

			public override void CreateDataPagers(DataPagerFieldItem container, int startRowIndex, int maximumRows, int totalRowCount, int fieldIndex)
			{
				_startRowIndex = startRowIndex;
				_maximumRows = maximumRows;
				_totalRowCount = totalRowCount;
				//if (string.IsNullOrEmpty(DataPager.QueryStringField))
				//{
				//	this.CreateDataPagersForCommand(container, fieldIndex);
				//}
				//else
				//{
				CreateDataPagersForQueryString(container, fieldIndex);
				//}
			}

			private void CreateDataPagersForQueryString(DataPagerFieldItem container, int fieldIndex)
			{
				var currPage = _startRowIndex / _maximumRows;
				var lastPage = (_totalRowCount - 1) / _maximumRows;
				QueryStringHandled = true;
				//var num2 = _startRowIndex / (ButtonCount * _maximumRows) * ButtonCount;
				//var rowCount = (num2 + ButtonCount) * _maximumRows - 1;

				const int offset = 2;

				if (currPage > 0)
				{
					if (currPage > offset)
					{
						container.Controls.Add(CreateNextPrevLink("<<", 1, NextPageImageUrl));
						AddNonBreakingSpace(container);
					}

					container.Controls.Add(CreateNextPrevLink("<", currPage, NextPageImageUrl));
					AddNonBreakingSpace(container);
				}

				for (var i = (currPage - offset).Max(0); i < currPage; i++)
				{
					container.Controls.Add(CreateNumericLink(i + 1));
					AddNonBreakingSpace(container);
				}

				var child = new Label { Text = (currPage + 1).ToString(CultureInfo.InvariantCulture) };

				if (!CurrentPageLabelCssClass.IsEmpty())
					child.CssClass = CurrentPageLabelCssClass;

				container.Controls.Add(child);

				AddNonBreakingSpace(container);

				for (var i = currPage; i < (currPage + offset).Min(lastPage); i++)
				{
					container.Controls.Add(CreateNumericLink(i + 2));
					AddNonBreakingSpace(container);
				}

				//if (rowCount < _totalRowCount - 1)
				//{
				//	AddNonBreakingSpace(container);
				//	container.Controls.Add(CreateNextPrevLink(NextPageText, num2 + ButtonCount, NextPageImageUrl));
				//	AddNonBreakingSpace(container);
				//}

				if (currPage < lastPage)
				{
					AddNonBreakingSpace(container);
					container.Controls.Add(CreateNextPrevLink(">", currPage + 2, NextPageImageUrl));

					if (currPage < lastPage - offset)
					{
						AddNonBreakingSpace(container);
						container.Controls.Add(CreateNextPrevLink(">>", lastPage + 1, NextPageImageUrl));
					}
				}
			}

			private void AddNonBreakingSpace(DataPagerFieldItem container)
			{
				if (RenderNonBreakingSpacesBetweenControls)
				{
					container.Controls.Add(new LiteralControl("&nbsp;"));
				}
			}

			private HyperLink CreateNextPrevLink(string buttonText, int pageNumber, string imageUrl)
			{
				//var pageNumber = pageIndex + 1;

				var link = new HyperLink
				{
					Text = buttonText,
					NavigateUrl = GetQueryStringNavigateUrl(pageNumber),
					ImageUrl = imageUrl
				};

				if (!NextPreviousButtonCssClass.IsEmpty())
					link.CssClass = NextPreviousButtonCssClass;

				return link;
			}

			private HyperLink CreateNumericLink(int pageNumber)
			{
				//var pageNumber = pageIndex + 1;

				var link = new HyperLink
				{
					Text = pageNumber.ToString(CultureInfo.InvariantCulture),
					NavigateUrl = GetQueryStringNavigateUrl(pageNumber)
				};

				if (!NumericButtonCssClass.IsEmpty())
					link.CssClass = NumericButtonCssClass;

				return link;
			}

			//public override void HandleEvent(CommandEventArgs e)
			//{
			//	if (!DataPager.QueryStringField.IsEmpty())
			//		return;

			//	var startRowIndex = -1;
			//	//int num1 = this._startRowIndex / base.DataPager.PageSize;
			//	var num2 = _startRowIndex / (ButtonCount * DataPager.PageSize) * ButtonCount;
			//	var num3 = (num2 + ButtonCount) * DataPager.PageSize - 1;
			//	if (string.Equals(e.CommandName, "Prev"))
			//	{
			//		startRowIndex = (num2 - 1) * DataPager.PageSize;
			//		if (startRowIndex < 0)
			//		{
			//			startRowIndex = 0;
			//		}
			//	}
			//	else if (string.Equals(e.CommandName, "Next"))
			//	{
			//		startRowIndex = num3 + 1;
			//		if (startRowIndex > _totalRowCount)
			//		{
			//			startRowIndex = _totalRowCount - DataPager.PageSize;
			//		}
			//	}
			//	else
			//	{
			//		startRowIndex = Convert.ToInt32(e.CommandName, CultureInfo.InvariantCulture) * DataPager.PageSize;
			//	}
			//	if (startRowIndex != -1)
			//	{
			//		DataPager.SetPageProperties(startRowIndex, DataPager.PageSize, true);
			//	}
			//}
		}

		public DataPagerEx()
		{
			QueryStringField = PageField;
			base.Fields.Add(new NumericPagerFieldEx
			{
				NextPreviousButtonCssClass = "data_pager_nextprev",
				NumericButtonCssClass = "data_pager_numeric",
			});
		}

		public const string PageField = "page";

		public int MaxRowCount { get; set; }

		protected override void OnInit(EventArgs e)
		{
			var ptr = typeof(Control).GetMember<MethodInfo>("OnInit").MethodHandle.GetFunctionPointer();
			var onInit = (Action<EventArgs>)Activator.CreateInstance(typeof(Action<EventArgs>), this, ptr);
			onInit.Invoke(e);
		
			if (!DesignMode)
			{
				var pageableItemControl = FindPageableItemContainer();

				if (pageableItemControl != null)
				{
					this.SetValue("_pageableItemContainer", pageableItemControl);

					ConnectToEvents(pageableItemControl);

					var startRowIndex = WebHelper.Current.QueryString.TryGetValue<int?>(PageField) ?? 0;

					if (startRowIndex == 1)
						startRowIndex = 0;
					else if (startRowIndex > 1)
						startRowIndex = (startRowIndex - 1) * MaximumRows;
					else if (startRowIndex < 0)
						startRowIndex = 0;

					this.SetValue("_startRowIndex", startRowIndex);

					pageableItemControl.SetPageProperties(StartRowIndex, MaximumRows, false);
					this.SetValue("_setPageProperties", true);
				}

				Page?.RegisterRequiresControlState(this);
			}

			this.SetValue("_initialized", true);
		}

		protected override void Render(HtmlTextWriter writer)
		{
			var pageCount = ((double)TotalRowCount / PageSize).Ceiling();

			if (pageCount > 1)
				base.Render(writer);
		}

		protected override void OnTotalRowCountAvailable(object sender, PageEventArgs e)
		{
			var maxRowCount = MaxRowCount != 0 ? MaxRowCount : e.TotalRowCount;
			base.OnTotalRowCountAvailable(sender, new PageEventArgs(e.StartRowIndex, e.MaximumRows, maxRowCount));
		}
	}
}