namespace Ecng.Data.Providers
{
	using System;
	using System.Data;

	using Ecng.Serialization;

	public class SQLiteRenderer : SqlRenderer
	{
		public SQLiteRenderer()
			: base(typeof(SQLiteRenderer).Name)
		{
			AddTypeName(DbType.AnsiString, "text");
			AddTypeName(DbType.AnsiStringFixedLength, "text");
			AddTypeName(DbType.String, "text");
			AddTypeName(DbType.StringFixedLength, "text");

			AddTypeName(DbType.Binary, "blob");

			AddTypeName(DbType.Boolean, "integer");

			AddTypeName(DbType.Byte, "integer");
			AddTypeName(DbType.SByte, "integer");

			AddTypeName(DbType.Currency, "real");
			AddTypeName(DbType.Decimal, "real");

			AddTypeName(DbType.Date, "text");
			AddTypeName(DbType.DateTime, "text");
			AddTypeName(DbType.DateTimeOffset, "text");
			AddTypeName(DbType.Time, "text");

			AddTypeName(DbType.Single, "real");
			AddTypeName(DbType.Double, "real");

			AddTypeName(DbType.Int16, "integer");
			AddTypeName(DbType.Int32, "integer");
			AddTypeName(DbType.Int64, "integer");
			AddTypeName(DbType.UInt16, "integer");
			AddTypeName(DbType.UInt32, "integer");
			AddTypeName(DbType.UInt64, "integer");
		}

		public override string GetIdentitySelect(Schema schema)
		{
			return "last_insert_rowid() as " + schema.Identity.Name;
		}

		protected override string ParameterPrefix => "@";

		protected override string[] ReservedWords => Properties.Resources.SQLiteReservedWords.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
	}
}