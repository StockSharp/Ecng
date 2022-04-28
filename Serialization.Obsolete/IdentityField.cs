namespace Ecng.Serialization
{
	using System.Reflection;

	public class IdentityField : Field
	{
		public IdentityField(Schema schema, MemberInfo member)
			: base(schema, member)
		{
			IsIndex = true;
			IsUnique = true;
		}
	}
}