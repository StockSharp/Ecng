namespace Ecng.Data.Providers
{
	using System;

	using Ecng.Serialization;

	public class SQLiteRenderer : SqlRenderer
	{
		public SQLiteRenderer()
			: base(typeof(SQLiteRenderer).Name)
		{
		}

		public override string GetIdentitySelect(Schema schema)
		{
			return "last_insert_rowid() as " + schema.Identity.Name;
		}

		protected override string ParameterPrefix
		{
			get { return "@"; }
		}

		protected override string[] ReservedWords
		{
			get { return Properties.Resources.SQLiteReservedWords.Split(new[] { Environment.NewLine }, StringSplitOptions.None); }
		}
	}
}