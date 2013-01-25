namespace Ecng.Data.Providers
{
	using System.Data;

	using Ecng.Common;
	using Ecng.Serialization;

	public class MySqlRenderer : SqlRenderer
	{
		#region MySqlRenderer.ctor()

		public MySqlRenderer()
			: base(typeof(MySqlRenderer).Name)
		{
			AddTypeName(DbType.AnsiString, size => "varchar({0})".Put(size.Max));
			AddTypeName(DbType.AnsiStringFixedLength, size => "char({0})".Put(size.Max));
			AddTypeName(DbType.String, size => "longtext({0})".Put(size.Max));
			AddTypeName(DbType.StringFixedLength, size => "longtext({0})".Put(size.Max));

			AddTypeName(DbType.Binary, "blob");

			AddTypeName(DbType.Boolean, "boolean");

			AddTypeName(DbType.Byte, "tinyint");
			AddTypeName(DbType.SByte, "tinyint");

			AddTypeName(DbType.Currency, "decimal");
			AddTypeName(DbType.Decimal, "decimal");

			AddTypeName(DbType.Date, "datetime");
			AddTypeName(DbType.DateTime, "datetime");
			AddTypeName(DbType.Time, "datetime");

			AddTypeName(DbType.Single, "float");
			AddTypeName(DbType.Double, "double");

			AddTypeName(DbType.Int16, "smallint");
			AddTypeName(DbType.Int32, "int");
			AddTypeName(DbType.Int64, "bigint");
			AddTypeName(DbType.UInt16, "smallint");
			AddTypeName(DbType.UInt32, "int");
			AddTypeName(DbType.UInt64, "bigint");

			//AddTypeName(DbType.VarNumeric, size => "numeric({0})".Put(size));

			//AddTypeName(DbType.Guid, "char(38)");
			//AddTypeName(DbType.Object, "blob");
			//AddTypeName(DbType.Xml, "xml");
		}

		#endregion

		#region SqlRenderer Members

		protected override string ParameterPrefix
		{
			get { return "?"; }
		}

		public override string GetIdentitySelect(Schema schema)
		{
			return "last_insert_id() as " + schema.Identity.Name;
		}

		protected override string[] ReservedWords
		{
			get { return new string[0]; }
		}

		#endregion
	}
}