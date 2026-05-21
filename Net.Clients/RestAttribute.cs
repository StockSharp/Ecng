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
	/// When <see langword="true"/>, the parameter is excluded from the
	/// request wire shape (<see cref="RestBaseApiClient.GetInfo"/> filters
	/// it out of the params/args validation), but the C# signature still
	/// carries it. Canonical use-case: a parameter that the server resolves
	/// from auth context (JWT claim) rather than from the request body or
	/// URL — e.g. <c>tenantId</c> / <c>userId</c> on a server-shaped client
	/// interface.
	/// </summary>
	public bool Ignore { get; set; }
}