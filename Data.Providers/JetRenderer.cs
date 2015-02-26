namespace Ecng.Data.Providers
{
	using System.Data;

	using Ecng.Common;
	using Ecng.Serialization;

	public class JetRenderer : SqlRenderer
	{
		#region JetRenderer.ctor()

		public JetRenderer()
			: base(typeof(JetRenderer).Name)
		{
			AddTypeName(DbType.AnsiString, size => "longtext({0})".Put(size.Max));
			AddTypeName(DbType.AnsiStringFixedLength, size => "longtext({0})".Put(size.Max));
			AddTypeName(DbType.String, size => "longtext({0})".Put(size.Max));
			AddTypeName(DbType.StringFixedLength, size => "longtext({0})".Put(size.Max));

			AddTypeName(DbType.Binary, size => "binary({0})".Put(size.Max));

			AddTypeName(DbType.Boolean, "bit");

			AddTypeName(DbType.Byte, "byte");
			AddTypeName(DbType.SByte, "byte");

			AddTypeName(DbType.Currency, "currency");
			AddTypeName(DbType.Decimal, "currency");

			AddTypeName(DbType.Date, "datetime");
			AddTypeName(DbType.DateTime, "datetime");
			AddTypeName(DbType.DateTimeOffset, "datetime");
			AddTypeName(DbType.Time, "datetime");

			AddTypeName(DbType.Single, "single");
			AddTypeName(DbType.Double, "double");

			AddTypeName(DbType.Int16, "short");
			AddTypeName(DbType.Int32, "long");
			AddTypeName(DbType.Int64, "currency");
			AddTypeName(DbType.UInt16, "short");
			AddTypeName(DbType.UInt32, "long");
			AddTypeName(DbType.UInt64, "currency");

			//AddTypeName(DbType.VarNumeric, size => "numeric({0})".Put(size));

			//AddTypeName(DbType.Guid, "char(38)");
			//AddTypeName(DbType.Object, "blob");
			//AddTypeName(DbType.Xml, "xml");
		}

		#endregion

		#region SqlRenderer Members

		protected override string ParameterPrefix
		{
			get { return "[?"; }
		}

		protected override string ParameterSuffix
		{
			get { return "]"; }
		}

		public override string GetIdentitySelect(Schema schema)
		{
			return "@@identity as " + schema.Identity.Name;
		}

		protected override string[] ReservedWords
		{
			get { return ArrayHelper.Empty<string>(); }
		}

		#endregion
	}
}