namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System.Web.UI;
	using System.Web.UI.WebControls;
	using Unit = System.Web.UI.WebControls.Unit;

	using Ecng.Common;

	#endregion

	public class FormattedText : Panel
	{
		#region Private Fields

		private const string _script = @"
			<script type='text/javascript'>
				document.getElementById('{0}').innerHTML = new Ecng.Wiki.Formatter().format(document.getElementById('{0}').innerHTML, 'sections');
            </script>";

		#endregion

		#region FormattedText.ctor()

		public FormattedText()
		{
			base.Width = new Unit(100, UnitType.Percentage);
			base.Height = new Unit(100, UnitType.Percentage);
		}

		#endregion

		#region Control Members

		protected override void Render(HtmlTextWriter writer)
		{
			base.Render(writer);

			if (!base.Page.ClientScript.IsStartupScriptRegistered(typeof(FormattedText), base.ClientID))
				base.Page.ClientScript.RegisterStartupScript(typeof(FormattedText), base.ClientID, _script.Put(base.ClientID));
		}

		#endregion
	}
}