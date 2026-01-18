namespace Ecng.Tests.Common;

using System.Security;
using System.Collections;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Data;

using Ecng.ComponentModel;

[TestClass]
public class ConverterTest
{
	[TestMethod]
	public void TimeZone()
	{
		static void _(TimeZoneInfo tz)
			=> tz.To<string>().To<TimeZoneInfo>().AssertEqual(tz);

		_(TimeZoneInfo.Utc);
		_(TimeHelper.Moscow);
		_(TimeHelper.Korea);
	}

	[TestMethod]
	public void String2Bool()
	{
		1.To<bool>().AssertTrue();
		0.To<bool>().AssertFalse();

		"1".To<bool>().AssertTrue();
		"0".To<bool>().AssertFalse();

		"true".To<bool>().AssertTrue();
		"false".To<bool>().AssertFalse();

		"True".To<bool>().AssertTrue();
		"False".To<bool>().AssertFalse();

		"TRUE".To<bool>().AssertTrue();
		"FALSE".To<bool>().AssertFalse();
	}

	[TestMethod]
	public void Type2String()
	{
		typeof(ObservableCollection<int>).To<string>().To<Type>().AssertEqual(typeof(ObservableCollection<int>));
		typeof(int).To<string>().To<Type>().AssertEqual(typeof(int));
		"int".To<Type>().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void TypeCrossPlatform()
	{
		"System.Int32".To<Type>().AssertEqual(typeof(int));
		"System.Int32, mscorlib".To<Type>().AssertEqual(typeof(int));
		"System.Int32, mscorlib".ToLowerInvariant().To<Type>().AssertEqual(typeof(int));
		"System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".To<Type>().AssertEqual(typeof(int));
		"System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089".ToLowerInvariant().To<Type>().AssertEqual(typeof(int));
		"System.Int32, System.Private.CoreLib".To<Type>().AssertEqual(typeof(int));
		"System.Int32, System.Private.CoreLib".ToLowerInvariant().To<Type>().AssertEqual(typeof(int));
		"System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e".To<Type>().AssertEqual(typeof(int));
		"System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e".ToLowerInvariant().To<Type>().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void FastTimeParser()
	{
		var date = new DateTime(2015, 5, 1);
		var time = new TimeSpan(18, 41, 59);
		RunDt("yyyyMMdd", "20150501", date);
		RunDt("yyyyddMM", "20150105", date);
		RunDt("ddMMyyyy", "01052015", date);
		RunDt("ddmmyyyy", "01052015", date);
		RunDt("d/MM/yyyy", "1/05/2015", date);
		RunDt("dd/m/yyyy", "01/5/2015", date);
		RunDt("dd/m/yy", "01/5/15", date);
		RunDt("D/M/YYYY", "1/5/2015", date);
		RunDt("DD/MM/YYYY", "01/05/2015", date);
		RunDt("D/M/YY", "1/5/15", date);
		RunDt("D/M/YY", "1/5/15", date);
		RunDt("D/M/YY HH:mm:ss.fff", "1/5/15 00:00:00.000", date);
		RunDt("D/M/YY HH:mm:ss.fff", "1/5/15 18:41:59.456", date + new TimeSpan(0, 18, 41, 59, 456));
		RunDt("D/M/YY HH:mm:ss.ffffff", "1/5/15 18:41:59.456123", date + new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123));
		RunDt("D/M/YY HH:mm:ss.ffffFFf", "1/5/15 18:41:59.4561234", date + new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(456));
		RunDt("D/M/YY HH:mm:ss.ffffFFfff", "1/5/15 18:41:59.456123456", date + new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(456));
		RunTs(@"hh_mm_ss", "18_41_59", time);
		RunTs(@"HH_MM_SS", "18_41_59", time);
		RunTs(@"HH:MM:SS.ffF", "18:41:59.456", new TimeSpan(0, 18, 41, 59, 456));
		RunTs(@"HH:MM:SS.ffFfff", "18:41:59.456123", new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123));
		RunTs(@"HH:MM:SS.ffFfffF", "18:41:59.4561234", new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(456));
		RunTs(@"HH:MM:SS.ffFfffFFf", "18:41:59.456123456", new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(456));
		RunTs("hhmmssfffffff", "1841594561234", new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(400));
		"01/05/2015".ToDateTime("dd/MM/yyyy").AssertEqual(date);
		"1/5/2015".ToDateTime("d/M/yyyy").AssertEqual(date);
		"18:41:59".ToTimeSpan(@"hh\:mm\:ss").AssertEqual(time);
	}

