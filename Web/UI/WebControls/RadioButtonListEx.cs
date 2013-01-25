namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System;
	using System.Web.UI.WebControls;
	using System.ComponentModel;

	using Ecng.Common;

	#endregion

	public class RadioButtonListEx : RadioButtonList
	{
		#region OnClientClick

		[Category("Behavior")]
		[DefaultValue("")]
		[Description("")]
		public string OnClientClick
		{
			get { return (string)base.ViewState["OnClientClick"] ?? string.Empty; }
			set { base.ViewState["OnClientClick"] = value; }
		}

		#endregion

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