namespace Ecng.Xaml
{
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Data;
	using System.Xml;

	using Ecng.Common;
	using Ecng.Localization;

	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow
	{
		/// <summary>
		/// Default constructor is protected so callers must use one with a parent.
		/// </summary>
		protected AboutWindow()
		{
			InitializeComponent();

			reserved.Content = ((string)reserved.Content).Translate();
			info.Content = ((string)info.Content).Translate();
		}

		/// <summary>
		/// Constructor that takes a parent for this AboutWindow dialog.
		/// </summary>
		/// <param name="parent">Parent window for this dialog.</param>
		public AboutWindow(Window parent)
			: this()
		{
			Owner = parent;
		}

		/// <summary>
		/// Handles click navigation on the hyperlink in the About dialog.
		/// </summary>
		/// <param name="sender">Object the sent the event.</param>
		/// <param name="e">Navigation events arguments.</param>
		private void hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			if (e.Uri == null || e.Uri.OriginalString.IsEmpty())
				return;

			var uri = e.Uri.AbsoluteUri;
			Process.Start(new ProcessStartInfo(uri));
			e.Handled = true;
		}

		#region AboutData Provider
		#region Member data
		private XmlDocument xmlDoc;

		private const string propertyNameTitle = "Title";
		private const string propertyNameDescription = "Description";
		private const string propertyNameProduct = "Product";
		private const string propertyNameCopyright = "Copyright";
		private const string propertyNameCompany = "Company";
		private const string xPathRoot = "ApplicationInfo/";
		private const string xPathTitle = xPathRoot + propertyNameTitle;
		private const string xPathVersion = xPathRoot + "Version";
		private const string xPathDescription = xPathRoot + propertyNameDescription;
		private const string xPathProduct = xPathRoot + propertyNameProduct;
		private const string xPathCopyright = xPathRoot + propertyNameCopyright;
		private const string xPathCompany = xPathRoot + propertyNameCompany;
		private const string xPathLink = xPathRoot + "Link";
		private const string xPathLinkUri = xPathRoot + "Link/@Uri";
		#endregion

		#region Properties
		/// <summary>
		/// Gets the title property, which is display in the About dialogs window title.
		/// </summary>
		public string ProductTitle
		{
			get
			{
				var result = CalculatePropertyValue<AssemblyTitleAttribute>(propertyNameTitle, xPathTitle);
				
				if (result.IsEmpty())
				{
					// otherwise, just get the name of the assembly itself.
					result = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().CodeBase);
				}

				return "About".Translate() + " " + result;
			}
		}

		/// <summary>
		/// Gets the application's version information to show.
		/// </summary>
		public string Version
		{
			get
			{
				// first, try to get the version string from the assembly.
				var version = Assembly.GetEntryAssembly().GetName().Version;
				var result = version != null ? version.ToString() : GetLogicalResourceString(xPathVersion);
				return result;
			}
		}

		/// <summary>
		/// Gets the description about the application.
		/// </summary>
		public string Description => CalculatePropertyValue<AssemblyDescriptionAttribute>(propertyNameDescription, xPathDescription);

		/// <summary>
		///  Gets the product's full name.
		/// </summary>
		public string Product => CalculatePropertyValue<AssemblyProductAttribute>(propertyNameProduct, xPathProduct);

		/// <summary>
		/// Gets the copyright information for the product.
		/// </summary>
		public string Copyright => CalculatePropertyValue<AssemblyCopyrightAttribute>(propertyNameCopyright, xPathCopyright);

		/// <summary>
		/// Gets the product's company name.
		/// </summary>
		public string Company => CalculatePropertyValue<AssemblyCompanyAttribute>(propertyNameCompany, xPathCompany);

		/// <summary>
		/// Gets the link text to display in the About dialog.
		/// </summary>
		public string LinkText => GetLogicalResourceString(xPathLink);

		/// <summary>
		/// Gets the link uri that is the navigation target of the link.
		/// </summary>
		public string LinkUri => GetLogicalResourceString(xPathLinkUri);

		#endregion

		#region Resource location methods
		/// <summary>
		/// Gets the specified property value either from a specific attribute, or from a resource dictionary.
		/// </summary>
		/// <typeparam name="T">Attribute type that we're trying to retrieve.</typeparam>
		/// <param name="propertyName">Property name to use on the attribute.</param>
		/// <param name="xpathQuery">XPath to the element in the XML data resource.</param>
		/// <returns>The resulting string to use for a property.
		/// Returns null if no data could be retrieved.</returns>
		private string CalculatePropertyValue<T>(string propertyName, string xpathQuery)
		{
			var result = string.Empty;
			// first, try to get the property value from an attribute.
			var attributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(T), false);
			if (attributes.Length > 0)
			{
				var attrib = (T)attributes[0];
				var property = attrib.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
				if (property != null)
				{
					result = property.GetValue(attributes[0], null) as string;
				}
			}

			// if the attribute wasn't found or it did not have a value, then look in an xml resource.
			if (result == string.Empty)
			{
				// if that fails, try to get it from a resource.
				result = GetLogicalResourceString(xpathQuery);
			}
			return result;
		}

		/// <summary>
		/// Gets the XmlDataProvider's document from the resource dictionary.
		/// </summary>
		protected virtual XmlDocument ResourceXmlDocument
		{
			get
			{
				if (xmlDoc != null)
					return xmlDoc;

				// if we haven't already found the resource XmlDocument, then try to find it.
				var provider = TryFindResource("aboutProvider") as XmlDataProvider;
				if (provider != null)
				{
					// save away the XmlDocument, so we don't have to get it multiple times.
					xmlDoc = provider.Document;
				}

				return xmlDoc;
			}
		}

		/// <summary>
		/// Gets the specified data element from the XmlDataProvider in the resource dictionary.
		/// </summary>
		/// <param name="xpathQuery">An XPath query to the XML element to retrieve.</param>
		/// <returns>The resulting string value for the specified XML element. 
		/// Returns empty string if resource element couldn't be found.</returns>
		protected virtual string GetLogicalResourceString(string xpathQuery)
		{
			var result = string.Empty;
			// get the About xml information from the resources.
			var doc = ResourceXmlDocument;
			if (doc == null)
				return result;
			// if we found the XmlDocument, then look for the specified data. 
			var node = doc.SelectSingleNode(xpathQuery);
			if (node == null)
				return result;
			if (node is XmlAttribute)
			{
				// only an XmlAttribute has a Value set.
				result = node.Value;
			}
			else
			{
				// otherwise, need to just return the inner text.
				result = node.InnerText;
			}
			return result;
		}
		#endregion
		#endregion
	}
}