	private static void RunDt(string format, string str, DateTime dt)
	{
		var parser = new FastDateTimeParser(format);
		parser.Parse(str).AssertEqual(dt);

		if (format.ContainsIgnoreCase("fffffffff"))
			str = str[..^2] + "00";

		parser.ToString(dt).AssertEqual(str);
	}

	private static void RunTs(string format, string str, TimeSpan ts)
	{
		var parser = new FastTimeSpanParser(format);
		parser.Parse(str).AssertEqual(ts);

		if (format.ContainsIgnoreCase("fffffffff"))
			str = str[..^2] + "00";

		parser.ToString(ts).AssertEqual(str);
	}

	private const string _uri = "net.tcp://localhost:8000";

	[TestMethod]
	public void Uri()
	{
		_uri.To<Uri>().To<string>().AssertEqual(_uri + "/");
	}

	[TestMethod]
	public void EndPoint()
	{
		_uri.To<Uri>().To<string>().To<EndPoint>().To<string>().AssertEqual(_uri);
		_uri.To<EndPoint>().To<string>().AssertEqual(_uri);
		_uri.To<Uri>().To<string>().To<EndPoint>().AssertEqual(_uri.To<EndPoint>());
		(_uri + "/").To<EndPoint>().To<string>().AssertEqual(_uri);
	}

	[TestMethod]
	public void DbNull()
	{
		DBNull.Value.To<DateTime?>().AssertNull();
		DBNull.Value.To<string>().AssertNull();
	}

	[TestMethod]
	public void Decimal()
	{
		static void _(decimal v)
		{
			v.To<int[]>().To<decimal>().AssertEqual(v);
			v.To<byte[]>().To<decimal>().AssertEqual(v);
		}

		_(decimal.MinValue);
		_(decimal.MaxValue);
		_(RandomGen.GetDecimal(8, 8));
	}

	[TestMethod]
	public void ArrayCovariance()
	{
		var objArr = new object[] { 1, 2, 3 };
		var longArr = new long[] { 1L, 2L, 3L };
		objArr.To<int[]>().To<long[]>().AssertEqual(longArr);
		objArr.To<short[]>().To<long[]>().AssertEqual(longArr);
		objArr.To<byte[]>().To<long[]>().AssertEqual(longArr);
		objArr.To<long[]>().AssertEqual(longArr);
		objArr.To<long[]>().To<int[]>().To<long[]>().AssertEqual(longArr);
	}

	[TestMethod]
	public void EnumerableCovariance()
	{
		IEnumerable<object> objArr = new object[] { 1, 2, 3 };
		IEnumerable<long> longArr = new long[] { 1L, 2L, 3L };
		objArr.To<IEnumerable<int>>().To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<short>>().To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<byte>>().To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<long>>().To<IEnumerable<int>>().To<IEnumerable<long>>().AssertEqual(longArr);
	}

	[TestMethod]
	public void EnumerableCovariance2()
	{
		var objArr = new object[] { 1, 2, 3 };
		var longArr = new long[] { 1, 2, 3 };
		objArr.To<IEnumerable<int>>().To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<short>>().To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<byte>>().To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<long>>().AssertEqual(longArr);
		objArr.To<IEnumerable<long>>().To<IEnumerable<int>>().To<IEnumerable<long>>().AssertEqual(longArr);
	}

