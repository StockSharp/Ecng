namespace Ecng.ComponentModel;

using System;

/// <summary>
/// Template editor attribute.
/// </summary>
public class TemplateEditorAttribute : Attribute
{
	/// <summary>
	/// Template key.
	/// </summary>
	public string TemplateKey { get; set; }
}