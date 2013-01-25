namespace Ecng.Forum.Components
{
	using Ecng.Web.UI.WebControls;

	public class ForumPager : DataPagerEx
	{
		public ForumPager()
		{
			PageSize = ForumHelper.PageSize;

			//NextPreviousPagerField firstField = new NextPreviousPagerField();
			//firstField.ButtonType = ButtonType.Link;
			//firstField.ShowFirstPageButton = true;
			//firstField.ShowNextPageButton = false;
			//firstField.ShowPreviousPageButton = false;
			//firstField.FirstPageText = "<<";
			//Fields.Add(firstField);

			//NextPreviousPagerField lastField = new NextPreviousPagerField();
			//lastField.ButtonType = ButtonType.Link;
			//lastField.ShowLastPageButton = true;
			//lastField.ShowNextPageButton = false;
			//lastField.ShowPreviousPageButton = false;
			//lastField.LastPageText = ">>";
			//Fields.Add(lastField);
		}
	}
}