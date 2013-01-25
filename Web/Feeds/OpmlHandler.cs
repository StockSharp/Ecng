namespace Ecng.Web.Feeds
{
	using System.Collections.Generic;
	using System.Xml;
	using System.Xml.Serialization;

	using RssToolkit.Opml;

	public abstract class OpmlHandler : XmlHandler
	{
		#region XmlHandler Members

        protected override void OnProcessRequest(XmlWriter writer)
		{
			var document = new OpmlDocument { Head = new OpmlHead(), Body = new OpmlBody(), Version = "1.0" };
        	PopulateHeader(document.Head);
			PopulateItems(document.Body.Outlines);

            new XmlSerializer(typeof(OpmlDocument)).Serialize(writer, document);
		}

		#endregion

		protected abstract void PopulateHeader(OpmlHead head);
		protected abstract void PopulateItems(List<OpmlOutline> outlines);
	}
}