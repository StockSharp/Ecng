namespace Ecng.UI
{
	#region Using Directives

	using System.Collections.Generic;
	using System.Windows.Browser;

	using Ecng.Common;

	#endregion

	public static class HtmlHelper
	{
		public static T Get<T>(string id)
		{
			return GetElement(id).GetAttribute("value").To<T>();
		}

		public static void Set<T>(string id, T value)
		{
			GetElement(id).SetAttribute("checked", value.ToString());
		}

		private static HtmlElement GetElement(string id)
		{
			return HtmlPage.Document.GetElementById(id);
		}

		public static IList<HtmlElement> GetElementsByTagName(HtmlElement rootElement, string tagName)
		{
			var elements = new List<HtmlElement>();

			foreach (HtmlElement element in rootElement.Children)
			{
				if (element.TagName == tagName)
					elements.Add(element);
				else
					elements.AddRange(GetElementsByTagName(element, tagName));
			}

			return elements;
		}

		public static HtmlElement CloneNode(HtmlElement element)
		{
			var clone = HtmlPage.Document.CreateElement(element.TagName);

			foreach (HtmlElement child in element.Children)
				clone.AppendChild(CloneNode(child));

			return clone;
		}
	}
}