namespace Ecng.Net;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
public class RestAttribute : Attribute
{
	public string Name { get; set; }
	public bool IsRequired { get; set; }
	public bool Ignore { get; set; }
}