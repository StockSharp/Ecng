namespace Ecng.Xaml
{
	using System;
	using System.Reflection;
	using System.Windows;
	using System.Windows.Markup;

	public class ResourseDictExtension : MarkupExtension
	{
		public ResourseDictExtension()
		{
			
		}

		public ResourseDictExtension(object assemblyName, object path, object key)
		{
			AssemblyName = assemblyName;
			Path = path;
			Key = key;
		}

		[ConstructorArgument("assemblyName")]
		public object AssemblyName { get; set; }

		[ConstructorArgument("path")]
		public object Path { get; set; }

		[ConstructorArgument("key")]
		public object Key { get; set; }

		protected static ResourceDictionary InnerDict { get; private set; }

		public void InitDict()
		{
			var resName = AssemblyName + "." + ((string)Path).Replace('/', '.');

			using (var s = Assembly.Load((string)AssemblyName).GetManifestResourceStream(resName))
				InnerDict = (ResourceDictionary)XamlReader.Load(s);
		}

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (InnerDict == null)
				InitDict();

			return InnerDict[Key];
		}

		protected void ClearCache()
		{
			InnerDict = null;
		}
	}
}