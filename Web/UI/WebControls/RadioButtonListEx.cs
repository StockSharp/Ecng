namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.Web.UI.WebControls;
	using System.ComponentModel;

	using Ecng.Common;

	public class RadioButtonListEx : RadioButtonList
	{
		[Category("Behavior")]
		[DefaultValue("")]
		public string OnClientClick
		{
			get => (string)ViewState["OnClientClick"] ?? string.Empty;
			set => ViewState["OnClientClick"] = value;
		}

		protected override void OnPreRender(EventArgs e)
		{
			base.OnPreRender(e);

			var click = OnClientClick;

			if (!click.IsEmpty())
			{
				foreach (ListItem item in Items)
					item.Attributes.Add("onclick", click);
			}
		}
	}
}