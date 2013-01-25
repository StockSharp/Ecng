namespace Ecng.Data
{
	using System;
	using System.Data;

	using Ecng.Common;
	using Ecng.Serialization;

	public class SqlServerRenderer : SqlRenderer
	{
		#region SqlServerRenderer.ctor()

		public SqlServerRenderer()
			: base(typeof(SqlServerRenderer).Name)
		{
			AddTypeName(DbType.AnsiString, size =>
				size.Min != size.Max
					? "varchar({0})".Put(GetStringSize(size.Max))
					: "char({0})".Put(GetStringSize(size.Max)));

			AddTypeName(DbType.AnsiStringFixedLength, size => "char({0})".Put(GetStringSize(size.Max)));
			AddTypeName(DbType.String, size =>
				size.Min != size.Max
					? "nvarchar({0})".Put(GetStringSize(size.Max))
					: "nchar({0})".Put(GetStringSize(size.Max)));

			AddTypeName(DbType.StringFixedLength, size => "nchar({0})".Put(GetStringSize(size.Max)));

			AddTypeName(DbType.Binary, size =>
				size.Min != size.Max
					? "varbinary({0})".Put(GetStringSize(size.Max))
					: "binary({0})".Put(GetStringSize(size.Max)));

			AddTypeName(DbType.Boolean, "bit");

			AddTypeName(DbType.Byte, "tinyint");
			AddTypeName(DbType.SByte, "tinyint");

			AddTypeName(DbType.Currency, "money");
			AddTypeName(DbType.Decimal, "smallmoney");

			AddTypeName(DbType.Date, "datetime");
			AddTypeName(DbType.DateTime, "datetime");
			AddTypeName(DbType.Time, "datetime");

			AddTypeName(DbType.Single, "float");
			AddTypeName(DbType.Double, "real");
			
			AddTypeName(DbType.Int16, "smallint");
			AddTypeName(DbType.Int32, "int");
			AddTypeName(DbType.Int64, "bigint");
			AddTypeName(DbType.UInt16, "smallint");
			AddTypeName(DbType.UInt32, "int");
			AddTypeName(DbType.UInt64, "bigint");

			AddTypeName(DbType.VarNumeric, size => "numeric({0})".Put(GetStringSize(size.Max)));

			AddTypeName(DbType.Guid, "unique identifier");
			AddTypeName(DbType.Object, "sql_variant");
			AddTypeName(DbType.Xml, "xml");
		}

		#endregion

		#region SqlRenderer Members

		public override string GetIdentitySelect(Schema schema)
		{
			return "scope_identity() as " + schema.Identity.Name;
		}

		protected override string ParameterPrefix
		{
			get { return "@"; }
		}

		protected override string[] ReservedWords
		{
			get
			{
				return Properties.Resources.SqlServerReservedWords.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			}
		}

		#endregion

		#region GetStringSize

		private static string GetStringSize(int size)
		{
			//if (size <= 0)
			//	throw new ArgumentOutOfRangeException("size");

			if (size == int.MaxValue)
				return "max";
			else if (size > 0)
				return size.ToString();
			else
				throw new ArgumentOutOfRangeException("size");
		}

		#endregion
	}
}