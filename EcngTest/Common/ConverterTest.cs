namespace Ecng.Test.Common
{
	using System;
	using System.Collections.ObjectModel;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class ConverterTest
	{
		[TestMethod]
		public void Type2String()
		{
			typeof(ObservableCollection<int>).To<string>().To<Type>().AssertEqual(typeof(ObservableCollection<int>));
			typeof(int).To<string>().To<Type>().AssertEqual(typeof(int));
			"int".To<Type>().AssertEqual(typeof(int));
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
			RunTs(@"hh_mm_ss", "18_41_59", time);
			RunTs(@"HH_MM_SS", "18_41_59", time);
			RunTs(@"HH:MM:SS.ffF", "18:41:59.456", new TimeSpan(0, 18, 41, 59, 456));
			"01/05/2015".ToDateTime("dd/MM/yyyy").AssertEqual(date);
			"1/5/2015".ToDateTime("d/M/yyyy").AssertEqual(date);
			"18:41:59".ToTimeSpan(@"hh\:mm\:ss").AssertEqual(time);
		}

		private static void RunDt(string format, string str, DateTime dt)
		{
			var parser = new FastDateTimeParser(format);
			parser.Parse(str).AssertEqual(dt);
			parser.ToString(dt).AssertEqual(str);
		}

		private static void RunTs(string format, string str, TimeSpan ts)
		{
			var parser = new FastTimeSpanParser(format);
			parser.Parse(str).AssertEqual(ts);
			parser.ToString(ts).AssertEqual(str);
		}
	}
}