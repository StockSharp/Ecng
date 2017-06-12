namespace Ecng.Web
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Web;

	using Ecng.Collections;

	public class SiteMapMapper
	{
		private readonly Dictionary<Type, SiteMapNode> _pageNodes = new Dictionary<Type, SiteMapNode>();

		public SiteMapMapper()
			: this("web.sitemap")
		{
		}

		public SiteMapMapper(string fileName)
			: this(CreateXmlProvider(fileName))
		{
		}

		public SiteMapMapper(SiteMapProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			Provider = provider;

			ObtainNodeInfo(provider.RootNode);
		}

		public SiteMapProvider Provider { get; private set; }

		public SiteMapNode GetNode(Type pageType)
		{
			if (pageType == null)
				throw new ArgumentNullException(nameof(pageType));

			return _pageNodes.TryGetValue(pageType);
		}

		private void ObtainNodeInfo(SiteMapNode node)
		{
			if (node == null)
				throw new ArgumentNullException(nameof(node));

			foreach (SiteMapNode childNode in node.ChildNodes)
				ObtainNodeInfo(childNode);

			var pageType = node.ToPageType();

			if (_pageNodes.ContainsKey(pageType))
				throw new ArgumentException($"Type {pageType} already exist.", nameof(node));

			_pageNodes.Add(pageType, node);
		}

		private static SiteMapProvider CreateXmlProvider(string fileName)
		{
			var provider = new XmlSiteMapProvider();

			var config = new NameValueCollection { { "siteMapFile", fileName } };
			provider.Initialize(SiteMap.Provider.Name, config);

			return provider;
		}
	}
}