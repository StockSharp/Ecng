namespace Ecng.Serialization
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.ServiceModel.Syndication;
	using System.Xml;

	using Ecng.Common;
	using Ecng.Reflection;

	public class XmlReaderEx : XmlTextReader
	{
		private bool _readingDate;
		private static readonly MethodInfo _dateFromString;
		//const string CustomUtcDateTimeFormat = "ddd MMM dd HH:mm:ss Z yyyy"; // Wed Oct 07 08:00:07 GMT 2009

		static XmlReaderEx()
		{
			_dateFromString = typeof(Rss20FeedFormatter).GetMember<MethodInfo>("DateFromString");
		}

		public XmlReaderEx(Stream s) : base(s) { }

		public XmlReaderEx(string inputUri) : base(inputUri) { }

		public string CustomDateFormat { get; set; }

		public override void ReadStartElement()
		{
			if (NamespaceURI.CompareIgnoreCase(string.Empty)
				&& (LocalName.CompareIgnoreCase("lastBuildDate") || LocalName.CompareIgnoreCase("pubDate")))
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