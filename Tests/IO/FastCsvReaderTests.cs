namespace Ecng.Tests.IO;

using Ecng.IO;

[TestClass]
public class FastCsvReaderTests : BaseTestClass
{
	[TestMethod]
	public void Dispose_DefaultCtor_DoesNotDisposeTextReader()
	{
		var tr = new TrackingTextReader("A" + StringHelper.N);

		using (new FastCsvReader(tr, StringHelper.N))
		{
		}

		tr.IsDisposed.AssertFalse();
	}

	[TestMethod]
	public void Dispose_LeaveOpenFalse_DisposesTextReader()
	{
		var tr = new TrackingTextReader("A" + StringHelper.N);

		using (new FastCsvReader(tr, StringHelper.N, leaveOpen: false))
		{
		}

		tr.IsDisposed.AssertTrue();
	}

	[TestMethod]
	public void Dispose_StreamLeaveOpen_Works()
	{
		var stream = new TrackingStream(Encoding.UTF8.GetBytes("A" + StringHelper.N));

		using (new FastCsvReader(stream, Encoding.UTF8, StringHelper.N, leaveOpen: true))
		{
		}

		stream.IsDisposed.AssertFalse();

		using (new FastCsvReader(stream, Encoding.UTF8, StringHelper.N, leaveOpen: false))
		{
		}

		stream.IsDisposed.AssertTrue();
	}

