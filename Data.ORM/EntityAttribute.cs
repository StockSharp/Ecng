namespace Ecng.Serialization;

/// <summary>
/// Attribute that maps a class to a database entity (table).
/// </summary>
[AttributeUsage(ReflectionHelper.Types)]
public class EntityAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the database table name.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets whether caching is disabled for this entity.
	/// </summary>
	public bool NoCache { get; set; }
}