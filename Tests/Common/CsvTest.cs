namespace Ecng.Tests.Common;

using System.IO;

[TestClass]
public class CsvTest
{
	[TestMethod]
	public void DoubleQuotes()
	{
		Assert(@"AFKS@TQBR;""АФК """"Система"""" ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
""AFLT@TQBR"";Аэрофлот-росс.авиалин(ПАО)ао;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQBR;ГДР ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
			(i, r) =>
			{
				var id = r.ReadString();
				var name = r.ReadString();

				switch (i)
				{
					case 0:
						id.AssertEqual("AFKS@TQBR");
						name.AssertEqual(@"АФК ""Система"" ПАО ао");
						break;
					case 1:
						id.AssertEqual("AFLT@TQBR");
						name.AssertEqual(@"Аэрофлот-росс.авиалин(ПАО)ао");
						break;
					case 2:
						id.AssertEqual("AGRO@TQBR");
						name.AssertEqual(@"ГДР ROS AGRO PLC ORD SHS");
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void DoubleQuotes2()
	{
		Assert(@"""""""AFKS@TQBR"""""";""АФК """"Система"""" ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
AFLT@TQ""""BR;Аэрофлот-росс.авиалин(ПАО)ао;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQBR;ГДР ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
			(i, r) =>
			{
				var id = r.ReadString();
				var name = r.ReadString();

				switch (i)
				{
					case 0:
						id.AssertEqual(@"""AFKS@TQBR""");
						name.AssertEqual(@"АФК ""Система"" ПАО ао");
						break;
					case 1:
						id.AssertEqual("AFLT@TQBR");
						name.AssertEqual(@"Аэрофлот-росс.авиалин(ПАО)ао");
						break;
					case 2:
						id.AssertEqual("AGRO@TQBR");
						name.AssertEqual(@"ГДР ROS AGRO PLC ORD SHS");
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void SingleQuotes()
	{
		Assert(@"AFKS@TQBR;""АФК 'Система' ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
""AFLT@TQBR"";Аэрофлот-росс.авиалин(ПАО)ао;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQ'BR;ГДР ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
			(i, r) =>
			{
				var id = r.ReadString();
				var name = r.ReadString();

				switch (i)
				{
					case 0:
						id.AssertEqual("AFKS@TQBR");
						name.AssertEqual(@"АФК 'Система' ПАО ао");
						break;
					case 1:
						id.AssertEqual("AFLT@TQBR");
						name.AssertEqual(@"Аэрофлот-росс.авиалин(ПАО)ао");
						break;
					case 2:
						id.AssertEqual("AGRO@TQ'BR");
						name.AssertEqual(@"ГДР ROS AGRO PLC ORD SHS");
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void SingleQuotes2()
	{
		Assert(@"""'AFKS@TQBR'"";""АФК 'Система' ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
AFLT@TQBR;Аэрофлот-росс.авиалин(ПАО)ао;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQBR;ГДР ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
			(i, r) =>
			{
				var id = r.ReadString();
				var name = r.ReadString();

				switch (i)
				{
					case 0:
						id.AssertEqual(@"'AFKS@TQBR'");
						name.AssertEqual(@"АФК 'Система' ПАО ао");
						break;
					case 1:
						id.AssertEqual("AFLT@TQBR");
						name.AssertEqual(@"Аэрофлот-росс.авиалин(ПАО)ао");
						break;
					case 2:
						id.AssertEqual("AGRO@TQBR");
						name.AssertEqual(@"ГДР ROS AGRO PLC ORD SHS");
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	private enum Sides
	{
		Buy,
		Sell,
	}

	[TestMethod]
	public void NegativeDecimals()
	{
		Assert(@"210000000;+03:00;-10;-1;-1;-0.1;Buy
210000000;-03:30;-0.1;-1;-1;-0.1;Sell", 2,
			(i, r) =>
			{
				switch (i)
				{
					case 0:
						r.ReadDateTime("HHmmssfff").AssertEqual(DateTime.Today.Add(new TimeSpan(21, 0, 0)));
						TimeSpan.Parse(r.ReadString().Remove("+")).AssertEqual(new TimeSpan(3, 0, 0));
						r.ReadDecimal().AssertEqual(-10);
						r.ReadInt().AssertEqual(-1);
						r.ReadLong().AssertEqual(-1);
						r.ReadDouble().AssertEqual(-0.1);
						r.ReadEnum<Sides>().AssertEqual(Sides.Buy);
						break;
					case 1:
						r.ReadDateTime("HHmmssfff").AssertEqual(DateTime.Today.Add(new TimeSpan(21, 0, 0)));
						TimeSpan.Parse(r.ReadString().Remove("+")).AssertEqual(new TimeSpan(-3, -30, 0));
						r.ReadDecimal().AssertEqual(-0.1m);
						r.ReadInt().AssertEqual(-1);
						r.ReadLong().AssertEqual(-1);
						r.ReadDouble().AssertEqual(-0.1);
						r.ReadEnum<Sides>().AssertEqual(Sides.Sell);
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void PositiveDecimals()
	{
		Assert(@"210000000;+03:00;+10;+1;+1;+0.1;Buy
210000000;-03:30;+0.1;+1;+1;+0.1;Sell", 2,
			(i, r) =>
			{
				switch (i)
				{
					case 0:
						r.ReadDateTime("HHmmssfff").AssertEqual(DateTime.Today.Add(new TimeSpan(21, 0, 0)));
						TimeSpan.Parse(r.ReadString().Remove("+")).AssertEqual(new TimeSpan(3, 0, 0));
						r.ReadDecimal().AssertEqual(10);
						r.ReadInt().AssertEqual(1);
						r.ReadLong().AssertEqual(1);
						r.ReadDouble().AssertEqual(0.1);
						r.ReadEnum<Sides>().AssertEqual(Sides.Buy);
						break;
					case 1:
						r.ReadDateTime("HHmmssfff").AssertEqual(DateTime.Today.Add(new TimeSpan(21, 0, 0)));
						TimeSpan.Parse(r.ReadString().Remove("+")).AssertEqual(new TimeSpan(-3, -30, 0));
						r.ReadDecimal().AssertEqual(0.1m);
						r.ReadInt().AssertEqual(1);
						r.ReadLong().AssertEqual(1);
						r.ReadDouble().AssertEqual(0.1);
						r.ReadEnum<Sides>().AssertEqual(Sides.Sell);
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void BigNumber()
	{
		Assert($@"210000001;+03:00;{decimal.MaxValue};{decimal.MinValue};0;Buy", 1,
			(i, r) =>
			{
				switch (i)
				{
					case 0:
						r.ReadDateTime("HHmmssfff").AssertEqual(DateTime.Today.Add(new TimeSpan(0, 21, 0, 0, 1)));
						TimeSpan.Parse(r.ReadString().Remove("+")).AssertEqual(new TimeSpan(3, 0, 0));
						r.ReadDecimal().AssertEqual(decimal.MaxValue);
						r.ReadDecimal().AssertEqual(decimal.MinValue);
						r.ReadDecimal().AssertEqual(0);
						r.ReadEnum<Sides>().AssertEqual(Sides.Buy);
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void BigFractal()
	{
		Assert("1;3.3333333333;2;3.3333333333333333;3.3333333333333333333333333333", 1,
			(i, r) =>
			{
				switch (i)
				{
					case 0:
						r.ReadInt().AssertEqual(1);
						r.ReadDecimal().AssertEqual(3.3333333333m);
						r.ReadInt().AssertEqual(2);
						r.ReadDecimal().AssertEqual(3.3333333333333333m);
						r.ReadDecimal().AssertEqual(3.3333333333333333333333333333m);
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	[TestMethod]
	public void SingleDecimal()
	{
		Assert("3.3", 1,
			(i, r) =>
			{
				switch (i)
				{
					case 0:
						r.ReadDecimal().AssertEqual(3.3m);
						break;
					default:
						throw new InvalidOperationException();
				}
			});
	}

	private static void Assert(string value, int lineCount, Action<int, FastCsvReader> assertLine)
	{
		Do.Invariant(() =>
		{
			var csvReader = new FastCsvReader(new StringReader(value), StringHelper.N);

			var lines = 0;

			while (csvReader.NextLine())
			{
				assertLine(lines, csvReader);
				lines++;
			}

			lines.AssertEqual(lineCount);
		});
	}

	[TestMethod]
	public void TwoDigitYear()
	{
		var parser = new FastDateTimeParser("yy-MM-dd");
		var dt = parser.Parse("98-01-01");

		dt.Year.AssertEqual(1998);
		dt.Month.AssertEqual(1);
		dt.Day.AssertEqual(1);
	}

	[TestMethod]
	public void ShortInput()
	{
		var parser = new FastDateTimeParser("yyyy-MM-dd HH:mm:ss.fff");
		var shortInput = "2024-01-02 03:04:0"; // too short for seconds+millis

		Microsoft.VisualStudio.TestTools.UnitTesting.Assert.ThrowsExactly<FormatException>(() => parser.Parse(shortInput));
	}
}