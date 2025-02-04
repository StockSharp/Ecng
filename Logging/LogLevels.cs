namespace Ecng.Logging;

using System.Runtime.Serialization;

/// <summary>
/// Levels of log messages <see cref="LogMessage"/>.
/// </summary>
[DataContract]
public enum LogLevels
{
	/// <summary>
	/// To use the logging level of the container.
	/// </summary>
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InheritedKey)]
	[EnumMember]
	Inherit,

	/// <summary>
	/// Verbose message, debug message, information, warnings and errors.
	/// </summary>
	[EnumMember]
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VerboseKey)]
	Verbose,

	/// <summary>
	/// Debug message, information, warnings and errors.
	/// </summary>
	[EnumMember]
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DebugKey)]
	Debug,
	
	/// <summary>
	/// Information, warnings and errors.
	/// </summary>
	[EnumMember]
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InfoKey)]
	Info,

	/// <summary>
	/// Warnings and errors.
	/// </summary>
	[EnumMember]
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.WarningsKey)]
	Warning,
	
	/// <summary>
	/// Errors only.
	/// </summary>
	[EnumMember]
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorsKey)]
	Error,

	/// <summary>
	/// Logs off.
	/// </summary>
	[EnumMember]
	//[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OffKey)]
	Off,
}