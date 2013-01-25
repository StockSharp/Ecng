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
		#region Url.ctor()

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

		#endregion

		#region QueryString

		private QueryString _queryString;

		public QueryString QueryString
		{
			get { return _queryString ?? (_queryString = new QueryString(this)); }
		}

		#endregion

		#region Current

		public static Url Current
		{
			get
			{
#if SILVERLIGHT
				//return new Url(HtmlPage.Document.DocumentUri);
				return new Url(Application.Current.Host.Source);
#else
				return new Url(HttpContext.Current.Request.Url);
#endif
			}
		}

		#endregion

		#region ICloneable Members

		object ICloneable.Clone()
		{
			return Clone();
		}

		#endregion

		#region ICloneable<Url> Members

		public Url Clone()
		{
			return new Url(this);
		}

		#endregion
	}
}