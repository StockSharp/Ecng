namespace Ecng.Data.Providers
{
	#region Using Directives

	using Ecng.Common;
	using Ecng.Serialization;

	#endregion

	public class PostgreSqlRenderer : SqlRenderer
	{
		#region PostgreSqlRenderer.ctor()

		public PostgreSqlRenderer()
			: base(typeof(PostgreSqlRenderer).Name)
		{
		}

		#endregion

		#region SqlRenderer Members

		protected override string ParameterPrefix
		{
			get { return "?"; }
		}

		public override string GetIdentitySelect(Schema schema)
		{
			return "currval('{0}_{1}_seq') as {1}".Put(schema.Name, schema.Identity.Name);
		}

		protected override string[] ReservedWords
		{
			get { return new string[0]; }
		}

		#endregion
	}
}