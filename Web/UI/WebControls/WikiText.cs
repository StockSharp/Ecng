namespace Ecng.Web.UI.WebControls
{
	using System.Collections.Generic;
	using System.Web.UI;

	using Ecng.Common;
	using Ecng.Collections;

	public class WikiText : Control
	{
		private static readonly Dictionary<string, string> _formattedTexts = new Dictionary<string, string>();

		public string Text { get; set; }

		protected override void Render(HtmlTextWriter writer)
		{
			if (!Text.IsEmpty())
			{
				var text = _formattedTexts.SafeAdd(Text, WikiFormatter.Format);
				writer.Write(text);
			}
		}
	}
}