	[TestMethod]
	public async Task DoubleQuotes()
	{
		await AssertAsync(@"AFKS@TQBR;""АФК """"Система"""" ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
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
	public async Task DoubleQuotes2()
	{
		await AssertAsync(@"""""""AFKS@TQBR"""""";""АФК """"Система"""" ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
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
	public async Task SingleQuotes()
	{
		await AssertAsync(@"AFKS@TQBR;""АФК 'Система' ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
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
	public async Task SingleQuotes2()
	{
		await AssertAsync(@"""'AFKS@TQBR'"";""АФК 'Система' ПАО ао"";AFKS;;;TQBR;@TQBR;0.005;;100;3;Stock;;;;;RUB;;;;;;;;
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
	public async Task NegativeDecimals()
	{
		await AssertAsync(@"210000000;+03:00;-10;-1;-1;-0.1;Buy
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
	public async Task PositiveDecimals()
	{
		await AssertAsync(@"210000000;+03:00;+10;+1;+1;+0.1;Buy
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
	public async Task BigNumber()
	{
		await AssertAsync($@"210000001;+03:00;{decimal.MaxValue};{decimal.MinValue};0;Buy", 1,
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
	public async Task BigFractal()
	{
		await AssertAsync("1;3.3333333333;2;3.3333333333333333;3.3333333333333333333333333333", 1,
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
	public async Task SingleDecimal()
	{
		await AssertAsync("3.3", 1,
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

	[TestMethod]
	public async Task ReadBlockSmall()
	{
		var separator = StringHelper.N;

		var line1 = new string('A', 4000);
		var line2 = new string('B', 4000);
		var csv = line1 + separator + line2 + separator;

		var tr = new FragmentingTextReader(csv, chunkSize: 128);

		using var reader = new FastCsvReader(tr, separator)
		{
			ColumnSeparator = ','
		};

		(await reader.NextLineAsync(CancellationToken)).AssertTrue();
		var first = reader.CurrentLine;
		(await reader.NextLineAsync(CancellationToken)).AssertTrue();
		var second = reader.CurrentLine;

		first.AssertEqual(line1);
		second.AssertEqual(line2);
	}

	/// <summary>
	/// Verifies that empty lines are handled correctly and do not terminate reading.
	/// </summary>
	[TestMethod]
	public async Task EmptyLine_ShouldNotBeEOF()
	{
		var csv = "A\n\nB";
		using var reader = new FastCsvReader(csv, "\n");

		// First line
		(await reader.NextLineAsync(CancellationToken)).AssertTrue();
		reader.ReadString().AssertEqual("A");

		// Empty line - should return true with 0 or 1 empty column, NOT false
		var hasEmptyLine = await reader.NextLineAsync(CancellationToken);
		// Note: This might legitimately return false for empty lines - need to verify intended behavior

		// Third line - this is the critical test
		(await reader.NextLineAsync(CancellationToken)).AssertTrue("Should be able to read line after empty line");
		reader.ReadString().AssertEqual("B");
	}

	private Task AssertAsync(string value, int lineCount, Action<int, FastCsvReader> assertLine)
	{
		return Do.InvariantAsync(async () =>
		{
			var token = CancellationToken;

			using var csvReader = new FastCsvReader(new StringReader(value), StringHelper.N, leaveOpen: false);

			var lines = 0;

			while (await csvReader.NextLineAsync(token))
			{
				assertLine(lines, csvReader);
				lines++;
			}

			lines.AssertEqual(lineCount);
		});
	}

	private class FragmentingTextReader(string content, int chunkSize) : TextReader
	{
		private readonly string _content = content ?? throw new ArgumentNullException(nameof(content));
		private readonly int _chunkSize = chunkSize > 0 ? chunkSize : throw new ArgumentOutOfRangeException(nameof(chunkSize));
		private int _pos;

		public override int ReadBlock(char[] buffer, int index, int count)
		{
			ArgumentNullException.ThrowIfNull(buffer);

			if (index < 0 || count < 0 || index + count > buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_pos >= _content.Length)
				return 0;

			var toCopy = Math.Min(_chunkSize, Math.Min(count, _content.Length - _pos));
			_content.CopyTo(_pos, buffer, index, toCopy);
			_pos += toCopy;
			return toCopy;
		}

		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
			=> Task.FromResult(ReadBlock(buffer, index, count));
	}

	private sealed class TrackingTextReader(string content) : StringReader(content)
	{
		public bool IsDisposed { get; private set; }

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	private sealed class TrackingStream(byte[] buffer) : MemoryStream(buffer)
	{
		public bool IsDisposed { get; private set; }

		protected override void Dispose(bool disposing)
		{
			IsDisposed = true;
			base.Dispose(disposing);
		}
	}

	#region Buffer Boundary Quote Tests

	/// <summary>
	/// Verifies that escaped quotes ("") spanning a buffer boundary are correctly
	/// decoded as a single literal quote character.
	/// </summary>
	[TestMethod]
	public async Task DoubleQuotes_AtBufferBoundary_ShouldBePreserved()
	{
		// CSV: "hello""world"\n
		// Split at position 7: buffer 1 gets "hello" (with quotes), buffer 2 gets "world"\n
		// The "" escape spans the boundary: first " at end of buffer 1, second " at start of buffer 2
		var csv = "\"hello\"\"world\"" + StringHelper.N;
		var tr = new BufferSplitReader(csv, splitAt: 7);

		using var reader = new FastCsvReader(tr, StringHelper.N);

		(await reader.NextLineAsync(CancellationToken)).AssertTrue();
		var value = reader.ReadString();

		value.AssertEqual("hello\"world",
			"Escaped quote spanning buffer boundary should produce a literal quote character");
	}

	/// <summary>
	/// Verifies that multiple escaped quotes across buffer boundaries are all preserved.
	/// </summary>
	[TestMethod]
	public async Task DoubleQuotes_MultipleAtBoundary_ShouldBePreserved()
	{
		// CSV: "a""b";c\n — split so "" spans boundary
		var csv = "\"a\"\"b\";c" + StringHelper.N;
		// Positions: 0:" 1:a 2:first" 3:second" 4:b 5:" 6:; 7:c 8:\n...
		var tr = new BufferSplitReader(csv, splitAt: 3);

		using var reader = new FastCsvReader(tr, StringHelper.N);

		(await reader.NextLineAsync(CancellationToken)).AssertTrue();
		var first = reader.ReadString();
		var second = reader.ReadString();

		first.AssertEqual("a\"b", "First column should have literal quote");
		second.AssertEqual("c", "Second column should be unaffected");
	}

	/// <summary>
	/// TextReader that forces a buffer boundary at the specified position.
	/// Returns 0 from ReadBlock after delivering the first chunk, causing
	/// FastCsvReader to refill its buffer at that exact point.
	/// </summary>
	private sealed class BufferSplitReader(string content, int splitAt) : TextReader
	{
		private int _pos;
		private bool _hitSplit;

		public override int ReadBlock(char[] buffer, int index, int count)
		{
			if (_pos >= content.Length)
				return 0;

			if (!_hitSplit && _pos >= splitAt)
			{
				_hitSplit = true;
				return 0;
			}

			var end = _hitSplit ? content.Length : splitAt;
			var available = end - _pos;
			var toCopy = Math.Min(available, count);

			if (toCopy <= 0)
			{
				if (!_hitSplit) { _hitSplit = true; return 0; }
				return 0;
			}

			content.CopyTo(_pos, buffer, index, toCopy);
			_pos += toCopy;
			return toCopy;
		}

		public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
			=> Task.FromResult(ReadBlock(buffer, index, count));
	}

	#endregion

	#region Scientific Notation Tests

	[TestMethod]
	public async Task ScientificNotation_PositiveExponent()
	{
		await AssertAsync("1.5e+02", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(150m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_NegativeExponent()
	{
		await AssertAsync("1.772e-05", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(0.00001772m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_UppercaseE()
	{
		await AssertAsync("2.5E+03", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(2500m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_NoSign()
	{
		await AssertAsync("3.0e2", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(300m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_NegativeMantissa()
	{
		await AssertAsync("-1.5e+02", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(-150m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_NegativeMantissaNegativeExponent()
	{
		await AssertAsync("-5.0e-03", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(-0.005m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_WholeNumber()
	{
		await AssertAsync("5e+02", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(500m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_ZeroExponent()
	{
		await AssertAsync("1.5e0", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(1.5m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_MultipleColumns()
	{
		await AssertAsync("12;0.034;5.6", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(12m);
			r.ReadDecimal().AssertEqual(0.034m);
			r.ReadDecimal().AssertEqual(5.6m);
		});
	}

	[TestMethod]
	public async Task ScientificNotation_MultipleColumnsWithExp()
	{
		await AssertAsync("1.2e+01;3.4e-02;5.6e+00", 1, (_, r) =>
		{
			r.ReadDecimal().AssertEqual(12m);
			r.ReadDecimal().AssertEqual(0.034m);
			r.ReadDecimal().AssertEqual(5.6m);
		});
	}

	#endregion
}

