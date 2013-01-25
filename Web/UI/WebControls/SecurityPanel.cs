namespace Ecng.Web.UI.WebControls
{
	using System.Web.UI;
	using System.Web.UI.WebControls;

	public abstract class SecurityPanel : Panel
	{
		protected abstract bool CanView();

		protected override void Render(HtmlTextWriter writer)
		{
			if (CanView())
				base.RenderContents(writer);
		}
	}
}