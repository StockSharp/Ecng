namespace Ecng.Web
{
	using System.Web;
	using System.Web.Configuration;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Net;

	public static class AspNetPath
	{
		//#region AspNetPath.cctor()

		//static AspNetPath()
		//{
		//    PortalName = VirtualPathUtility.ToAbsolute("~").Remove(0, 1);
		//}

		//#endregion

		public const string Bin = "Bin";
		public const string LocalResources = "App_LocalResources";
		public const string GlobalResources = "App_GlobalResources";
		public const string Code = "App_Code";
		public const string Data = "App_Data";
		public const string Themes = "App_Themes";
		public const string Browsers = "App_Browsers";
		public const string WebReferences = "App_WebReferences";
		public const string Scripts = "App_Script";

		public const string DefaultPage = "Default.aspx";

		public static string PortalName => WebHelper.CurrentUrl.Host;

		public static string MakeEmail(string account)
		{
			return account + "@" + PortalName;
		}

		public static string GetThemedImagesPath(string fileName)
		{
			var section = ConfigManager.GetSection<PagesSection>();
			return "~/{0}/{1}/Images/{2}".Put(Themes, section.StyleSheetTheme, fileName);
		}

		public static Url ToFullAbsolute(string virtualPath)
		{
			if (virtualPath.StartsWithIgnoreCase("http"))
				return new Url(virtualPath);

			if (HttpContext.Current == null)
			{
				return new Url(virtualPath.Replace("~", "https://stocksharp.com"));
				//return new Url(HostingEnvironment.MapPath(virtualPath));
			}

			return new Url(HttpContext.Current.Request.Url.ToString(), VirtualPathUtility.ToAbsolute(virtualPath));//.ToString();
		}

		public static string ToPhysical(string virtualPath)
		{
			return HttpContext.Current.Request.MapPath(virtualPath);
		}
	}
}