namespace Ecng.Test.Common
{
	using System;
	using System.Globalization;
	using System.IO;

	using Ecng.Common;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CsvTest
	{
		[TestMethod]
		public void DoubleQuotes()
		{
			Assert(@"AFKS@TQBR;""��� """"�������"""" ��� ��"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
""AFLT@TQBR"";��������-����.�������(���)��;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQBR;��� ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
				(i, r) =>
				{
					var id = r.ReadString();
					var name = r.ReadString();

					switch (i)
					{
						case 0:
							id.AssertEqual("AFKS@TQBR");
							name.AssertEqual(@"��� ""�������"" ��� ��");
							break;
						case 1:
							id.AssertEqual("AFLT@TQBR");
							name.AssertEqual(@"��������-����.�������(���)��");
							break;
						case 2:
							id.AssertEqual("AGRO@TQBR");
							name.AssertEqual(@"��� ROS AGRO PLC ORD SHS");
							break;
						default:
							throw new InvalidOperationException();
					}
				});
		}

		[TestMethod]
		public void DoubleQuotes2()
		{
			Assert(@"""""""AFKS@TQBR"""""";""��� """"�������"""" ��� ��"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
AFLT@TQ""""BR;��������-����.�������(���)��;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQBR;��� ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
				(i, r) =>
				{
					var id = r.ReadString();
					var name = r.ReadString();

					switch (i)
					{
						case 0:
							id.AssertEqual(@"""AFKS@TQBR""");
							name.AssertEqual(@"��� ""�������"" ��� ��");
							break;
						case 1:
							id.AssertEqual("AFLT@TQBR");
							name.AssertEqual(@"��������-����.�������(���)��");
							break;
						case 2:
							id.AssertEqual("AGRO@TQBR");
							name.AssertEqual(@"��� ROS AGRO PLC ORD SHS");
							break;
						default:
							throw new InvalidOperationException();
					}
				});
		}

		[TestMethod]
		public void SingleQuotes()
		{
			Assert(@"AFKS@TQBR;""��� '�������' ��� ��"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
""AFLT@TQBR"";��������-����.�������(���)��;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQ'BR;��� ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
				(i, r) =>
				{
					var id = r.ReadString();
					var name = r.ReadString();

					switch (i)
					{
						case 0:
							id.AssertEqual("AFKS@TQBR");
							name.AssertEqual(@"��� '�������' ��� ��");
							break;
						case 1:
							id.AssertEqual("AFLT@TQBR");
							name.AssertEqual(@"��������-����.�������(���)��");
							break;
						case 2:
							id.AssertEqual("AGRO@TQ'BR");
							name.AssertEqual(@"��� ROS AGRO PLC ORD SHS");
							break;
						default:
							throw new InvalidOperationException();
					}
				});
		}

		[TestMethod]
		public void SingleQuotes2()
		{
			Assert(@"""'AFKS@TQBR'"";""��� '�������' ��� ��"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
AFLT@TQBR;��������-����.�������(���)��;AFLT;;;TQBR;@TQBR;0.05;;100;2;Stock;;;;;RUB;;;;;;;;
AGRO@TQBR;��� ROS AGRO PLC ORD SHS;AGRO;;;TQBR;@TQBR;0;;1;0;Stock;;;;;RUB;;;;;;;;", 3,
				(i, r) =>
				{
					var id = r.ReadString();
					var name = r.ReadString();

					switch (i)
					{
						case 0:
							id.AssertEqual(@"'AFKS@TQBR'");
							name.AssertEqual(@"��� '�������' ��� ��");
							break;
						case 1:
							id.AssertEqual("AFLT@TQBR");
							name.AssertEqual(@"��������-����.�������(���)��");
							break;
						case 2:
							id.AssertEqual("AGRO@TQBR");
							name.AssertEqual(@"��� ROS AGRO PLC ORD SHS");
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

		private static void Assert(string value, int lineCount, Action<int, FastCsvReader> assertLine)
		{
			Do.Invariant(() =>
			{
				var csvReader = new FastCsvReader(new StringReader(value));

				var lines = 0;

				while (csvReader.NextLine())
				{
					assertLine(lines, csvReader);
					lines++;
				}

				lines.AssertEqual(lineCount);
			});
		}
	}
}