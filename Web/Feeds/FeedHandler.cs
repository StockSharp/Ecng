namespace Ecng.Web.Feeds
{
	using System.Collections.Generic;
	using System.ServiceModel.Syndication;
	using System.Xml;

	using Ecng.Reflection;

	public abstract class FeedHandler<TFeed, TFormatter> : XmlHandler
		where TFeed : SyndicationFeed, new()
		where TFormatter : SyndicationFeedFormatter
	{
		protected override void OnProcessRequest(XmlWriter writer)
		{
			var feed = new TFeed();
			PopulateHeader(feed);

			var items = new List<SyndicationItem>();
			PopulateItems(items);

			feed.Items = items;

			var formatter = typeof(TFormatter).CreateInstance<TFormatter>(feed);
			formatter.WriteTo(writer);
		}

		protected abstract void PopulateHeader(SyndicationFeed feed);
		protected abstract void PopulateItems(List<SyndicationItem> items);
	}
}