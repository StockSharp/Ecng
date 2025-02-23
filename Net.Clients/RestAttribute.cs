namespace Ecng.Net;

/// <summary>
/// Specifies that the associated element is intended to be used in REST API operations.
/// This attribute can be applied to methods, parameters, or return values.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
public class RestAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the name associated with the REST entity.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the REST element is required.
	/// </summary>
	public bool IsRequired { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the REST element should be ignored.
	/// </summary>
	public bool Ignore { get; set; }
}