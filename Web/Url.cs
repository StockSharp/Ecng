namespace Ecng.Web
{
	using System;
#if SILVERLIGHT
	using System.Windows;
#else
	using System.Web;
#endif

	using Ecng.Common;

	public class Url : Uri, ICloneable<Url>
	{
#if !SILVERLIGHT
		private static Uri GetUri(Type pageType)
		{
			var node = HttpHelper.DefaultMapper.GetNode(pageType);

			if (node == null)
				throw new ArgumentException("Node for type {0} doesn't exist.".Put(pageType));

			return AspNetPath.ToFullAbsolute(node.Url);
		}

		public Url(Type pageType)
			: this(GetUri(pageType))
		{
		}
#endif

		public Url(Uri url)
			: this(url.ToString())
		{
		}

		public Url(string url)
			: base(url)
		{
		}

		public Url(string basePart, string relativePart)
			: this(new Uri(basePart), relativePart)
		{
		}

		public Url(Uri basePart, string relativePart)
			: base(basePart, relativePart)
		{
		}

		public bool KeepDefaultPage { get; set; }
		public bool PreventEncodeUrl { get; set; }

		private QueryString _queryString;

		public QueryString QueryString => _queryString ?? (_queryString = new QueryString(this));

		public static Url Current => new Url(HttpContext.Current.Request.Url);

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Url Clone()
		{
			return new Url(this);
		}
	}
}