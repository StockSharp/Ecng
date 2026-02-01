namespace Ecng.Tests.IO;

using System.Text;

using Ecng.IO;

[TestClass]
public class CsvFileTests : BaseTestClass
{
	[TestMethod]
	public async Task WriteAndRead_SimpleRow()
	{
		var ct = CancellationToken;

		using var ms = new MemoryStream();
		// writer
		using var writer = new CsvFileWriter(ms, Encoding.UTF8);
		writer.Delimiter = ',';
		writer.LineSeparator = "\n";
		await writer.WriteRowAsync(["col1", "col2", "col,3"], ct);
		await writer.FlushAsync(ct);

		ms.Position = 0;

		// reader
		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';
		var cols = new List<string>();
		(await reader.ReadRowAsync(cols, ct)).AssertTrue();
		cols.Count.AssertEqual(3);
		cols[0].AssertEqual("col1");
		cols[1].AssertEqual("col2");
		cols[2].AssertEqual("col,3");
	}

	[TestMethod]
	public async Task WriteAndRead_QuotedMultiline_Async()
	{
		var ct = CancellationToken;

		using var ms = new MemoryStream();
		using var writer = new CsvFileWriter(ms, Encoding.UTF8);
		writer.Delimiter = ',';
		writer.LineSeparator = "\n";

		var multi = "line1\nline2";
		var quoteInside = "he said \"hello\"";

		await writer.WriteRowAsync(["a", multi, quoteInside], ct);
		await writer.FlushAsync(ct).NoWait();

		ms.Position = 0;

		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';

		var cols = new List<string>();
		(await reader.ReadRowAsync(cols, ct)).AssertTrue();
		cols.Count.AssertEqual(3);
		cols[0].AssertEqual("a");
		cols[1].AssertEqual(multi);
		cols[2].AssertEqual(quoteInside);
	}

	[TestMethod]
	public async Task EmptyLineBehavior_Various()
	{
		var ct = CancellationToken;
		var content = "a,b\n\n c,d\n"; // note: space before c to ensure trimming not applied by reader

		// NoColumns
		using (var ms = new MemoryStream(content.UTF8()))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.NoColumns);
			r.Delimiter = ',';
			var cols = new List<string>();
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { "a", "b" });
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.Count.AssertEqual(0);
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { " c", "d" });
			(await r.ReadRowAsync(cols, ct)).AssertFalse();
		}

		// EmptyColumn
		using (var ms = new MemoryStream(content.UTF8()))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.EmptyColumn);
			r.Delimiter = ',';
			var cols = new List<string>();
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { "a", "b" });
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.Count.AssertEqual(1);
			cols[0].AssertEqual(string.Empty);
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { " c", "d" });
			(await r.ReadRowAsync(cols, ct)).AssertFalse();
		}

		// Ignore
		using (var ms = new MemoryStream(content.UTF8()))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.Ignore);
			r.Delimiter = ',';
			var cols = new List<string>();
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { "a", "b" });
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { " c", "d" });
			(await r.ReadRowAsync(cols, ct)).AssertFalse();
		}

		// EndOfFile
		using (var ms = new MemoryStream(content.UTF8()))
		{
			using var r = new CsvFileReader(ms, "\n", EmptyLineBehavior.EndOfFile);
			r.Delimiter = ',';
			var cols = new List<string>();
			(await r.ReadRowAsync(cols, ct)).AssertTrue();
			cols.AssertEqual(new[] { "a", "b" });
			(await r.ReadRowAsync(cols, ct)).AssertFalse();
		}
	}

	[TestMethod]
	public async Task LargeWriteRead()
	{
		var token = CancellationToken;

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
			await writer.WriteRowAsync(arr, token).NoWait();
		}
		await writer.FlushAsync(token).NoWait();

		ms.Position = 0;

		using var reader = new CsvFileReader(ms, "\n");
		reader.Delimiter = ',';
		var cols = new List<string>();
		int ri = 0;
		while (await reader.ReadRowAsync(cols, token).NoWait())
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
