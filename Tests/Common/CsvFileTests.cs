namespace Ecng.Tests.Common;

using System.Text;

[TestClass]
public class CsvFileTests
{
	[TestMethod]
	public void WriteAndRead_SimpleRow_Sync()
	{
		using var ms = new MemoryStream();
		// writer
		using var writer = new CsvFileWriter(ms, Encoding.UTF8);
		writer.Delimiter = ',';
		writer.LineSeparator = "\n";
		writer.WriteRow(["col1", "col2", "col,3"]);
		writer.Flush();

		ms.Position = 0;

		// reader
		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';
		var cols = new List<string>();
		Assert.IsTrue(reader.ReadRow(cols));
		cols.Count.AssertEqual(3);
		cols[0].AssertEqual("col1");
		cols[1].AssertEqual("col2");
		cols[2].AssertEqual("col,3");
	}

	[TestMethod]
	public async Task WriteAndRead_QuotedMultiline_Async()
	{
		using var ms = new MemoryStream();
		using var writer = new CsvFileWriter(ms, Encoding.UTF8);
		writer.Delimiter = ',';
		writer.LineSeparator = "\n";

		var multi = "line1\nline2";
		var quoteInside = "he said \"hello\"";

		await writer.WriteRowAsync(["a", multi, quoteInside]);
		await writer.FlushAsync().ConfigureAwait(false);

		ms.Position = 0;

		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';

		var cols = new List<string>();
		Assert.IsTrue(await reader.ReadRowAsync(cols));
		cols.Count.AssertEqual(3);
		cols[0].AssertEqual("a");
		cols[1].AssertEqual(multi);
		cols[2].AssertEqual(quoteInside);
	}

	[TestMethod]
	public void EmptyLineBehavior_Various()
	{
		var content = "a,b\n\n c,d\n"; // note: space before c to ensure trimming not applied by reader

		// NoColumns
		using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.NoColumns);
			r.Delimiter = ',';
			var cols = new List<string>();
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { "a", "b" });
			Assert.IsTrue(r.ReadRow(cols));
			cols.Count.AssertEqual(0);
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { " c", "d" });
			Assert.IsFalse(r.ReadRow(cols));
		}

		// EmptyColumn
		using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.EmptyColumn);
			r.Delimiter = ',';
			var cols = new List<string>();
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { "a", "b" });
			Assert.IsTrue(r.ReadRow(cols));
			cols.Count.AssertEqual(1);
			cols[0].AssertEqual(string.Empty);
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { " c", "d" });
			Assert.IsFalse(r.ReadRow(cols));
		}

		// Ignore
		using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.Ignore);
			r.Delimiter = ',';
			var cols = new List<string>();
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { "a", "b" });
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { " c", "d" });
			Assert.IsFalse(r.ReadRow(cols));
		}

		// EndOfFile
		using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.EndOfFile);
			r.Delimiter = ',';
			var cols = new List<string>();
			Assert.IsTrue(r.ReadRow(cols));
			cols.AssertEqual(new[] { "a", "b" });
			Assert.IsFalse(r.ReadRow(cols));
		}
	}

	// Large data tests
	[TestMethod]
	public void LargeWriteRead_Sync()
	{
		const int rows = 2000;
		const int colsCount = 10;
		const int cellSize = 200; // ~4MB total

		using var ms = new MemoryStream();
		using var writer = new CsvFileWriter(ms, Encoding.UTF8);
		writer.Delimiter = ',';
		writer.LineSeparator = "\n";

		for (int r = 0; r < rows; r++)
		{
			var arr = new string[colsCount];
			for (int c = 0; c < colsCount; c++)
				arr[c] = $"R{r}C{c}_" + new string((char)('a' + (c % 26)), cellSize);
			writer.WriteRow(arr);
		}
		writer.Flush();

		ms.Position = 0;

		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';
		var cols = new List<string>();
		int ri = 0;
		while (reader.ReadRow(cols))
		{
			if (ri % 500 == 0)
			{
				for (int c = 0; c < colsCount; c++)
				{
					var expected = $"R{ri}C{c}_" + new string((char)('a' + (c % 26)), cellSize);
					cols[c].AssertEqual(expected);
				}
			}
			ri++;
		}
		ri.AssertEqual(rows);
	}

	[TestMethod]
	public async Task LargeWriteRead_Async()
	{
		const int rows = 2000;
		const int colsCount = 10;
		const int cellSize = 200; // ~4MB total

		using var ms = new MemoryStream();
		using var writer = new CsvFileWriter(ms, Encoding.UTF8);
		writer.Delimiter = ',';
		writer.LineSeparator = "\n";

		for (int r = 0; r < rows; r++)
		{
			var arr = new string[colsCount];
			for (int c = 0; c < colsCount; c++)
				arr[c] = $"R{r}C{c}_" + new string((char)('a' + (c % 26)), cellSize);
			await writer.WriteRowAsync(arr).ConfigureAwait(false);
		}
		await writer.FlushAsync().ConfigureAwait(false);

		ms.Position = 0;

		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';
		var cols = new List<string>();
		int ri = 0;
		while (await reader.ReadRowAsync(cols).ConfigureAwait(false))
		{
			if (ri % 500 == 0)
			{
				for (int c = 0; c < colsCount; c++)
				{
					var expected = $"R{ri}C{c}_" + new string((char)('a' + (c % 26)), cellSize);
					cols[c].AssertEqual(expected);
				}
			}
			ri++;
		}
		ri.AssertEqual(rows);
	}
}
