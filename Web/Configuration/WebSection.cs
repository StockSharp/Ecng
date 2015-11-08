namespace Ecng.Web.Configuration
{
    #region Using Directives

    using System.Configuration;

    #endregion

	public class WebSection : ConfigurationSection
	{
		#region UrlRewritingRules

		[ConfigurationProperty("urlRewritingRules", IsRequired = true)]
		public ProviderSettingsCollection UrlRewritingRules => (ProviderSettingsCollection)base["urlRewritingRules"];

		#endregion
	}
}