	[TestMethod]
	public void IPAddr()
	{
		const string ip = "95.46.7.4";
		ip.AssertEqual(ip.To<IPAddress>().To<long>().To<IPAddress>().To<string>());

		var loopback = IPAddress.Loopback;
		loopback.AssertEqual(loopback.To<long>().To<IPAddress>());
		loopback.AssertEqual(loopback.To<string>().To<IPAddress>());
	}

	[TestMethod]
	public void DateTimeConvert()
	{
		static void Do<T>(T v)
		{
			v.To<long>().To<T>().AssertEqual(v);
			//v.To<string>().To<T>().AssertEqual(v);
		}

		Do(DateTime.UtcNow);
		Do(DateTimeOffset.UtcNow);

		Do(DateTime.MinValue);
		Do(DateTime.MaxValue);

		Do(DateTimeOffset.MinValue);
		Do(DateTimeOffset.MaxValue);
	}

	[TestMethod]
	public void DateTimeOffsetConvert()
	{
		static void ToDt(DateTimeOffset dto)
			=> dto.To<DateTime>().AssertEqual(dto.UtcDateTime);

		static void ToDto(DateTime dt, DateTimeOffset? expected = default)
			=> dt.To<DateTimeOffset>().AssertEqual(expected ?? dt);

		ToDt(DateTimeOffset.Now);
		ToDt(DateTimeOffset.UtcNow);
		ToDt(DateTimeOffset.MinValue);
		ToDt(DateTimeOffset.MaxValue);

		ToDto(DateTime.Now);
		ToDto(DateTime.UtcNow);
		ToDto(DateTime.MinValue, DateTimeOffset.MinValue);
		ToDto(DateTime.MaxValue, DateTimeOffset.MaxValue);
	}

	[TestMethod]
	public void ImplicitExplicit()
	{
		10m.To<Price>().To<decimal>().AssertEqual(10m);
	}

	private class Price2
	{
		public decimal Value { get; set; }

		public static explicit operator Price(Price2 v)
			=> new() { Value = v.Value };

		public static implicit operator Price2(Price v)
			=> new() { Value = v.Value };
	}

	[TestMethod]
	public void ImplicitExplicit2()
	{
		10m.To<Price>().To<Price2>().To<Price>().To<decimal>().AssertEqual(10m);
	}

	private class TestConvert<T>
	{
		public T Value { get; set; }

		public static explicit operator T(TestConvert<T> v)
			=> v.Value;

		public static implicit operator TestConvert<T>(T v)
			=> new() { Value = v };
	}

	[TestMethod]
	public void ImplicitExplicitGeneric()
	{
		10m.To<TestConvert<decimal>>().To<decimal>().AssertEqual(10m);
	}

	private class Price3
	{
		static Price3()
		{
			Converter.AddTypedConverter<Price3, decimal>(input => input.Value);
			Converter.AddTypedConverter<decimal, Price3>(input => new() { Value = input });
		}

		public decimal Value { get; set; }

		public static string SomeMethod(int _)
		{
			return string.Empty;
		}

		public override string ToString() => Value.To<string>();
	}

	[TestMethod]
	public void ImplicitExplicitStatic()
	{
		10m.To<Price3>().To<decimal>().AssertEqual(10m);
		10m.To<Price3>().To<string>().AssertEqual("10");
	}

	[TestMethod]
	public void EncodingTest()
	{
		static void _(Encoding e)
			=> e.To<int>().To<Encoding>().AssertEqual(e);

		_(Encoding.UTF8);
		_(StringHelper.WindowsCyrillic);
	}

	[TestMethod]
	public void Endpoint()
	{
		static void _<T>(T e)
			=> e.To<string>().To<T>().AssertEqual(e);

		_(IPEndPoint.Parse("127.0.0.1:443"));
		_(new DnsEndPoint("google.com", 443));
		_<EndPoint>(new DnsEndPoint("google.com", 443));
	}

