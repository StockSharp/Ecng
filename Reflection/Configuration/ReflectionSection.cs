namespace Ecng.Reflection.Configuration
{
	#region Using Directives

	using System.Configuration;

	#endregion

	public class ReflectionSection : ConfigurationSection
	{
		#region NeedCache

		/// <summary>
		/// Gets or sets a value indicating whether [need cache].
		/// </summary>
		/// <value><c>true</c> if [need cache]; otherwise, <c>false</c>.</value>
		[ConfigurationProperty("needCache")]
		public bool NeedCache
		{
			get => (bool)base["needCache"];
			set => base["needCache"] = value;
		}

		#endregion

		#region AssemblyCachePath

		[ConfigurationProperty("assemblyCachePath")]
		public string AssemblyCachePath
		{
			get => (string)base["assemblyCachePath"];
			set => base["assemblyCachePath"] = value;
		}

		#endregion

		#region CompiledTypeLimit

		[ConfigurationProperty("compiledTypeLimit")]
		public int CompiledTypeLimit
		{
			get => (int)base["compiledTypeLimit"];
			set => base["compiledTypeLimit"] = value;
		}

		#endregion
	}
}