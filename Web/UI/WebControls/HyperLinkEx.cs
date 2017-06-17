namespace Ecng.Web.UI.WebControls
{
	using System.Web.UI;
	using System.Web.UI.WebControls;

	using Common;

	public class HyperLinkEx : HyperLink
	{
		public string Rel
		{
			get => Attributes["rel"];
			set => Attributes["rel"] = value;
		}

		protected override void RenderContents(HtmlTextWriter writer)
		{
			var imageUrl = ImageUrl;

			if (imageUrl.Length > 0)
			{
				var image = new Image { ImageUrl = ResolveClientUrl(imageUrl) };

				if (!ToolTip.IsEmpty())
					image.ToolTip = ToolTip;

				if (!Text.IsEmpty())
					image.AlternateText = Text;

				image.Width = Width;

				image.RenderControl(writer);
			}
			else
			{
				base.RenderContents(writer);
			}
		}
	}
}