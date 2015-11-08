namespace Ecng.Web.UrlRewriting
{
	#region Using Directives

	using System.Collections.Specialized;
	using System.Text.RegularExpressions;

	using Ecng.ComponentModel;

	#endregion

	public class RegexUrlRewritingRule : UrlRewritingRule
	{
		public string SourceUrl { get; set; }
		public string DestinationUrl { get; set; }

		private Regex _regex;

		private Regex Regex => _regex ?? (_regex = new Regex(SourceUrl, RegexOptions.IgnoreCase | RegexOptions.Compiled));

		public override void Initialize(string name, NameValueCollection config)
		{
			this.Initialize(config);
			base.Initialize(name, config);
		}

		protected internal override bool IsCompatible(string originalPath)
		{
			return Regex.IsMatch(originalPath);
		}

		protected internal override string TransformPath(string originalPath)
		{
			return Regex.Replace(originalPath, DestinationUrl);
		}
	}
}