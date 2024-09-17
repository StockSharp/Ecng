namespace Ecng.Tests.Common
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Net;
	using System.Text;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ConverterTest
	{
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
			RunDt("D/M/YY HH:mm:ss.ffffFFfff", "1/5/15 18:41:59.456123456", date + new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(456));
			RunTs(@"hh_mm_ss", "18_41_59", time);
			RunTs(@"HH_MM_SS", "18_41_59", time);
			RunTs(@"HH:MM:SS.ffF", "18:41:59.456", new TimeSpan(0, 18, 41, 59, 456));
			RunTs(@"HH:MM:SS.ffFfff", "18:41:59.456123", new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123));
			RunTs(@"HH:MM:SS.ffFfffFFf", "18:41:59.456123456", new TimeSpan(0, 18, 41, 59, 456).AddMicroseconds(123).AddNanoseconds(456));
			"01/05/2015".ToDateTime("dd/MM/yyyy").AssertEqual(date);
			"1/5/2015".ToDateTime("d/M/yyyy").AssertEqual(date);
			"18:41:59".ToTimeSpan(@"hh\:mm\:ss").AssertEqual(time);
		}

		private static void RunDt(string format, string str, DateTime dt)
		{
			var parser = new FastDateTimeParser(format);
			parser.Parse(str).AssertEqual(dt);

			if (format.ContainsIgnoreCase("fffffffff"))
				str = str.Remove(str.Length - 2) + "00";

			parser.ToString(dt).AssertEqual(str);
		}

		private static void RunTs(string format, string str, TimeSpan ts)
		{
			var parser = new FastTimeSpanParser(format);
			parser.Parse(str).AssertEqual(ts);

			if (format.ContainsIgnoreCase("fffffffff"))
				str = str.Remove(str.Length - 2) + "00";

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
			decimal.MinValue.To<int[]>().To<decimal>().AssertEqual(decimal.MinValue);
			decimal.MaxValue.To<int[]>().To<decimal>().AssertEqual(decimal.MaxValue);
		}

		[TestMethod]
		public void ArrayCovariance()
		{
			var objArr = new object[] { 1, 2, 3 };
			var longArr = new long[] { 1, 2, 3 };
			objArr.To<int[]>().To<long[]>().SequenceEqual(longArr).AssertTrue();
			objArr.To<short[]>().To<long[]>().SequenceEqual(longArr).AssertTrue();
			objArr.To<byte[]>().To<long[]>().SequenceEqual(longArr).AssertTrue();
			objArr.To<long[]>().SequenceEqual(longArr).AssertTrue();
			objArr.To<long[]>().To<int[]>().To<long[]>().SequenceEqual(longArr).AssertTrue();
		}

		[TestMethod]
		public void EnumerableCovariance()
		{
			IEnumerable<object> objArr = [1, 2, 3];
			IEnumerable<long> longArr = [1, 2, 3];
			objArr.To<IEnumerable<int>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<short>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<byte>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<long>>().To<IEnumerable<int>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
		}

		[TestMethod]
		public void EnumerableCovariance2()
		{
			var objArr = new object[] { 1, 2, 3 };
			var longArr = new long[] { 1, 2, 3 };
			objArr.To<IEnumerable<int>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<short>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<byte>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
			objArr.To<IEnumerable<long>>().To<IEnumerable<int>>().To<IEnumerable<long>>().SequenceEqual(longArr).AssertTrue();
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
		public void EndpointTest()
		{
			static void _<T>(T e)
				=> e.To<string>().To<T>().AssertEqual(e);

			_(IPEndPoint.Parse("127.0.0.1:443"));
			_(new DnsEndPoint("google.com", 443));
			_<EndPoint>(new DnsEndPoint("google.com", 443));
		}
	}
}