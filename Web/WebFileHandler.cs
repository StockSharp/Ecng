namespace Ecng.Web
{
	using System.Web;

	using Ecng.Common;
	using Ecng.ComponentModel;

	public abstract class WebFileHandler : IHttpHandler
	{
		public abstract IWebFile File { get; }

		bool IHttpHandler.IsReusable => true;

		void IHttpHandler.ProcessRequest(HttpContext context)
		{
			File.Download(Size, Embed, context);
		}

		private static bool Embed => Url.Current.QueryString.TryGetValue("embed", true);

		private static Size<int> Size
		{
			get
			{
				var size = Url.Current.QueryString.TryGetValue<string>("size");

				if (size == null)
					return new Size<int>();

				var parts = size.Split('x');

				if (parts.Length != 2)
					return new Size<int>();

				return new Size<int>(parts[0].To<int>(), parts[1].To<int>());
			}
		}
	}
}