	[TestMethod]
	public void MixedTypes()
	{
		// bool <-> string
		true.To<string>().To<bool>().AssertTrue();
		"false".To<bool>().AssertFalse();

		// int <-> string
		123.To<string>().To<int>().AssertEqual(123);
		"456".To<int>().AssertEqual(456);

		// double <-> string
		1.23.To<string>().To<double>().AssertEqual(1.23);
		"2.34".To<double>().AssertEqual(2.34);

		// decimal <-> string
		123.45m.To<string>().To<decimal>().AssertEqual(123.45m);
		"678.90".To<decimal>().AssertEqual(678.90m);

		// DateTime <-> string
		var dt = DateTime.UtcNow;
		dt.To<long>().To<DateTime>().AssertEqual(dt);

		// DateTimeOffset <-> string
		var dto = DateTimeOffset.UtcNow;
		dto.To<long>().To<DateTimeOffset>().AssertEqual(dto);

		// Guid <-> string
		var guid = Guid.NewGuid();
		guid.To<string>().To<Guid>().AssertEqual(guid);

		// IPAddress <-> string
		var ip = IPAddress.Parse("127.0.0.1");
		ip.To<string>().To<IPAddress>().AssertEqual(ip);
		ip.To<long>().To<IPAddress>().AssertEqual(ip);

		// EndPoint <-> string
		var ep = new IPEndPoint(ip, 8080);
		ep.To<string>().To<EndPoint>().AssertEqual(ep);

		// IPv6 EndPoint parsing - bracket notation
		var ipv6Loopback = new IPEndPoint(IPAddress.IPv6Loopback, 8080);
		var ipv6Str = "[::1]:8080";
		var parsed = ipv6Str.To<EndPoint>();
		(parsed is IPEndPoint).AssertTrue($"'{ipv6Str}' should parse as IPEndPoint, got {parsed.GetType().Name}");
		parsed.AssertEqual(ipv6Loopback, $"'{ipv6Str}' should parse to IPv6 loopback endpoint");

		// IPv6 round-trip (endpoint to string and back)
		var ipv6RoundTrip = ipv6Loopback.To<string>().To<EndPoint>();
		ipv6RoundTrip.AssertEqual(ipv6Loopback, "IPv6 endpoint should round-trip correctly");

		// IPv6 without brackets (common format from ToString)
		var ipv6NoBrackets = "::1:8080"; // This is how IPv6 endpoint converts to string
		var parsedNoBrackets = ipv6NoBrackets.To<EndPoint>();
		(parsedNoBrackets is IPEndPoint).AssertTrue($"'{ipv6NoBrackets}' should parse as IPEndPoint");
		parsedNoBrackets.AssertEqual(ipv6Loopback, $"'{ipv6NoBrackets}' should equal IPv6 loopback:8080");

		// Full IPv6 address with port
		var fullIpv6 = "[2001:db8::1]:443";
		var parsedFull = fullIpv6.To<EndPoint>();
		(parsedFull is IPEndPoint).AssertTrue($"'{fullIpv6}' should parse as IPEndPoint");
		((IPEndPoint)parsedFull).Port.AssertEqual(443);

		// StringBuilder <-> string
		var sb = new StringBuilder("abc");
		sb.To<string>().To<StringBuilder>().ToString().AssertEqual("abc");

		// SecureString <-> string
		var sec = "secret".Secure();
		sec.To<string>().AssertEqual("secret");
		"secret".To<SecureString>().UnSecure().AssertEqual("secret");

		// byte[] <-> string
		const string helloStr = "hello";
		helloStr.To<byte[]>().To<string>().AssertEqual(helloStr);

		// char[] <-> string
		var chars = "test".ToCharArray();
		chars.To<string>().ToCharArray().AssertEqual(chars);

		// Enum <-> string
		PriceTypes.Percent.To<string>().To<PriceTypes>().AssertEqual(PriceTypes.Percent);

		// Type <-> string
		typeof(int).To<string>().To<Type>().AssertEqual(typeof(int));

		// decimal <-> int[]
		123.45m.To<int[]>().To<decimal>().AssertEqual(123.45m);

		// BitArray <-> bool[]
		var bools = new[] { true, false, true };
		bools.To<BitArray>().To<bool[]>().AssertEqual(bools);

		// CultureInfo <-> string
		var ci = CultureInfo.InvariantCulture;
		ci.To<string>().To<CultureInfo>().AssertEqual(ci);

		// Encoding <-> int
		Encoding.UTF8.To<int>().To<Encoding>().AssertEqual(Encoding.UTF8);

		// Guid? <-> string
		guid.To<string>().To<Guid?>().AssertEqual(guid);
		string.Empty.To<Guid?>().AssertNull();

		// DateTime? <-> string
		dt.To<string>().To<DateTime>().ToUniversalTime().AssertEqual(dt.ToUniversalTime());
		string.Empty.To<DateTime?>().AssertNull();

		// TimeSpan <-> string
		var ts = TimeSpan.FromMinutes(5);
		ts.To<string>().To<TimeSpan>().AssertEqual(ts);
	}

