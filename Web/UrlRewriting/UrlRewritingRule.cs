namespace Ecng.Web.UrlRewriting
{
	#region Using Directives

	using System.Configuration.Provider;

	#endregion

	public abstract class UrlRewritingRule : ProviderBase
	{
		protected internal abstract bool IsCompatible(string originalPath);
		protected internal abstract string TransformPath(string originalPath);
	}
}