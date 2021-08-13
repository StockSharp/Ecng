namespace Ecng.Serialization
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
#if !NETCOREAPP && !NETSTANDARD
	using System.ServiceModel.Syndication;
#endif
	using System.Xml;

	using Ecng.Common;
	using Ecng.Reflection;

	public class XmlReaderEx : XmlTextReader
	{
		private bool _readingDate;
#if !NETCOREAPP && !NETSTANDARD
		private static readonly MethodInfo _dateFromString;
		//const string CustomUtcDateTimeFormat = "ddd MMM dd HH:mm:ss Z yyyy"; // Wed Oct 07 08:00:07 GMT 2009
#endif

		static XmlReaderEx()
		{
#if !NETCOREAPP && !NETSTANDARD
			_dateFromString = typeof(Rss20FeedFormatter).GetMember<MethodInfo>("DateFromString");
#else
			throw new PlatformNotSupportedException();
#endif
		}

		public XmlReaderEx(Stream s) : base(s) { }

		public XmlReaderEx(string inputUri) : base(inputUri) { }

		public XmlReaderEx(TextReader reader) : base(reader) { }

		public string CustomDateFormat { get; set; }

		public override void ReadStartElement()
		{
			if (NamespaceURI.EqualsIgnoreCase(string.Empty)
				&& (LocalName.EqualsIgnoreCase("lastBuildDate") || LocalName.EqualsIgnoreCase("pubDate")))
			{
				_readingDate = true;
			}

			base.ReadStartElement();
		}

		public override void ReadEndElement()
		{
			if (_readingDate)
				_readingDate = false;

			base.ReadEndElement();
		}

		public override string ReadString()
		{
			if (_readingDate)
			{
				var dateString = base.ReadString();

				DateTimeOffset dto;

				if (dateString.IsEmpty())
					dto = new DateTimeOffset();
				else
				{
#if !NETCOREAPP && !NETSTANDARD
					try
					{
						dto = _dateFromString.GetValue<object[], DateTimeOffset>(new object[] { dateString, this });
					}
					catch (Exception)
					{
						if (CustomDateFormat.IsEmpty())
							throw;

						dto = dateString.ToDateTimeOffset(CustomDateFormat);
					}
#else
					throw new PlatformNotSupportedException();
#endif
				}

				return dto.ToString(CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern);
			}
			else
			{
				return base.ReadString();
			}
		}
	}
}