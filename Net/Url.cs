namespace Ecng.Net
{
	using System;
#if SILVERLIGHT
	using System.Windows;
#endif

	using Ecng.Common;

	public class Url : Uri, ICloneable<Url>
	{
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