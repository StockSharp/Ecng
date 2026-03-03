namespace Ecng.Data.Sql;

/// <summary>
/// Marks a property as mapped to an external SQL column or table.
/// </summary>
public class SqlExternalAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the external SQL name.
	/// </summary>
	public string Name { get; set; }
}