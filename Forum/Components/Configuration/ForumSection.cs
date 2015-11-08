namespace Ecng.Forum.Components.Configuration
{
	#region Using Directives

	using System.Configuration;
	using System.Web.Configuration;

	#endregion

	class ForumSection : ConfigurationSection
	{
		//#region RootObject

		//[ConfigurationProperty("rootObject", IsRequired = true)]
		//public string RootObject
		//{
		//    get { return (string)base["rootObject"]; }
		//    set { base["rootObject"] = value; }
		//}

		//#endregion

		//#region RootType

		//[ConfigurationProperty("rootType", IsRequired = true)]
		//public string RootType
		//{
		//    get { return (string)base["rootType"]; }
		//    set { base["rootType"] = value; }
		//}

		//#endregion

		#region PageTypes

		[ConfigurationProperty("pageTypes", IsRequired = false)]
		public ProviderSettingsCollection PageTypes => (ProviderSettingsCollection)base["pageTypes"];

		#endregion

		#region EntityAssemblies

		[ConfigurationProperty("entityAssemblies", IsRequired = false)]
		public AssemblyCollection EntityAssemblies => (AssemblyCollection)base["entityAssemblies"];

		#endregion
	}
}