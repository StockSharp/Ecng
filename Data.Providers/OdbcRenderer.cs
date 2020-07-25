namespace Ecng.Data.Providers
{
	using Ecng.Common;
	using Ecng.Serialization;

	public class OdbcRenderer : SqlRenderer
	{
		public OdbcRenderer()
			: base(typeof(OdbcRenderer).Name)
		{
		}

		protected override string ParameterPrefix => "[?";

		protected override string ParameterSuffix => "]";

		public override string GetIdentitySelect(Schema schema)
		{
			return "@@identity as " + schema.Identity.Name;
		}

		protected override string[] ReservedWords => ArrayHelper.Empty<string>();
	}
}
