namespace Ecng.Data;

public class SqlServerRenderer : SqlRenderer
{
	#region SqlServerRenderer.ctor()

	public SqlServerRenderer()
		: base(typeof(SqlServerRenderer).Name)
	{
		AddTypeName(DbType.AnsiString, size =>
			size.Min != size.Max
				? $"varchar({GetStringSize(size.Max)})"
				: $"char({GetStringSize(size.Max)})");

		AddTypeName(DbType.AnsiStringFixedLength, size => $"char({GetStringSize(size.Max)})");
		AddTypeName(DbType.String, size =>
			size.Min != size.Max
				? $"nvarchar({GetStringSize(size.Max)})"
				: $"nchar({GetStringSize(size.Max)})");

		AddTypeName(DbType.StringFixedLength, size => $"nchar({GetStringSize(size.Max)})");

		AddTypeName(DbType.Binary, size =>
			size.Min != size.Max
				? $"varbinary({GetStringSize(size.Max)})"
				: $"binary({GetStringSize(size.Max)})");

		AddTypeName(DbType.Boolean, "bit");

		AddTypeName(DbType.Byte, "tinyint");
		AddTypeName(DbType.SByte, "tinyint");

		AddTypeName(DbType.Currency, "money");
		AddTypeName(DbType.Decimal, "smallmoney");

		AddTypeName(DbType.Date, "datetime");
		AddTypeName(DbType.DateTime, "datetime");
		AddTypeName(DbType.DateTimeOffset, "datetimeoffset");
		AddTypeName(DbType.Time, "datetime");

		AddTypeName(DbType.Single, "float");
		AddTypeName(DbType.Double, "real");

		AddTypeName(DbType.Int16, "smallint");
		AddTypeName(DbType.Int32, "int");
		AddTypeName(DbType.Int64, "bigint");
		AddTypeName(DbType.UInt16, "smallint");
		AddTypeName(DbType.UInt32, "int");
		AddTypeName(DbType.UInt64, "bigint");

		AddTypeName(DbType.VarNumeric, size => $"numeric({GetStringSize(size.Max)})");

		AddTypeName(DbType.Guid, "unique identifier");
		AddTypeName(DbType.Object, "sql_variant");
		AddTypeName(DbType.Xml, "xml");
	}

	#endregion

	#region SqlRenderer Members

	public override string GetIdentitySelect(string idCol) => "scope_identity() as " + idCol;

	protected override string ParameterPrefix => "@";

	protected override string[] ReservedWords => Properties.Resources.SqlServerReservedWords.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

	public override string Skip(string skip) => $"offset {skip} rows";
	public override string Take(string take) => $"fetch next {take} rows only";

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
			throw new ArgumentOutOfRangeException(nameof(size));
	}

	#endregion

	public override string Now() => "getDate()";
	public override string UtcNow() => "getUtcDate()";
	public override string SysNow() => "sysDateTimeOffset()";
	public override string SysUtcNow() => "sysUtcDateTime()";
	public override string NewId() => "newId()";
}
