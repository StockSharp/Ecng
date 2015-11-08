namespace Ecng.Web
{
	using System;
	using System.Web;

	using Ecng.Common;
	using Ecng.Reflection;

	[AttributeUsage(ReflectionHelper.Types)]
	public class VisitPageAttribute : Attribute
	{
		public VisitPageAttribute(byte id)
		{
			Id = id;
		}

		public byte Id { get; }

		public static byte? GetId(IHttpHandler page)
		{
			if (page == null)
				throw new ArgumentNullException(nameof(page));

			var attr = page.GetType().GetAttribute<VisitPageAttribute>();
			if (attr == null)
				return null;
			else
				return attr.Id;
		}

		public static byte GetId(Type pageType)
		{
			if (pageType == null)
				throw new ArgumentNullException(nameof(pageType));

			var attr = pageType.GetAttribute<VisitPageAttribute>();
			if (attr == null)
				throw new ArgumentException("pageType");
			else
				return attr.Id;
		}
	}
}