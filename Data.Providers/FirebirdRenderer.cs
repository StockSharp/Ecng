namespace Ecng.Data.Providers
{
	using System.Data;

	using Ecng.Common;
	using Ecng.Serialization;

	public class FirebirdRenderer : SqlRenderer
	{
		#region FirebirdRenderer.ctor()

		public FirebirdRenderer()
			: base(typeof(FirebirdRenderer).Name)
		{
			AddTypeName(DbType.AnsiString, size => "varchar({0})".Put(size.Max));
			AddTypeName(DbType.AnsiStringFixedLength, size => "char({0})".Put(size.Max));
			AddTypeName(DbType.String, size => "varchar({0}) character set unicode_fss".Put(size.Max));
			AddTypeName(DbType.StringFixedLength, size => "char({0}) character set unicode_fss".Put(size.Max));

			AddTypeName(DbType.Binary, size => "binary({0})".Put(size.Max));

			AddTypeName(DbType.Boolean, "char(1)");

			AddTypeName(DbType.Byte, "smallint");
			AddTypeName(DbType.SByte, "smallint");

			AddTypeName(DbType.Currency, "decimal(10, 4)");
			AddTypeName(DbType.Decimal, "decimal");
			
			AddTypeName(DbType.Date, "timestamp");
			AddTypeName(DbType.DateTime, "timestamp");
			AddTypeName(DbType.DateTimeOffset, "timestamp");
			AddTypeName(DbType.Time, "timestamp");

			AddTypeName(DbType.Single, "float");
			AddTypeName(DbType.Double, "double");
			
			AddTypeName(DbType.Int16, "smallint");
			AddTypeName(DbType.Int32, "integer");
			AddTypeName(DbType.Int64, "int64");
			AddTypeName(DbType.UInt16, "smallint");
			AddTypeName(DbType.UInt32, "integer");
			AddTypeName(DbType.UInt64, "int64");

			AddTypeName(DbType.VarNumeric, size => "numeric({0})".Put(size.Max));

			AddTypeName(DbType.Guid, "char(38)");
			AddTypeName(DbType.Object, "blob");
			//AddTypeName(DbType.Xml, "xml");
		}

		#endregion

		#region SqlRenderer Members

		protected override string ParameterPrefix
		{
			get { return "@"; }
		}

		public override string GetIdentitySelect(Schema schema)
		{
			return "gen_id(gen_{0}_{1}, 0) as {1} from rdb$database".Put(schema.Name, schema.Identity.Name);
		}

		protected override string[] ReservedWords
		{
			get { return ArrayHelper<string>.EmptyArray; }
		}

		#endregion
	}
}