#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Data;

using Ecng.Data;

[TestClass]
public class DatabaseCommandHelperTests : BaseTestClass
{
	[TestMethod]
	public void ToDbType_DateTime_MapsToDateTime2()
	{
		// CLR DateTime range is 0001-01-01 .. 9999-12-31. The legacy
		// DbType.DateTime maps to SQL Server's `datetime` (1753-9999),
		// which rejects values like DateTime.MinValue at bind time with
		// SqlDateTime overflow even when the column itself is DATETIME2.
		// DbType.DateTime2 matches the schema's DATETIME2 emission and
		// the full CLR range, so it's the right wire type.
		typeof(DateTime).ToDbType().AssertEqual(DbType.DateTime2);
	}

	[TestMethod]
	public void ToDbType_NullableDateTime_MapsToDateTime2()
	{
		typeof(DateTime?).ToDbType().AssertEqual(DbType.DateTime2);
	}

	[TestMethod]
	public void ToDbType_DateTimeOffset_MapsToDateTimeOffset()
	{
		typeof(DateTimeOffset).ToDbType().AssertEqual(DbType.DateTimeOffset);
	}

	[TestMethod]
	public void ToDbType_TimeSpan_MapsToInt64()
	{
		// TimeSpan rides as ticks (Int64) — preserved sub-millisecond precision
		// across all dialects without dialect-specific TIME handling.
		typeof(TimeSpan).ToDbType().AssertEqual(DbType.Int64);
	}
}

#endif
