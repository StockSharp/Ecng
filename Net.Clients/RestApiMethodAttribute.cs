namespace Ecng.Net;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class RestApiMethodAttribute : Attribute
{
    public RestApiMethodAttribute(string name)
    {
		Name = name.ThrowIfEmpty(nameof(name));
	}

    public string Name { get; }
}