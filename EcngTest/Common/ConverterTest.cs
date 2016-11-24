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
		public void FastDateTimeParser()
		{
			var date = new DateTime(2015, 5, 1);
			var time = new TimeSpan(18, 41, 59);
			new FastDateTimeParser("yyyyMMdd").Parse("20150501").AssertEqual(date);
			new FastDateTimeParser("yyyyddMM").Parse("20150105").AssertEqual(date);
			new FastDateTimeParser("ddMMyyyy").Parse("01052015").AssertEqual(date);
			new FastDateTimeParser("ddmmyyyy").Parse("01052015").AssertEqual(date);
			new FastDateTimeParser("d/MM/yyyy").Parse("1/05/2015").AssertEqual(date);
			new FastDateTimeParser("dd/m/yyyy").Parse("01/5/2015").AssertEqual(date);
			new FastDateTimeParser("dd/m/yy").Parse("01/5/15").AssertEqual(date);
			new FastDateTimeParser("D/M/YYYY").Parse("1/5/2015").AssertEqual(date);
			new FastDateTimeParser("DD/MM/YYYY").Parse("01/05/2015").AssertEqual(date);
			new FastDateTimeParser("D/M/YY").Parse("1/5/15").AssertEqual(date);
			new FastDateTimeParser("D/M/YY").Parse("1/5/15").AssertEqual(date);
			new FastTimeSpanParser(@"hh_mm_ss").Parse("18_41_59").AssertEqual(time);
			new FastTimeSpanParser(@"HH_MM_SS").Parse("18_41_59").AssertEqual(time);
			"01/05/2015".ToDateTime("dd/MM/yyyy").AssertEqual(date);
			"1/5/2015".ToDateTime("d/M/yyyy").AssertEqual(date);
			"18:41:59".ToTimeSpan(@"hh\:mm\:ss").AssertEqual(time);
		}
	}
}