	[TestMethod]
	public void ChangeOrder()
	{
		// Test that ChangeOrder does not modify the array for little-endian
		var bytes = new byte[] { 0x01, 0x02, 0x03, 0x04 };
		var le = bytes.ChangeOrder(4, true);
		le.AssertEqual(bytes);

		// Test that ChangeOrder reverses the array for big-endian
		var be = bytes.ChangeOrder(4, false);
		be.AssertEqual([0x04, 0x03, 0x02, 0x01]);
	}

	[TestMethod]
	public void ToRadix()
	{
		long value = 255;
		// Test conversion to binary
		value.ToRadix(2).AssertEqual("11111111");
		// Test conversion to octal
		value.ToRadix(8).AssertEqual("377");
		// Test conversion to decimal
		value.ToRadix(10).AssertEqual("255");
		// Test conversion to hexadecimal
		value.ToRadix(16).AssertEqual("FF");
		// Test conversion to base36
		value.ToRadix(36).AssertEqual("73");

		// Test conversion of negative value to hexadecimal
		(-255L).ToRadix(16).AssertEqual("-FF");
		// Test conversion of zero to binary
		0L.ToRadix(2).AssertEqual("0");
	}

	[TestMethod]
	public void DbTypeConversion()
	{
		// Test Type <-> DbType round-trip
		typeof(int).To<DbType>().AssertEqual(DbType.Int32);
		typeof(string).To<DbType>().AssertEqual(DbType.String);
		typeof(DateTime).To<DbType>().AssertEqual(DbType.DateTime);
		typeof(bool).To<DbType>().AssertEqual(DbType.Boolean);
		typeof(byte[]).To<DbType>().AssertEqual(DbType.Binary);
		typeof(decimal).To<DbType>().AssertEqual(DbType.Decimal);
		typeof(Guid).To<DbType>().AssertEqual(DbType.Guid);
		typeof(double).To<DbType>().AssertEqual(DbType.Double);
		typeof(float).To<DbType>().AssertEqual(DbType.Single);
		typeof(long).To<DbType>().AssertEqual(DbType.Int64);
		typeof(short).To<DbType>().AssertEqual(DbType.Int16);
		typeof(object).To<DbType>().AssertEqual(DbType.Object);

		// Test DbType <-> Type round-trip
		DbType.Int32.To<Type>().AssertEqual(typeof(int));
		DbType.String.To<Type>().AssertEqual(typeof(string));
		DbType.DateTime.To<Type>().AssertEqual(typeof(DateTime));
		DbType.Boolean.To<Type>().AssertEqual(typeof(bool));
		DbType.Binary.To<Type>().AssertEqual(typeof(byte[]));
		DbType.Decimal.To<Type>().AssertEqual(typeof(decimal));
		DbType.Guid.To<Type>().AssertEqual(typeof(Guid));
		DbType.Double.To<Type>().AssertEqual(typeof(double));
		DbType.Single.To<Type>().AssertEqual(typeof(float));
		DbType.Int64.To<Type>().AssertEqual(typeof(long));
		DbType.Int16.To<Type>().AssertEqual(typeof(short));
		DbType.Object.To<Type>().AssertEqual(typeof(object));
	}
}