namespace Ecng.Serialization;

[AttributeUsage(ReflectionHelper.Types)]
public class EntityAttribute : Attribute
{
	public string Name { get; set; }
	public bool NoCache { get; set; }
}