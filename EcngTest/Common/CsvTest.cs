namespace Ecng.Test.Common
{
	using System;
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

		private static void Assert(string value, int lineCount, Action<int, FastCsvReader> assertLine)
		{
			var csvReader = new FastCsvReader(new StringReader(value));

			var lines = 0;

			while (csvReader.NextLine())
			{
				assertLine(lines, csvReader);
				lines++;
			}

			lines.AssertEqual(lineCount);
		}
	}
}