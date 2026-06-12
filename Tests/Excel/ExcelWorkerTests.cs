namespace Ecng.Tests.Excel;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

using Ecng.Excel;

using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using A = DocumentFormat.OpenXml.Drawing;

[TestClass]
[TestCategory("Integration")]
public class ExcelWorkerTests : BaseTestClass
{
	private static IExcelWorkerProvider CreateProvider(string providerName)
		=> providerName switch
		{
			nameof(DevExpExcelWorkerProvider) => new DevExpExcelWorkerProvider(),
			nameof(OpenXmlExcelWorkerProvider) => new OpenXmlExcelWorkerProvider(),
			_ => throw new ArgumentException($"Unknown provider: {providerName}")
		};

	#region Common tests (both providers)

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void CreateNew_AddSheet_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("TestSheet");

		worker.ContainsSheet("TestSheet").AssertTrue();
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCell_GetCell_StringValue_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Hello World");  // A1

		worker.GetCell<string>(0, 0).AssertEqual("Hello World");
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCell_GetCell_IntValue_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, 42);  // A1

		worker.GetCell<int>(0, 0).AssertEqual(42);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCell_GetCell_DecimalValue_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, 123.45m);  // A1

		worker.GetCell<decimal>(0, 0).AssertEqual(123.45m);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCell_GetCell_DateTimeValue_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		var dt = new DateTime(2024, 1, 15, 10, 30, 0);
		worker
			.AddSheet()
			.SetCell(0, 0, dt);  // A1

		worker.GetCell<DateTime>(0, 0).AssertEqual(dt);
	}

	[TestMethod]
	public void OpenXml_GetCell_NullableDateTimeReadsValue()
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream);
		var value = new DateTime(2026, 2, 3, 4, 5, 6);

		worker
			.AddSheet()
			.SetCell(0, 0, value);

		worker.GetCell<DateTime?>(0, 0).AssertEqual(value);
	}

	[TestMethod]
	[DataRow(0)]
	[DataRow(2)]
	public void OpenXml_SetCell_DateTimeOffsetPreservesOwnClockTime(int offsetHours)
	{
		var inputOffset = TimeSpan.FromHours(offsetHours);
		var value = new DateTimeOffset(2026, 1, 1, 10, 0, 0, inputOffset);

		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, value);
		}

		var cell = GetCell(stream, "A1");
		var stored = DateTime.FromOADate(double.Parse(cell.CellValue!.Text, CultureInfo.InvariantCulture));

		stored.AssertEqual(value.DateTime);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCell_GetCell_BoolValue_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, true);  // A1

		worker.GetCell<bool>(0, 0).AssertEqual(true);
	}

	[TestMethod]
	[DataRow(1_000_000_000_000_000L)]
	[DataRow(9_007_199_254_740_991L)]
	public void OpenXml_SetCell_LongWithinDoublePrecisionRangeStaysNumeric(long value)
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, value);
		}

		var cell = GetCell(stream, "A1");

		(cell.DataType?.Value == CellValues.InlineString).AssertFalse();
	}

	[TestMethod]
	public void OpenXml_SetCell_LongMinValueWritesWithoutOverflow()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, long.MinValue);
		}

		var cell = GetCell(stream, "A1");

		cell.InnerText.AssertEqual(long.MinValue.ToString(CultureInfo.InvariantCulture));
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void MultipleSheets_SwitchSheet_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Sheet1")
			.SetCell(0, 0, "Data1")  // A1
			.AddSheet()
			.RenameSheet("Sheet2")
			.SetCell(0, 0, "Data2");  // A1

		worker.ContainsSheet("Sheet1").AssertTrue();
		worker.ContainsSheet("Sheet2").AssertTrue();

		worker.SwitchSheet("Sheet1");
		worker.GetCell<string>(0, 0).AssertEqual("Data1");

		worker.SwitchSheet("Sheet2");
		worker.GetCell<string>(0, 0).AssertEqual("Data2");
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void GetSheetNames_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Alpha")
			.AddSheet()
			.RenameSheet("Beta")
			.AddSheet()
			.RenameSheet("Gamma");

		var names = worker.GetSheetNames().ToArray();
		names.Length.AssertEqual(3);
		names.AssertContains("Alpha");
		names.AssertContains("Beta");
		names.AssertContains("Gamma");
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void MultipleCells_CountsCorrect(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "A1")  // A1
			.SetCell(1, 0, "B1")  // B1
			.SetCell(0, 1, "A2")  // A2
			.SetCell(1, 1, "B2"); // B2

		worker.GetColumnsCount().AssertEqual(2);
		worker.GetRowsCount().AssertEqual(2);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetColumnWidth_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetColumnWidth(0, 20.5)  // Column A
			.SetCell(0, 0, "Test");   // A1

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetRowHeight_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetRowHeight(0, 30.0)   // Row 1
			.SetCell(0, 0, "Test");  // A1

		worker.GetRowsCount().AssertEqual(1);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void FreezeRows_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Header")  // A1
			.SetCell(0, 1, "Data")    // A2
			.FreezeRows(1);

		worker.GetRowsCount().AssertEqual(2);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void FreezeCols_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Label")  // A1
			.SetCell(1, 0, "Value")  // B1
			.FreezeCols(1);

		worker.GetColumnsCount().AssertEqual(2);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void MergeCells_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Merged Header")  // A1
			.MergeCells(0, 0, 2, 0);  // A1:C1

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetHyperlink_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetHyperlink(0, 0, "https://stocksharp.com", "StockSharp");  // A1

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void DeleteSheet_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Keep")
			.AddSheet()
			.RenameSheet("Delete");

		worker.ContainsSheet("Delete").AssertTrue();
		worker.DeleteSheet("Delete");
		worker.ContainsSheet("Delete").AssertFalse();
		worker.ContainsSheet("Keep").AssertTrue();
	}

	#endregion

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetStyle_ColumnFormat_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		var dt = new DateTime(2024, 6, 15, 14, 30, 0);
		worker
			.AddSheet()
			.SetStyle(0, "yyyy-MM-dd")  // Column A
			.SetCell(0, 0, dt);         // A1

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCellFormat_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, 12345.6789)        // A1
			.SetCellFormat(0, 0, "#,##0.00");

		worker.GetCell<double>(0, 0).AssertEqual(12345.6789);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetCellColor_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Colored Cell")         // A1
			.SetCellColor(0, 0, "#FF0000", "#FFFFFF");

		worker.GetCell<string>(0, 0).AssertEqual("Colored Cell");
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void SetConditionalFormatting_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, 100)  // A1
			.SetCell(0, 1, 50)   // A2
			.SetCell(0, 2, 25)   // A3
			.SetConditionalFormatting(0, ComparisonOperator.Greater, "75", "#00FF00", null);  // Column A

		worker.GetRowsCount().AssertEqual(3);
	}

	[TestMethod]
	public void OpenXml_ConditionalFormatting_FillColorIsApplied()
	{
		// Regression: a conditional-formatting (differential) solid fill must put
		// the requested colour in the PatternFill BackgroundColor — that is the
		// slot Excel actually paints for a <dxf> fill (the opposite of a normal
		// cell fill, which uses ForegroundColor). Putting the colour in
		// ForegroundColor with BackgroundColor=Indexed 64 renders the cells black.
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, 100)
				.SetCell(0, 1, 50)
				.SetConditionalFormatting(0, ComparisonOperator.Greater, "75", "#00FF00", "#FFFFFF");
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var dxfs = doc.WorkbookPart?.WorkbookStylesPart?.Stylesheet?.DifferentialFormats;
		(dxfs is not null).AssertTrue();

		var dxf = dxfs.Elements<DifferentialFormat>().First();
		var patternFill = dxf.Fill?.PatternFill;
		(patternFill is not null).AssertTrue();

		// The visible fill colour lives in the BackgroundColor of a dxf fill.
		// Extract first, then assert — a `?.` chain ending in AssertEqual would
		// short-circuit to no-op when the value is null (the very bug we test for).
		var bgRgb = patternFill.BackgroundColor?.Rgb?.Value;
		bgRgb.AssertEqual("FF00FF00");
		// Font colour is applied too.
		var fontRgb = dxf.Font?.Color?.Rgb?.Value;
		fontRgb.AssertEqual("FFFFFFFF");
	}

	[TestMethod]
	public void OpenXml_SetCellColor_FillColorIsApplied()
	{
		// A plain cell fill stores the colour in ForegroundColor of a solid
		// PatternFill; verify the actual bytes, not just that the call survived.
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "x")
				.SetCellColor(0, 0, "#FF0000", "#FFFFFF");  // A1 red on white text
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var stylesheet = doc.WorkbookPart.WorkbookStylesPart.Stylesheet;
		var cell = doc.WorkbookPart.WorksheetParts.First().Worksheet.Descendants<Cell>().First(c => c.CellReference == "A1");
		var xf = stylesheet.CellFormats.Elements<CellFormat>().ElementAt((int)(cell.StyleIndex?.Value ?? 0));
		var fill = stylesheet.Fills.Elements<Fill>().ElementAt((int)(xf.FillId?.Value ?? 0));

		var fgRgb = fill.PatternFill?.ForegroundColor?.Rgb?.Value;
		fgRgb.AssertEqual("FFFF0000");
	}

	[TestMethod]
	public void OpenXml_SetCellColor_PreservesExistingNumberFormat()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, new DateTime(2026, 1, 1))
				.SetCellFormat(0, 0, "yyyy-MM-dd")
				.SetCellColor(0, 0, "#FF0000");
		}

		var cell = GetCell(stream, "A1");
		var format = GetCellFormat(stream, cell);

		(format.NumberFormatId?.Value ?? 0).AssertGreater(0u);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void ComplexWorkflow_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Summary")
			.SetColumnWidth(0, 15)   // Column A
			.SetColumnWidth(1, 25)   // Column B
			.SetColumnWidth(2, 15)   // Column C
			.FreezeRows(1)
			.SetCell(0, 0, "ID")     // A1
			.SetCell(1, 0, "Name")   // B1
			.SetCell(2, 0, "Value")  // C1
			.SetCellColor(0, 0, "LightGray")
			.SetCellColor(1, 0, "LightGray")
			.SetCellColor(2, 0, "LightGray");

		for (var i = 0; i < 10; i++)
		{
			worker
				.SetCell(0, i + 1, i + 1)           // Column A, rows 2-11
				.SetCell(1, i + 1, $"Item {i + 1}") // Column B, rows 2-11
				.SetCell(2, i + 1, (i + 1) * 100.5m); // Column C, rows 2-11
		}

		worker
			.SetConditionalFormatting(2, ComparisonOperator.Greater, "500", "#90EE90", null)  // Column C
			.MergeCells(0, 11, 1, 11)   // A12:B12
			.SetCell(0, 11, "Total:")   // A12
			.SetCell(2, 11, 5527.5m)    // C12
			.SetHyperlink(0, 13, "https://stocksharp.com", "Visit StockSharp");  // A14

		// Note: Row count varies by provider implementation and hyperlink handling
		(worker.GetRowsCount() >= 12).AssertTrue();
		worker.GetColumnsCount().AssertEqual(3);
	}

	#region DevExp-specific tests (features not yet implemented in OpenXml)

	[TestMethod]
	public void DevExp_AutoFitColumn_Success()
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(nameof(DevExpExcelWorkerProvider)).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "This is a long text that should auto-fit")  // A1
			.AutoFitColumn(0);  // Column A

		worker.GetColumnsCount().AssertEqual(1);
	}

	#endregion

	#region OpenXml-specific tests (template support - DevExp is write-only)

	[TestMethod]
	public void OpenXml_OpenExist_ReadValues_Success()
	{
		using var stream = new MemoryStream();

		// Create template
		using (var writer = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			writer
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, "Header1")  // A1
				.SetCell(1, 0, "Header2")  // B1
				.SetCell(0, 1, 100)        // A2
				.SetCell(1, 1, 200.5m)     // B2
				.SetCell(0, 2, true)       // A3
				.SetCell(1, 2, new DateTime(2024, 6, 15, 14, 30, 0));  // B3
		}

		// Open and read
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		reader.ContainsSheet("Data").AssertTrue();
		reader.GetCell<string>(0, 0).AssertEqual("Header1");
		reader.GetCell<string>(1, 0).AssertEqual("Header2");
		reader.GetCell<int>(0, 1).AssertEqual(100);
		reader.GetCell<decimal>(1, 1).AssertEqual(200.5m);
		reader.GetCell<bool>(0, 2).AssertEqual(true);
		reader.GetCell<DateTime>(1, 2).AssertEqual(new DateTime(2024, 6, 15, 14, 30, 0));
	}

	[TestMethod]
	public void OpenXml_OpenExist_ModifyValues_Success()
	{
		using var stream = new MemoryStream();

		// Create template
		using (var writer = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			writer
				.AddSheet()
				.RenameSheet("Sheet1")
				.SetCell(0, 0, "Original")  // A1
				.SetCell(0, 1, 100);        // A2
		}

		// Open, modify, save
		stream.Position = 0;
		using (var editor = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			editor.GetCell<string>(0, 0).AssertEqual("Original");
			editor.GetCell<int>(0, 1).AssertEqual(100);

			editor
				.SetCell(0, 0, "Modified")   // A1
				.SetCell(0, 1, 999)          // A2
				.SetCell(0, 2, "New Row");   // A3
		}

		// Reopen and verify
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		reader.GetCell<string>(0, 0).AssertEqual("Modified");
		reader.GetCell<int>(0, 1).AssertEqual(999);
		reader.GetCell<string>(0, 2).AssertEqual("New Row");
	}

	[TestMethod]
	public void OpenXml_OpenExist_MultipleSheets_Success()
	{
		using var stream = new MemoryStream();

		// Create template with multiple sheets
		using (var writer = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			writer
				.AddSheet()
				.RenameSheet("Sales")
				.SetCell(0, 0, "Q1")    // A1
				.SetCell(0, 1, 1000)    // A2
				.AddSheet()
				.RenameSheet("Expenses")
				.SetCell(0, 0, "Rent")  // A1
				.SetCell(0, 1, 500);    // A2
		}

		// Open and navigate sheets
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		var sheets = reader.GetSheetNames().ToArray();
		sheets.Length.AssertEqual(2);
		sheets.AssertContains("Sales");
		sheets.AssertContains("Expenses");

		// First sheet is active by default
		reader.GetCell<string>(0, 0).AssertEqual("Q1");

		reader.SwitchSheet("Expenses");
		reader.GetCell<string>(0, 0).AssertEqual("Rent");
		reader.GetCell<int>(0, 1).AssertEqual(500);

		reader.SwitchSheet("Sales");
		reader.GetCell<int>(0, 1).AssertEqual(1000);
	}

	[TestMethod]
	public void OpenXml_OpenExist_AddNewSheet_Success()
	{
		using var stream = new MemoryStream();

		// Create template
		using (var writer = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			writer
				.AddSheet()
				.RenameSheet("Original")
				.SetCell(0, 0, "Data");  // A1
		}

		// Open and add new sheet
		stream.Position = 0;
		using (var editor = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			editor
				.AddSheet()
				.RenameSheet("Added")
				.SetCell(0, 0, "New Data");  // A1
		}

		// Verify both sheets exist
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		var sheets = reader.GetSheetNames().ToArray();
		sheets.Length.AssertEqual(2);
		sheets.AssertContains("Original");
		sheets.AssertContains("Added");

		reader.SwitchSheet("Original");
		reader.GetCell<string>(0, 0).AssertEqual("Data");

		reader.SwitchSheet("Added");
		reader.GetCell<string>(0, 0).AssertEqual("New Data");
	}

	[TestMethod]
	public void OpenXml_OpenExist_PreserveStyles_Success()
	{
		using var stream = new MemoryStream();

		// Create template with styles
		using (var writer = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			writer
				.AddSheet()
				.SetStyle(0, "#,##0.00")            // Column A
				.SetCell(0, 0, 1234.5678)           // A1
				.SetCellColor(1, 0, "#FF0000")      // B1
				.SetCell(1, 0, "Red");              // B1
		}

		// Open and add more data (styles should be preserved)
		stream.Position = 0;
		using (var editor = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			editor
				.SetCell(0, 1, 9999.1234)   // A2
				.SetCell(1, 1, "Plain");    // B2
		}

		// Verify values (style verification would require opening in Excel)
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		reader.GetCell<double>(0, 0).AssertEqual(1234.5678);
		reader.GetCell<double>(0, 1).AssertEqual(9999.1234);
		reader.GetCell<string>(1, 0).AssertEqual("Red");
		reader.GetCell<string>(1, 1).AssertEqual("Plain");
	}

	#endregion

	#region Chart tests

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddLineChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Data")
			.SetCell(0, 0, "X")    // A1
			.SetCell(1, 0, "Y")    // B1
			.SetCell(0, 1, 1)      // A2
			.SetCell(1, 1, 10)     // B2
			.SetCell(0, 2, 2)      // A3
			.SetCell(1, 2, 20)     // B3
			.SetCell(0, 3, 3)      // A4
			.SetCell(1, 3, 30)     // B4
			.AddLineChart("Test Line Chart", "A2:B4", 1, 2, 3, 0, 400, 300);  // xCol/yCol 1-based

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddBarChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Data")
			.SetCell(0, 0, "Category")  // A1
			.SetCell(1, 0, "Value")     // B1
			.SetCell(0, 1, "A")         // A2
			.SetCell(1, 1, 100)         // B2
			.SetCell(0, 2, "B")         // A3
			.SetCell(1, 2, 200)         // B3
			.SetCell(0, 3, "C")         // A4
			.SetCell(1, 3, 150)         // B4
			.AddBarChart("Test Bar Chart", "A2:B4", 3, 0, 400, 300);

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddPieChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Data")
			.SetCell(0, 0, "Label")       // A1
			.SetCell(1, 0, "Value")       // B1
			.SetCell(0, 1, "Sales")       // A2
			.SetCell(1, 1, 45)            // B2
			.SetCell(0, 2, "Marketing")   // A3
			.SetCell(1, 2, 30)            // B3
			.SetCell(0, 3, "Development") // A4
			.SetCell(1, 3, 25)            // B4
			.AddPieChart("Test Pie Chart", "A2:B4", 3, 0, 400, 300);

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddMultipleCharts_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("Dashboard")
			.SetCell(0, 0, "X")    // A1
			.SetCell(1, 0, "Y")    // B1
			.SetCell(0, 1, 1)      // A2
			.SetCell(1, 1, 10)     // B2
			.SetCell(0, 2, 2)      // A3
			.SetCell(1, 2, 25)     // B3
			.SetCell(0, 3, 3)      // A4
			.SetCell(1, 3, 15)     // B4
			.AddLineChart("Line", "A2:B4", 1, 2, 3, 0, 300, 200)  // xCol/yCol 1-based
			.AddBarChart("Bar", "A2:B4", 3, 11, 300, 200)
			.AddPieChart("Pie", "A2:B4", 9, 0, 300, 200);

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	public void OpenXml_AddLineChart_CreatesChartPart()
	{
		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, 1)    // A1
				.SetCell(1, 0, 10)   // B1
				.SetCell(0, 1, 2)    // A2
				.SetCell(1, 1, 20)   // B2
				.AddLineChart("Equity Curve", "A1:B2", 1, 2, 3, 0, 500, 300);  // xCol/yCol 1-based
		}

		// Verify chart was created by reopening
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);
		reader.GetCell<int>(0, 0).AssertEqual(1);
		reader.GetCell<int>(1, 1).AssertEqual(20);
	}

	[TestMethod]
	public void OpenXml_LineChart_ColumnFormulas_AreCorrect()
	{
		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, 1.0)    // A1
				.SetCell(1, 0, 10.0)   // B1
				.SetCell(0, 1, 2.0)    // A2
				.SetCell(1, 1, 20.0)   // B2
				// Note: xCol/yCol are 1-based for chart data column references (1=A, 2=B)
				// anchorCol/anchorRow (3, 0) are 0-based worksheet positions
				.AddLineChart("Test", "Data!$A$1:$B$2", xCol: 1, yCol: 2, 3, 0, 400, 300);
		}

		// Reopen and check formulas in chart XML
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var chartPart = doc.WorkbookPart!.WorksheetParts.First()
			.DrawingsPart!.ChartParts.First();
		var xml = chartPart.ChartSpace.OuterXml;

		// xCol=1 should reference column A, yCol=2 should reference column B
		xml.Contains("$A$").AssertTrue("X formula should reference column A");
		xml.Contains("$B$").AssertTrue("Y formula should reference column B");
	}

	[TestMethod]
	public void OpenXml_AddLineChart_UsesZeroBasedColumnIndexes()
	{
		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, "X")
				.SetCell(1, 0, "Y")
				.SetCell(0, 1, 1)
				.SetCell(1, 1, 2)
				.AddLineChart("Line", "Data!$A$1:$B$2", xCol: 0, yCol: 1, 3, 0, 400, 300);
		}

		var xml = GetFirstChartXml(stream);

		xml.Contains("$$").AssertFalse();
		xml.Contains("$A$1:$A$2").AssertTrue();
		xml.Contains("$B$1:$B$2").AssertTrue();
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddAreaChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Month")   // A1
			.SetCell(1, 0, "Value")   // B1
			.SetCell(0, 1, "Jan")     // A2
			.SetCell(1, 1, 100)       // B2
			.SetCell(0, 2, "Feb")     // A3
			.SetCell(1, 2, 150)       // B3
			.SetCell(0, 3, "Mar")     // A4
			.SetCell(1, 3, 120)       // B4
			.AddAreaChart("Test Area Chart", "A2:B4", 3, 0, 400, 300);

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddDoughnutChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Category")    // A1
			.SetCell(1, 0, "Value")       // B1
			.SetCell(0, 1, "Product A")   // A2
			.SetCell(1, 1, 35)            // B2
			.SetCell(0, 2, "Product B")   // A3
			.SetCell(1, 2, 40)            // B3
			.SetCell(0, 3, "Product C")   // A4
			.SetCell(1, 3, 25)            // B4
			.AddDoughnutChart("Test Doughnut Chart", "A2:B4", 3, 0, 400, 300);

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddScatterChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "X")      // A1
			.SetCell(1, 0, "Y")      // B1
			.SetCell(0, 1, 1.0)      // A2
			.SetCell(1, 1, 2.5)      // B2
			.SetCell(0, 2, 2.0)      // A3
			.SetCell(1, 2, 4.0)      // B3
			.SetCell(0, 3, 3.0)      // A4
			.SetCell(1, 3, 3.5)      // B4
			.AddScatterChart("Test Scatter Chart", "A2:B4", 1, 2, 3, 0, 400, 300);  // xCol/yCol 1-based

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddRadarChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Attribute")    // A1
			.SetCell(1, 0, "Score")        // B1
			.SetCell(0, 1, "Speed")        // A2
			.SetCell(1, 1, 80)             // B2
			.SetCell(0, 2, "Reliability")  // A3
			.SetCell(1, 2, 90)             // B3
			.SetCell(0, 3, "Cost")         // A4
			.SetCell(1, 3, 70)             // B4
			.SetCell(0, 4, "Features")     // A5
			.SetCell(1, 4, 85)             // B5
			.AddRadarChart("Test Radar Chart", "A2:B5", 3, 0, 400, 400);

		worker.GetRowsCount().AssertEqual(5);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddBubbleChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "X")      // A1
			.SetCell(1, 0, "Y")      // B1
			.SetCell(2, 0, "Size")   // C1
			.SetCell(0, 1, 10)       // A2
			.SetCell(1, 1, 20)       // B2
			.SetCell(2, 1, 5)        // C2
			.SetCell(0, 2, 30)       // A3
			.SetCell(1, 2, 40)       // B3
			.SetCell(2, 2, 10)       // C3
			.SetCell(0, 3, 50)       // A4
			.SetCell(1, 3, 25)       // B4
			.SetCell(2, 3, 15)       // C4
			.AddBubbleChart("Test Bubble Chart", "A2:C4", 1, 2, 3, 4, 0, 400, 300);  // xCol/yCol/sizeCol 1-based

		worker.GetRowsCount().AssertEqual(4);
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void AddStockChart_Success(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "Date")        // A1
			.SetCell(1, 0, "Open")        // B1
			.SetCell(2, 0, "High")        // C1
			.SetCell(3, 0, "Low")         // D1
			.SetCell(4, 0, "Close")       // E1
			.SetCell(0, 1, "2024-01-01")  // A2
			.SetCell(1, 1, 100.0)         // B2
			.SetCell(2, 1, 105.0)         // C2
			.SetCell(3, 1, 98.0)          // D2
			.SetCell(4, 1, 103.0)         // E2
			.SetCell(0, 2, "2024-01-02")  // A3
			.SetCell(1, 2, 103.0)         // B3
			.SetCell(2, 2, 108.0)         // C3
			.SetCell(3, 2, 101.0)         // D3
			.SetCell(4, 2, 106.0)         // E3
			.AddStockChart("Test Stock Chart", "A2:E3", 5, 0, 500, 300);

		worker.GetRowsCount().AssertEqual(3);
	}

	[TestMethod]
	public void OpenXml_SetCell_ColumnsBeyondZ_Success()
	{
		// Test columns beyond Z (AA, AB, etc.) to verify ToColumnName fix
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				// Column 0 = A, Column 25 = Z, Column 26 = AA, Column 27 = AB
				.SetCell(0, 0, "A1")
				.SetCell(25, 0, "Z1")
				.SetCell(26, 0, "AA1")
				.SetCell(27, 0, "AB1")
				.SetCell(51, 0, "AZ1")
				.SetCell(52, 0, "BA1");
		}

		// Read back and verify
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var sheetData = doc.WorkbookPart!.WorksheetParts.First().Worksheet.GetFirstChild<SheetData>()!;
		var cells = sheetData.Elements<Row>().First().Elements<Cell>().ToList();

		// Verify cell references
		cells.Any(c => c.CellReference?.Value == "A1").AssertTrue("Cell A1 should exist");
		cells.Any(c => c.CellReference?.Value == "Z1").AssertTrue("Cell Z1 should exist");
		cells.Any(c => c.CellReference?.Value == "AA1").AssertTrue("Cell AA1 should exist");
		cells.Any(c => c.CellReference?.Value == "AB1").AssertTrue("Cell AB1 should exist");
		cells.Any(c => c.CellReference?.Value == "AZ1").AssertTrue("Cell AZ1 should exist");
		cells.Any(c => c.CellReference?.Value == "BA1").AssertTrue("Cell BA1 should exist");
	}

	[TestMethod]
	public void OpenXml_SetCell_CellsBeyondZAreInsertedByColumnIndex()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(2, 0, "C")
				.SetCell(25, 0, "Z")
				.SetCell(26, 0, "AA")
				.SetCell(27, 0, "AB");
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var refs = doc.WorkbookPart!.WorksheetParts.First()
			.Worksheet.Descendants<Row>().First()
			.Elements<Cell>()
			.Select(c => c.CellReference!.Value)
			.ToArray();

		refs.AssertEqual(["C1", "Z1", "AA1", "AB1"]);
	}

	#endregion

	#region OpenXML Validation and Structure Tests

	/// <summary>
	/// Helper: Validates OpenXML document against schema.
	/// </summary>
	private static void AssertOpenXmlValid(MemoryStream stream, FileFormatVersions version = FileFormatVersions.Office2016)
	{
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var validator = new OpenXmlValidator(version);
		var errors = validator.Validate(doc).ToArray();

		if (errors.Length == 0)
			return;

		var msg = errors.Take(20).Select(e =>
			$"- {e.Description}; Part={e.Part?.Uri}; Path={e.Path?.XPath}").JoinN();

		Fail($"OpenXmlValidator found {errors.Length} error(s):\n{msg}");
	}

	private static string GetFirstChartXml(MemoryStream stream)
	{
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);

		return doc.WorkbookPart!.WorksheetParts.First()
			.DrawingsPart!.ChartParts.First()
			.ChartSpace.OuterXml;
	}

	private static Cell GetCell(MemoryStream stream, string cellRef)
	{
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);

		return (Cell)doc.WorkbookPart!.WorksheetParts.First()
			.Worksheet.Descendants<Cell>()
			.First(c => c.CellReference?.Value == cellRef)
			.CloneNode(true);
	}

	private static CellFormat GetCellFormat(MemoryStream stream, Cell cell)
	{
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);

		var styleIndex = (int)(cell.StyleIndex?.Value ?? 0);

		return (CellFormat)doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet
			.CellFormats!.Elements<CellFormat>()
			.ElementAt(styleIndex)
			.CloneNode(true);
	}

	/// <summary>
	/// Helper: Gets cell value directly from XML (not through API).
	/// </summary>
	private static string GetCellValueFromXml(SpreadsheetDocument doc, string cellRef)
	{
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();
		var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>()!;

		var rowIndex = new string(cellRef.Where(char.IsDigit).ToArray()).To<uint>();
		var row = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);
		var cell = row?.Elements<Cell>().FirstOrDefault(c => c.CellReference?.Value == cellRef);

		if (cell == null)
			return null;

		// Handle inline string
		if (cell.DataType?.Value == CellValues.InlineString)
			return cell.InlineString?.Text?.Text;

		// Handle shared string
		if (cell.DataType?.Value == CellValues.SharedString)
		{
			var idx = cell.CellValue!.Text.To<int>();
			return doc.WorkbookPart.SharedStringTablePart?.SharedStringTable
				.Elements<SharedStringItem>().ElementAt(idx).Text?.Text;
		}

		return cell.CellValue?.Text;
	}

	[TestMethod]
	public void OpenXml_IndexingContract_ZeroBased_A1()
	{
		// This test documents and verifies the API contract:
		// SetCell(col=0, row=0) should write to Excel cell A1
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Origin");  // Should be A1
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);

		// Verify through XML that (0,0) maps to A1
		var value = GetCellValueFromXml(doc, "A1");
		value.AssertEqual("Origin", "SetCell(0,0) should write to A1");

		AssertOpenXmlValid(stream);
	}

	[TestMethod]
	public void OpenXml_IndexingContract_ZeroBased_B2()
	{
		// SetCell(col=1, row=1) should write to Excel cell B2
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(1, 1, "B2Value");  // Should be B2
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);

		// Verify through XML that (1,1) maps to B2
		var value = GetCellValueFromXml(doc, "B2");
		value.AssertEqual("B2Value", "SetCell(1,1) should write to B2");

		// A1 should NOT exist
		var a1 = GetCellValueFromXml(doc, "A1");
		a1.AssertNull("A1 should not exist when writing to (1,1)");

		AssertOpenXmlValid(stream);
	}

	[TestMethod]
	public void OpenXml_AddLineChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")
				.SetCell(1, 0, "Y")
				.SetCell(0, 1, 1)
				.SetCell(1, 1, 10)
				.SetCell(0, 2, 2)
				.SetCell(1, 2, 20)
				.AddLineChart("Test Chart", "A2:B3", 1, 2, 3, 0, 400, 300);  // xCol/yCol 1-based
		}

		// Validate schema
		AssertOpenXmlValid(stream);

		// Verify chart structure
		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		// DrawingsPart must exist
		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist after adding chart");

		// ChartParts must contain exactly 1 chart
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart");

		// WorksheetDrawing must have TwoCellAnchor
		var anchors = wsPart.DrawingsPart.WorksheetDrawing.Elements<Xdr.TwoCellAnchor>().ToList();
		anchors.Count.AssertEqual(1, "Should have 1 TwoCellAnchor for chart");
	}

	[TestMethod]
	public void OpenXml_AddBarChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Cat")
				.SetCell(1, 0, "Val")
				.SetCell(0, 1, "A")
				.SetCell(1, 1, 100)
				.AddBarChart("Bar Chart", "A2:B2", 3, 0, 400, 300);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart");
	}

	[TestMethod]
	[DataRow(2, 3, "Data!$C$1:$D$2", "$C$1:$C$2", "$D$1:$D$2")]
	[DataRow(4, 5, "Data!$E$1:$F$2", "$E$1:$E$2", "$F$1:$F$2")]
	public void OpenXml_AddBarChart_UsesColumnsFromDataRange(
		int categoryColumn,
		int valueColumn,
		string dataRange,
		string expectedCategoryRange,
		string expectedValueRange)
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(categoryColumn, 0, "Category")
				.SetCell(valueColumn, 0, "Value")
				.SetCell(categoryColumn, 1, "A")
				.SetCell(valueColumn, 1, 10)
				.AddBarChart("Bar", dataRange, valueColumn + 2, 0, 400, 300);
		}

		var xml = GetFirstChartXml(stream);

		xml.Contains(expectedCategoryRange).AssertTrue();
		xml.Contains(expectedValueRange).AssertTrue();
		xml.Contains("$A$1:$A$2").AssertFalse();
		xml.Contains("$B$1:$B$2").AssertFalse();
	}

	[TestMethod]
	public void OpenXml_FreezeRows_ValidPaneStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Header")
				.SetCell(0, 1, "Data")
				.FreezeRows(1);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
		var sheetViews = worksheet.GetFirstChild<SheetViews>();

		sheetViews.AssertNotNull("SheetViews should exist after FreezeRows");

		var sheetView = sheetViews!.GetFirstChild<SheetView>();
		var pane = sheetView?.GetFirstChild<Pane>();

		pane.AssertNotNull("Pane should exist after FreezeRows");
		pane!.State!.Value.AssertEqual(PaneStateValues.Frozen, "Pane should be frozen");
		pane.VerticalSplit!.Value.AssertEqual(1D, "VerticalSplit should be 1");
	}

	[TestMethod]
	public void OpenXml_FreezeCols_ValidPaneStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Col1")
				.SetCell(1, 0, "Col2")
				.FreezeCols(1);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
		var pane = worksheet.GetFirstChild<SheetViews>()?.GetFirstChild<SheetView>()?.GetFirstChild<Pane>();

		pane.AssertNotNull("Pane should exist after FreezeCols");
		pane!.State!.Value.AssertEqual(PaneStateValues.Frozen, "Pane should be frozen");
		pane.HorizontalSplit!.Value.AssertEqual(1D, "HorizontalSplit should be 1");
	}

	[TestMethod]
	public void OpenXml_FreezeRowsAndCols_CombinesPaneState()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.FreezeRows(1)
				.FreezeCols(1);
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var pane = doc.WorkbookPart!.WorksheetParts.First()
			.Worksheet.GetFirstChild<SheetViews>()?.GetFirstChild<SheetView>()?.GetFirstChild<Pane>();

		pane.AssertNotNull();
		pane!.VerticalSplit!.Value.AssertEqual(1D);
		pane.HorizontalSplit!.Value.AssertEqual(1D);
		pane.TopLeftCell!.Value.AssertEqual("B2");
		pane.ActivePane!.Value.AssertEqual(PaneValues.BottomRight);
	}

	[TestMethod]
	public void OpenXml_MergeCells_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Merged")
				.MergeCells(0, 0, 2, 0);  // A1:C1
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
		var mergeCells = worksheet.GetFirstChild<MergeCells>();

		mergeCells.AssertNotNull("MergeCells element should exist");

		var mergeCell = mergeCells!.GetFirstChild<MergeCell>();
		mergeCell.AssertNotNull("MergeCell should exist");
		mergeCell!.Reference!.Value.AssertEqual("A1:C1", "Merge reference should be A1:C1");
	}

	[TestMethod]
	public void OpenXml_SetHyperlink_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetHyperlink(0, 0, "https://example.com", "Example");
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();
		var worksheet = wsPart.Worksheet;
		var hyperlinks = worksheet.GetFirstChild<Hyperlinks>();

		hyperlinks.AssertNotNull("Hyperlinks element should exist");

		var hyperlink = hyperlinks!.GetFirstChild<Hyperlink>();
		hyperlink.AssertNotNull("Hyperlink should exist");
		hyperlink!.Reference!.Value.AssertEqual("A1", "Hyperlink should reference A1");
		hyperlink.Id.AssertNotNull("Hyperlink should have relationship Id");

		// Verify relationship exists
		var relId = hyperlink.Id!.Value;
		var rel = wsPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == relId);
		rel.AssertNotNull($"Relationship with Id '{relId}' should exist");
		rel!.Uri.ToString().AssertEqual("https://example.com/", "Relationship URI should match");
	}

	[TestMethod]
	public void OpenXml_MultipleCharts_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")
				.SetCell(1, 0, "Y")
				.SetCell(0, 1, 1)
				.SetCell(1, 1, 10)
				.AddLineChart("Line", "A2:B2", 1, 2, 3, 0, 300, 200)  // xCol/yCol 1-based
				.AddBarChart("Bar", "A2:B2", 3, 5, 300, 200);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(2, "Should have 2 ChartParts");
		wsPart.DrawingsPart.WorksheetDrawing.Elements<Xdr.TwoCellAnchor>().Count().AssertEqual(2, "Should have 2 anchors");
	}

	[TestMethod]
	public void OpenXml_BasicWorksheet_PassesValidation()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("TestSheet")
				.SetCell(0, 0, "String")                                 // A1
				.SetCell(1, 0, 123)                                      // B1
				.SetCell(2, 0, 45.67m)                                   // C1
				.SetCell(0, 1, new DateTime(2024, 6, 15, 10, 30, 0))     // A2 - fixed date instead of Now
				.SetCell(1, 1, true);                                    // B2
		}

		AssertOpenXmlValid(stream);
	}

	[TestMethod]
	public void OpenXml_AddPieChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Label")  // A1
				.SetCell(1, 0, "Value")  // B1
				.SetCell(0, 1, "A")      // A2
				.SetCell(1, 1, 50)       // B2
				.AddPieChart("Pie Chart", "A2:B2", 3, 0, 400, 300);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for PieChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for PieChart");
	}

	[TestMethod]
	public void OpenXml_AddPieChart_WithColors_SetsPerSliceFills()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Cat").SetCell(1, 0, "Val") // A1:B1
				.SetCell(0, 1, "A").SetCell(1, 1, 30)       // A2:B2
				.SetCell(0, 2, "B").SetCell(1, 2, 50)       // A3:B3
				.SetCell(0, 3, "C").SetCell(1, 3, 20)       // A4:B4
				.AddPieChart("Pie", "A2:B4", 3, 0, 400, 300, new[] { "#FF0000", "#00FF00", "#0000FF" });
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var chartPart = doc.WorkbookPart!.WorksheetParts.First().DrawingsPart!.ChartParts.First();
		var series = chartPart.ChartSpace.Descendants<C.PieChartSeries>().First();
		var dataPoints = series.Elements<C.DataPoint>().ToList();

		dataPoints.Count.AssertEqual(3);

		// Each data point colours its slice by index with the requested srgbClr (6-hex, no alpha).
		string ColorOf(int idx)
		{
			var dp = dataPoints.Single(d => d.GetFirstChild<C.Index>()!.Val!.Value == (uint)idx);
			return dp.Descendants<A.RgbColorModelHex>().First().Val!.Value;
		}

		ColorOf(0).AssertEqual("FF0000");
		ColorOf(1).AssertEqual("00FF00");
		ColorOf(2).AssertEqual("0000FF");
	}

	[TestMethod]
	public void OpenXml_AddAreaChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")   // A1
				.SetCell(1, 0, "Y")   // B1
				.SetCell(0, 1, 1)     // A2
				.SetCell(1, 1, 10)    // B2
				.AddAreaChart("Area Chart", "A2:B2", 3, 0, 400, 300);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for AreaChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for AreaChart");
	}

	[TestMethod]
	public void OpenXml_AddDoughnutChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Cat")    // A1
				.SetCell(1, 0, "Val")    // B1
				.SetCell(0, 1, "A")      // A2
				.SetCell(1, 1, 40)       // B2
				.AddDoughnutChart("Doughnut Chart", "A2:B2", 3, 0, 400, 300);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for DoughnutChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for DoughnutChart");
	}

	[TestMethod]
	public void OpenXml_AddScatterChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")     // A1
				.SetCell(1, 0, "Y")     // B1
				.SetCell(0, 1, 1.0)     // A2
				.SetCell(1, 1, 2.0)     // B2
				.AddScatterChart("Scatter Chart", "A2:B2", 1, 2, 3, 0, 400, 300);  // xCol/yCol 1-based
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for ScatterChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for ScatterChart");
	}

	[TestMethod]
	public void OpenXml_AddRadarChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Attr")   // A1
				.SetCell(1, 0, "Score")  // B1
				.SetCell(0, 1, "Speed")  // A2
				.SetCell(1, 1, 80)       // B2
				.AddRadarChart("Radar Chart", "A2:B2", 3, 0, 400, 400);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for RadarChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for RadarChart");
	}

	[TestMethod]
	public void OpenXml_AddBubbleChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")      // A1
				.SetCell(1, 0, "Y")      // B1
				.SetCell(2, 0, "Size")   // C1
				.SetCell(0, 1, 10)       // A2
				.SetCell(1, 1, 20)       // B2
				.SetCell(2, 1, 5)        // C2
				.AddBubbleChart("Bubble Chart", "A2:C2", 1, 2, 3, 4, 0, 400, 300);  // xCol/yCol/sizeCol 1-based
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for BubbleChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for BubbleChart");
	}

	[TestMethod]
	public void OpenXml_AddStockChart_ValidStructure()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Date")    // A1
				.SetCell(1, 0, "Open")    // B1
				.SetCell(2, 0, "High")    // C1
				.SetCell(3, 0, "Low")     // D1
				.SetCell(4, 0, "Close")   // E1
				.SetCell(0, 1, "2024-01") // A2
				.SetCell(1, 1, 100.0)     // B2
				.SetCell(2, 1, 105.0)     // C2
				.SetCell(3, 1, 98.0)      // D2
				.SetCell(4, 1, 103.0)     // E2
				.AddStockChart("Stock Chart", "A2:E2", 5, 0, 500, 300);
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();

		wsPart.DrawingsPart.AssertNotNull("DrawingsPart should exist for StockChart");
		wsPart.DrawingsPart!.ChartParts.Count().AssertEqual(1, "Should have 1 ChartPart for StockChart");
	}

	[TestMethod]
	public void OpenXml_AddStockChart_UsesDistinctOhlcColumns()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, "Date")
				.SetCell(1, 0, "Open")
				.SetCell(2, 0, "High")
				.SetCell(3, 0, "Low")
				.SetCell(4, 0, "Close")
				.SetCell(0, 1, new DateTime(2026, 1, 1))
				.SetCell(1, 1, 10)
				.SetCell(2, 1, 12)
				.SetCell(3, 1, 9)
				.SetCell(4, 1, 11)
				.AddStockChart("OHLC", "Data!$A$1:$E$2", 6, 0, 500, 300);
		}

		var xml = GetFirstChartXml(stream);

		xml.Contains("$$").AssertFalse();
		xml.Contains("$A$1:$A$2").AssertTrue();
		xml.Contains("$B$1:$B$2").AssertTrue();
		xml.Contains("$C$1:$C$2").AssertTrue();
		xml.Contains("$D$1:$D$2").AssertTrue();
		xml.Contains("$E$1:$E$2").AssertTrue();
	}

	[TestMethod]
	public void OpenXml_SetConditionalFormattingFormula_WritesExpressionRule()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Status")    // A1
				.SetCell(0, 1, "OVERDUE")   // A2
				.SetCell(0, 2, "OK")        // A3
				// Paint A2:A3 red-on-white whenever the cell text equals "OVERDUE".
				// A real expression rule, so Excel recomputes the fill when the user
				// edits the value — not a static fill baked onto the cell.
				.SetConditionalFormattingFormula(0, 1, 0, 2, "$A2=\"OVERDUE\"", "#FF0000", "#FFFFFF");
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;

		var cf = worksheet.Elements<ConditionalFormatting>().FirstOrDefault();
		cf.AssertNotNull("ConditionalFormatting element should be written");
		cf.SequenceOfReferences.InnerText.AssertEqual("A2:A3");

		var rule = cf.Elements<ConditionalFormattingRule>().First();
		rule.Type.Value.AssertEqual(ConditionalFormatValues.Expression);
		rule.GetFirstChild<Formula>()!.Text.AssertEqual("$A2=\"OVERDUE\"");
		rule.FormatId.AssertNotNull("rule must reference a differential format");

		// The referenced dxf carries the requested fill (bg) and font (fg) colors.
		// A dxf solid fill stores the colour in BackgroundColor (the slot Excel
		// paints), not ForegroundColor.
		var dxfs = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!;
		var dxf = (DifferentialFormat)dxfs.ChildElements[(int)rule.FormatId.Value];
		dxf.Fill!.PatternFill!.BackgroundColor!.Rgb!.Value.AssertEqual("FFFF0000");
		dxf.Font!.GetFirstChild<Color>()!.Rgb!.Value.AssertEqual("FFFFFFFF");
	}

	[TestMethod]
	public void OpenXml_SetConditionalFormattingFormula_AppliesFullFontStyling()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Value") // A1
				.SetCell(0, 1, -5)      // A2
				.SetCell(0, 2, 10)      // A3
				// Highlight negatives with a fully styled font + yellow fill.
				.SetConditionalFormattingFormula(0, 1, 0, 2, "$A2<0", new ExcelConditionalFormat
				{
					BackgroundColor = "#FFFF00",
					FontColor = "#FF0000",
					Bold = true,
					Italic = true,
					Underline = true,
					Strikethrough = true,
					FontSize = 14,
					FontName = "Calibri",
				});
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
		var rule = worksheet.Elements<ConditionalFormatting>().First().Elements<ConditionalFormattingRule>().First();

		var dxfs = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!;
		var dxf = (DifferentialFormat)dxfs.ChildElements[(int)rule.FormatId!.Value];
		var font = dxf.Font!;

		font.GetFirstChild<Bold>()!.Val!.Value.AssertEqual(true);
		font.GetFirstChild<Italic>()!.Val!.Value.AssertEqual(true);
		font.GetFirstChild<Strike>()!.Val!.Value.AssertEqual(true);
		font.GetFirstChild<Underline>()!.Val!.Value.AssertEqual(UnderlineValues.Single);
		font.GetFirstChild<FontSize>()!.Val!.Value.AssertEqual(14d);
		font.GetFirstChild<FontName>()!.Val!.Value.AssertEqual("Calibri");
		font.GetFirstChild<Color>()!.Rgb!.Value.AssertEqual("FFFF0000");
		dxf.Fill!.PatternFill!.BackgroundColor!.Rgb!.Value.AssertEqual("FFFFFF00");

		// No border was requested (Border defaults to None) -> the dxf carries none.
		(dxf.Border is null).AssertTrue("Border=None must not write a border element");
	}

	[TestMethod]
	public void OpenXml_SetConditionalFormattingFormula_AppliesNumberFormatAndBorder()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Rate") // A1
				.SetCell(0, 1, 0.25)   // A2
				.SetConditionalFormattingFormula(0, 1, 0, 1, "$A2>0", new ExcelConditionalFormat
				{
					NumberFormat = "0.00%",
					Border = ExcelBorderStyle.Thick,
					BorderColor = "#0000FF",
				});
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
		var rule = worksheet.Elements<ConditionalFormatting>().First().Elements<ConditionalFormattingRule>().First();

		var dxfs = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!;
		var dxf = (DifferentialFormat)dxfs.ChildElements[(int)rule.FormatId!.Value];

		dxf.NumberingFormat!.FormatCode!.Value.AssertEqual("0.00%");

		var border = dxf.Border!;
		border.LeftBorder!.Style!.Value.AssertEqual(BorderStyleValues.Thick);
		border.LeftBorder!.Color!.Rgb!.Value.AssertEqual("FF0000FF");
		border.RightBorder!.Style!.Value.AssertEqual(BorderStyleValues.Thick);
		border.TopBorder!.Style!.Value.AssertEqual(BorderStyleValues.Thick);
		border.BottomBorder!.Style!.Value.AssertEqual(BorderStyleValues.Thick);
	}

	[TestMethod]
	public void OpenXml_SetConditionalFormattingFormula_BooleanFalseForcesOff()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")  // A1
				.SetCell(0, 1, 1)    // A2
				// false must write the element with Val=false (force OFF), not omit it
				// (which would mean "leave unchanged"). FontColor is set so a font is built.
				.SetConditionalFormattingFormula(0, 1, 0, 1, "$A2>0", new ExcelConditionalFormat
				{
					FontColor = "#000000",
					Bold = false,
					Underline = false,
				});
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var rule = doc.WorkbookPart!.WorksheetParts.First().Worksheet
			.Elements<ConditionalFormatting>().First().Elements<ConditionalFormattingRule>().First();
		var dxfs = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!;
		var font = ((DifferentialFormat)dxfs.ChildElements[(int)rule.FormatId!.Value]).Font!;

		font.GetFirstChild<Bold>()!.Val!.Value.AssertEqual(false);
		font.GetFirstChild<Underline>()!.Val!.Value.AssertEqual(UnderlineValues.None);
	}

	[TestMethod]
	public void OpenXml_SetConditionalFormatting_CellIs_WritesFillAndFontColor()
	{
		// The CellIs path was refactored to share the dxf builder with the formula
		// path; this guards that its differential format is still written correctly.
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, 100) // A1
				.SetCell(0, 1, 50)  // A2
				.SetConditionalFormatting(0, ComparisonOperator.Greater, "75", "#00FF00", "#FFFFFF");
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var rule = doc.WorkbookPart!.WorksheetParts.First().Worksheet
			.Elements<ConditionalFormatting>().First().Elements<ConditionalFormattingRule>().First();
		rule.Type!.Value.AssertEqual(ConditionalFormatValues.CellIs);
		rule.GetFirstChild<Formula>()!.Text.AssertEqual("75");

		var dxfs = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!;
		var dxf = (DifferentialFormat)dxfs.ChildElements[(int)rule.FormatId!.Value];
		dxf.Fill!.PatternFill!.BackgroundColor!.Rgb!.Value.AssertEqual("FF00FF00");
		dxf.Font!.GetFirstChild<Color>()!.Rgb!.Value.AssertEqual("FFFFFFFF");
	}

	[TestMethod]
	public void OpenXml_SetConditionalFormattingFormula_AppliesFillPattern()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")  // A1
				.SetCell(0, 1, 1)    // A2
				// A non-solid pattern: red grid lines over a yellow background.
				.SetConditionalFormattingFormula(0, 1, 0, 1, "$A2>0", new ExcelConditionalFormat
				{
					BackgroundColor = "#FFFF00",
					FillPattern = ExcelFillPattern.LightGrid,
					PatternColor = "#FF0000",
				});
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var rule = doc.WorkbookPart!.WorksheetParts.First().Worksheet
			.Elements<ConditionalFormatting>().First().Elements<ConditionalFormattingRule>().First();
		var dxfs = doc.WorkbookPart.WorkbookStylesPart!.Stylesheet.DifferentialFormats!;
		var pf = ((DifferentialFormat)dxfs.ChildElements[(int)rule.FormatId!.Value]).Fill!.PatternFill!;

		pf.PatternType!.Value.AssertEqual(PatternValues.LightGrid);
		pf.ForegroundColor!.Rgb!.Value.AssertEqual("FFFF0000"); // pattern lines
		pf.BackgroundColor!.Rgb!.Value.AssertEqual("FFFFFF00");  // background
	}

	[TestMethod]
	public void OpenXml_SetCellColor_AppliesFillPattern()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "X")  // A1
				.SetCellColor(0, 0, "#FFFF00", ExcelFillPattern.DarkUp, "#0000FF");
		}

		AssertOpenXmlValid(stream);

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var styles = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet;
		var cell = doc.WorkbookPart.WorksheetParts.First().Worksheet
			.GetFirstChild<SheetData>()!.Elements<Row>().First().Elements<Cell>().First();
		var cellFormat = (CellFormat)styles.CellFormats!.ChildElements[(int)cell.StyleIndex!.Value];
		var pf = ((Fill)styles.Fills!.ChildElements[(int)cellFormat.FillId!.Value]).PatternFill!;

		pf.PatternType!.Value.AssertEqual(PatternValues.DarkUp);
		pf.ForegroundColor!.Rgb!.Value.AssertEqual("FF0000FF"); // pattern lines
		pf.BackgroundColor!.Rgb!.Value.AssertEqual("FFFFFF00");  // background
	}

	#endregion

	#region Bug Reproduction Tests

	// Regression test for GetColumnsCount: returns the column span (max column index + 1),
	// so columns A (0) and C (2) with data report 3. (Was: returned the unique count of
	// columns with data, DevExpExcelWorkerProvider.cs:374 / OpenXmlExcelWorkerProvider.cs:415.)
	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void GetColumnsCount_SparseData_ReturnsSpan(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "A1")   // Column A (0)
			.SetCell(2, 0, "C1");  // Column C (2), skip column B

		// Returns the column span (max column index + 1): A, B, C => 3.
		worker.GetColumnsCount().AssertEqual(3, "GetColumnsCount should return max column span, not unique count");
	}

	[TestMethod]
	[DataRow(nameof(DevExpExcelWorkerProvider))]
	[DataRow(nameof(OpenXmlExcelWorkerProvider))]
	public void GetRowsCount_SparseData_ReturnsSpan(string providerName)
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(providerName).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, "A1")   // Row 1 (0)
			.SetCell(0, 4, "A5");  // Row 5 (4), skip rows 2-4

		// Regression test for GetRowsCount: returns the row span (max row index + 1), so rows
		// 1 (0) and 5 (4) with data report 5. (Was: returned the unique count of rows with data,
		// DevExpExcelWorkerProvider.cs:375 / OpenXmlExcelWorkerProvider.cs:435.)
		worker.GetRowsCount().AssertEqual(5, "GetRowsCount should return max row span, not unique count");
	}

	// Regression test for CreateNew(readOnly: true): rejects the contradictory request to create
	// a new file in read-only mode by throwing InvalidOperationException immediately. (Was: the
	// readOnly parameter was ignored and the file was created normally, OpenXmlExcelWorkerProvider.cs:38.)
	[TestMethod]
	public void OpenXml_CreateNew_ReadOnly_ShouldThrowImmediately()
	{
		using var stream = new MemoryStream();

		Throws<InvalidOperationException>(
			() => CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream, true),
			"CreateNew with readOnly=true should throw InvalidOperationException");
	}

	// Note: OpenExist does NOT have readOnly parameter in interface
	// This is a design issue - OpenExist always opens for editing
	// If readOnly support is needed for OpenExist, interface should be extended

	#endregion

	#region DevExp audit regression tests

	/// <summary>
	/// Regression test for DevExp OpenExist: the write-only provider refuses the unsupported
	/// operation with NotSupportedException, leaving the caller's stream untouched.
	/// (Was: created a brand-new write-only document on top of the stream, corrupting the
	/// existing workbook bytes, DevExpExcelWorkerProvider.cs:542.)
	/// </summary>
	[TestMethod]
	public void OpenExist_ThrowsNotSupported()
	{
		using var stream = new MemoryStream();

		// Fill the stream with some pre-existing bytes representing an existing workbook.
		var original = new byte[] { 1, 2, 3, 4, 5 };
		stream.Write(original, 0, original.Length);
		stream.Position = 0;

		var provider = CreateProvider(nameof(DevExpExcelWorkerProvider));

		Throws<NotSupportedException>(() => provider.OpenExist(stream));
	}

	/// <summary>
	/// Regression test for DevExp SetHyperlink: the produced xlsx contains a real hyperlink
	/// for the cell (the sheet's Hyperlinks collection is populated on write).
	/// (Was: url/text were stored but the Hyperlinks collection was never populated, so the
	/// URL was silently dropped, DevExpExcelWorkerProvider.cs:228.)
	/// </summary>
	[TestMethod]
	public void SetHyperlink_WritesHyperlinkToOutput()
	{
		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(DevExpExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Links")
				.SetHyperlink(0, 0, "https://stocksharp.com", "StockSharp");  // A1
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart.WorksheetParts.First().Worksheet;
		var hyperlinks = worksheet.GetFirstChild<Hyperlinks>();

		hyperlinks.AssertNotNull("Hyperlinks element should exist in the produced workbook");
		hyperlinks.GetFirstChild<Hyperlink>().AssertNotNull("A hyperlink for the cell should be present");
	}

	/// <summary>
	/// Regression test for DevExp SetCellFormat: the produced xlsx stores the literal Excel
	/// number-format code "#,##0.00" verbatim. (Was: SetCellFormat set IsDateTimeFormatString=true
	/// and fed the code into NetFormatString, so DevExpress mangled "#,##0.00" as a date-time
	/// .NET format, DevExpExcelWorkerProvider.cs:182.)
	/// </summary>
	[TestMethod]
	public void SetCellFormat_PreservesNumericFormatCode()
	{
		const string format = "#,##0.00";

		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(DevExpExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, 12345.6789)   // A1
				.SetCellFormat(0, 0, format);
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var stylesPart = doc.WorkbookPart.WorkbookStylesPart;

		stylesPart.AssertNotNull("Styles part should exist");

		var numberingFormats = stylesPart.Stylesheet.NumberingFormats;
		numberingFormats.AssertNotNull("Custom number formats should be present");

		var hasFormat = numberingFormats
			.Elements<NumberingFormat>()
			.Any(nf => nf.FormatCode?.Value == format);

		hasFormat.AssertTrue($"The literal Excel number format '{format}' should be written, not a mangled date-time format");
	}

	/// <summary>
	/// Regression test for DevExp SetCellColor on a value-less cell: a cell that only has
	/// formatting/colors (no value, no hyperlink) is still emitted with its fill.
	/// (Was: such a cell was skipped before the formatting block, so the requested coloring
	/// was lost, DevExpExcelWorkerProvider.cs:171.)
	/// </summary>
	[TestMethod]
	public void SetCellColor_OnValuelessCell_EmitsCell()
	{
		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(DevExpExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Colors")
				.SetCellColor(0, 0, "#FF0000");  // A1 colored but no value
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart.WorksheetParts.First().Worksheet;

		var cell = worksheet.Descendants<Cell>().FirstOrDefault(c => c.CellReference?.Value == "A1");
		cell.AssertNotNull("The value-less but colored cell A1 should still be written to the output");
	}

	/// <summary>
	/// Regression test for DevExp GetCell type conversion: convertible stored values are converted
	/// (SetCell(int 42) then GetCell&lt;double&gt; =&gt; 42.0, GetCell&lt;string&gt; =&gt; "42"), matching the
	/// OpenXml provider and the IExcelWorker contract ("the value of the cell cast to type T").
	/// (Was: a type-pattern (Value is T) returned default(T) for convertible-but-not-exact types,
	/// DevExpExcelWorkerProvider.cs:71.)
	/// </summary>
	[TestMethod]
	public void GetCell_ConvertsCompatibleStoredValue()
	{
		using var stream = new MemoryStream();
		using var worker = CreateProvider(nameof(DevExpExcelWorkerProvider)).CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(0, 0, 42);  // A1, stored as int

		worker.GetCell<double>(0, 0).AssertEqual(42d);
		worker.GetCell<string>(0, 0).AssertEqual("42");
	}

	/// <summary>
	/// Regression test for DevExp ParseColor: a bare 6-digit hex value ("FF0000") is parsed as that
	/// RGB color, so the produced fill is red (FFFF0000). (Was: non-'#' strings went straight to
	/// Color.FromName, which returned a transparent/black color for an unrecognized bare-hex name,
	/// DevExpExcelWorkerProvider.cs:259.)
	/// </summary>
	[TestMethod]
	public void SetCellColor_BareHex_ProducesExpectedFill()
	{
		using var stream = new MemoryStream();

		using (var worker = CreateProvider(nameof(DevExpExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "x")             // A1
				.SetCellColor(0, 0, "FF0000");  // bare hex, no leading '#'
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var stylesheet = doc.WorkbookPart.WorkbookStylesPart.Stylesheet;
		var cell = doc.WorkbookPart.WorksheetParts.First().Worksheet.Descendants<Cell>().First(c => c.CellReference?.Value == "A1");
		var xf = stylesheet.CellFormats.Elements<CellFormat>().ElementAt((int)(cell.StyleIndex?.Value ?? 0));
		var fill = stylesheet.Fills.Elements<Fill>().ElementAt((int)(xf.FillId?.Value ?? 0));

		// The fill color is stored in the solid PatternFill ForegroundColor as ARGB.
		// A correctly parsed bare-hex "FF0000" must yield opaque red (FFFF0000).
		var rgb = fill.PatternFill?.ForegroundColor?.Rgb?.Value;
		rgb.AssertNotNull("A solid fill color should be written for the bare-hex color input");
		rgb.AssertEqual("FFFF0000", "The bare hex 'FF0000' should be parsed as red, not a black/empty color");
	}

	#endregion

	#region OpenXml CT_Worksheet element-order and lifecycle regressions

	/// <summary>
	/// Returns the index of the first child of <paramref name="worksheet"/> whose
	/// runtime type is <typeparamref name="T"/>, or -1 when absent.
	/// </summary>
	private static int IndexOf<T>(Worksheet worksheet)
		where T : OpenXmlElement
	{
		var children = worksheet.ChildElements;
		for (var i = 0; i < children.Count; i++)
			if (children[i] is T)
				return i;

		return -1;
	}

	private static byte[] BuildSimpleWorkbookBytes()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, "Header"); // A1
		}

		return stream.ToArray();
	}

	/// <summary>
	/// Regression test for CT_Worksheet element order: calling FreezeRows() (creates &lt;sheetViews&gt;)
	/// then SetColumnWidth() (creates &lt;cols&gt;) keeps &lt;sheetViews&gt; before &lt;cols&gt;, so the worksheet
	/// passes OpenXml validation. (Was: both elements were inserted at worksheet index 0, yielding
	/// [cols, sheetViews, sheetData] which violates sheetViews &lt; cols, OpenXmlExcelWorkerProvider.cs:449.)
	/// </summary>
	[TestMethod]
	public void OpenXml_FreezeRowsThenSetColumnWidth_KeepsValidElementOrder()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, "Header") // A1
				.SetCell(0, 1, "Data")   // A2
				.FreezeRows(1)           // inserts <sheetViews>
				.SetColumnWidth(0, 20);  // inserts <cols>
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;

		var sheetViewsIndex = IndexOf<SheetViews>(worksheet);
		var colsIndex = IndexOf<Columns>(worksheet);

		sheetViewsIndex.AssertGreater(-1, "SheetViews should exist after FreezeRows");
		colsIndex.AssertGreater(-1, "Columns should exist after SetColumnWidth");
		(sheetViewsIndex < colsIndex).AssertTrue("CT_Worksheet requires <sheetViews> before <cols>");

		AssertOpenXmlValid(stream);
	}

	/// <summary>
	/// Regression test for CT_Worksheet element order on OpenExist: SetColumnWidth inserts &lt;cols&gt;
	/// after the existing &lt;dimension&gt; element of a real xlsx, honouring dimension &lt; cols.
	/// (Was: &lt;cols&gt; was blindly inserted at index 0, before &lt;dimension&gt;,
	/// OpenXmlExcelWorkerProvider.cs:449.)
	/// </summary>
	[TestMethod]
	public void OpenXml_OpenExist_SetColumnWidth_KeepsColsAfterDimension()
	{
		using var stream = new MemoryStream();

		// Build a minimal but realistic template whose worksheet starts with <dimension>.
		using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
		{
			var workbookPart = doc.AddWorkbookPart();
			workbookPart.Workbook = new Workbook();
			var sheets = workbookPart.Workbook.AppendChild(new Sheets());

			var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
			worksheetPart.Worksheet = new Worksheet(
				new SheetDimension { Reference = "A1" },
				new SheetData(new Row { RowIndex = 1U }));

			sheets.Append(new Sheet
			{
				Id = workbookPart.GetIdOfPart(worksheetPart),
				SheetId = 1U,
				Name = "Data",
			});
		}

		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			worker.SetColumnWidth(0, 25);
		}

		stream.Position = 0;
		using var reopened = SpreadsheetDocument.Open(stream, false);
		var worksheet = reopened.WorkbookPart!.WorksheetParts.First().Worksheet;

		var dimensionIndex = IndexOf<SheetDimension>(worksheet);
		var colsIndex = IndexOf<Columns>(worksheet);

		dimensionIndex.AssertGreater(-1, "the template <dimension> should be preserved");
		colsIndex.AssertGreater(-1, "Columns should exist after SetColumnWidth");
		(dimensionIndex < colsIndex).AssertTrue("CT_Worksheet requires <dimension> before <cols>");

		AssertOpenXmlValid(stream);
	}

	/// <summary>
	/// Regression test for CT_Worksheet element order: calling MergeCells, SetConditionalFormatting,
	/// then SetHyperlink emits &lt;mergeCells&gt; &lt; &lt;conditionalFormatting&gt; &lt; &lt;hyperlinks&gt; in schema
	/// order and the worksheet passes validation. (Was: each element was inserted directly after
	/// SheetData, so the last call ended up first and the three appeared reversed,
	/// OpenXmlExcelWorkerProvider.cs:1588.)
	/// </summary>
	[TestMethod]
	public void OpenXml_MergeConditionalHyperlink_KeepValidElementOrder()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, 100) // A1
				.SetCell(0, 1, 50)  // A2
				.MergeCells(1, 0, 2, 0) // B1:C1
				.SetConditionalFormatting(0, ComparisonOperator.Greater, "75", "#00FF00", null)
				.SetHyperlink(3, 0, "https://stocksharp.com", "Link"); // D1
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var worksheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;

		var mergeIndex = IndexOf<MergeCells>(worksheet);
		var condIndex = IndexOf<ConditionalFormatting>(worksheet);
		var linkIndex = IndexOf<Hyperlinks>(worksheet);

		mergeIndex.AssertGreater(-1, "MergeCells should exist");
		condIndex.AssertGreater(-1, "ConditionalFormatting should exist");
		linkIndex.AssertGreater(-1, "Hyperlinks should exist");

		(mergeIndex < condIndex).AssertTrue("CT_Worksheet requires <mergeCells> before <conditionalFormatting>");
		(condIndex < linkIndex).AssertTrue("CT_Worksheet requires <conditionalFormatting> before <hyperlinks>");

		AssertOpenXmlValid(stream);
	}

	/// <summary>
	/// Regression test for Dispose on a non-writable target: Dispose skips the write-back when the
	/// target stream is not writable and completes without throwing. (Was: Dispose unconditionally
	/// called _targetStream.SetLength(0) and copied the workbook back, throwing NotSupportedException
	/// from SetLength on a non-writable stream, OpenXmlExcelWorkerProvider.cs:1849.)
	/// </summary>
	[TestMethod]
	public void OpenXml_Dispose_ReadOnlyTargetStream_DoesNotThrow()
	{
		var bytes = BuildSimpleWorkbookBytes();

		// A non-writable, seekable stream over the workbook bytes (e.g. a file opened FileAccess.Read).
		using var readOnly = new MemoryStream(bytes, writable: false);

		var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(readOnly);
		worker.GetCell<string>(0, 0).AssertEqual("Header");

		// Must not throw NotSupportedException when writing back is impossible.
		worker.Dispose();
	}

	/// <summary>
	/// Regression test for Dispose idempotency: a second Dispose is a harmless no-op, per the
	/// IDisposable contract. (Was: Dispose had no idempotency guard, so a second call invoked
	/// _doc.Save() on an already-disposed document and threw ObjectDisposedException,
	/// OpenXmlExcelWorkerProvider.cs:1841.)
	/// </summary>
	[TestMethod]
	public void OpenXml_Dispose_IsIdempotent()
	{
		using var stream = new MemoryStream();

		var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream);
		worker
			.AddSheet()
			.SetCell(0, 0, "X");

		worker.Dispose();

		// A second dispose must be a harmless no-op per the IDisposable contract.
		worker.Dispose();
	}

	/// <summary>
	/// Regression test for inserting a new row when existing rows lack a RowIndex: SetCell succeeds
	/// and writes the value even when the existing &lt;row&gt; omits the optional r attribute (allowed by
	/// ECMA-376, emitted by some streaming writers). (Was: the sorted-insert lookup dereferenced
	/// r.RowIndex.Value without a null guard and threw NullReferenceException,
	/// OpenXmlExcelWorkerProvider.cs:1999.)
	/// </summary>
	[TestMethod]
	public void OpenXml_OpenExist_SetCell_WhenExistingRowLacksRowIndex()
	{
		using var stream = new MemoryStream();

		// Build a template whose single existing <row> has NO r attribute (RowIndex omitted).
		using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
		{
			var workbookPart = doc.AddWorkbookPart();
			workbookPart.Workbook = new Workbook();
			var sheets = workbookPart.Workbook.AppendChild(new Sheets());

			var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
			var indexlessRow = new Row();
			indexlessRow.Append(new Cell
			{
				CellReference = "A1",
				DataType = CellValues.InlineString,
				InlineString = new InlineString(new Text("existing")),
			});
			worksheetPart.Worksheet = new Worksheet(new SheetData(indexlessRow));

			sheets.Append(new Sheet
			{
				Id = workbookPart.GetIdOfPart(worksheetPart),
				SheetId = 1U,
				Name = "Data",
			});
		}

		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			// Inserting a brand new row must not crash on the missing RowIndex of the existing row.
			worker.SetCell(0, 2, "new"); // A3
		}

		stream.Position = 0;
		using var reopened = SpreadsheetDocument.Open(stream, false);
		var added = reopened.WorkbookPart!.WorksheetParts.First()
			.Worksheet.Descendants<Cell>()
			.FirstOrDefault(c => c.CellReference?.Value == "A3");

		added.AssertNotNull("the new cell A3 should be written");
	}

	/// <summary>
	/// Regression test for custom number-format id uniqueness across cell numFmts and dxf numFmts:
	/// a dxf number format and a cell number format get distinct ids. (Was: the dxf path and the
	/// cell path computed the next id independently, so a dxf numFmt in DifferentialFormats was
	/// unseen by the cell path and both got the same id; now a single shared allocator seeded from
	/// both collections hands out unique ids, OpenXmlExcelWorkerProvider.cs:1418.)
	/// </summary>
	[TestMethod]
	public void OpenXml_DxfAndCellNumberFormatIds_AreUnique()
	{
		using var stream = new MemoryStream();
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.SetCell(0, 0, 0.25) // A1
				// dxf number format -> AddDifferentialFormat -> NextDxfNumberFormatId.
				.SetConditionalFormattingFormula(0, 0, 0, 0, "$A1>0", new ExcelConditionalFormat
				{
					NumberFormat = "0.00%",
				})
				// cell number format -> GetOrCreateNumberFormatId (independent counter).
				.SetStyle(0, "0.000");
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var stylesheet = doc.WorkbookPart!.WorkbookStylesPart!.Stylesheet;

		var cellNumFmtId = stylesheet.NumberingFormats!
			.Elements<NumberingFormat>()
			.First(nf => nf.FormatCode?.Value == "0.000")
			.NumberFormatId!.Value;

		var dxfNumFmtId = stylesheet.DifferentialFormats!
			.Elements<DifferentialFormat>()
			.Select(d => d.NumberingFormat)
			.First(nf => nf?.FormatCode?.Value == "0.00%")!
			.NumberFormatId!.Value;

		(cellNumFmtId != dxfNumFmtId).AssertTrue(
			$"custom number-format ids must be unique across cell numFmts and dxf numFmts (both were {cellNumFmtId})");
	}

	/// <summary>
	/// Regression test for unique drawing ids: reopening a template that already holds a chart and
	/// adding another chart assigns a fresh NonVisualDrawingProperties.Id, so every drawing id in the
	/// worksheet drawing is unique (required by drawingML). (Was: chart ids came from a per-worker
	/// counter starting at 0 that was never seeded from existing drawing content, so the new chart
	/// reused id 1; now NextDrawingId seeds the counter from the existing max,
	/// OpenXmlExcelWorkerProvider.cs:2098.)
	/// </summary>
	[TestMethod]
	public void OpenXml_AddChartToTemplateWithExistingChart_AssignsUniqueDrawingId()
	{
		using var stream = new MemoryStream();

		// Template already contains one chart (its GraphicFrame gets drawing Id 1).
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).CreateNew(stream))
		{
			worker
				.AddSheet()
				.RenameSheet("Data")
				.SetCell(0, 0, 1)
				.SetCell(1, 0, 10)
				.SetCell(0, 1, 2)
				.SetCell(1, 1, 20)
				.AddLineChart("First", "Data!$A$1:$B$2", 1, 2, 3, 0, 400, 300);
		}

		// Reopen and add a second chart - a fresh worker resets its chart-id counter.
		stream.Position = 0;
		using (var worker = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			worker.AddLineChart("Second", "Data!$A$1:$B$2", 1, 2, 10, 0, 400, 300);
		}

		stream.Position = 0;
		using var doc = SpreadsheetDocument.Open(stream, false);
		var drawing = doc.WorkbookPart!.WorksheetParts.First().DrawingsPart!.WorksheetDrawing;

		var ids = drawing.Descendants<Xdr.NonVisualDrawingProperties>()
			.Select(p => p.Id!.Value)
			.ToArray();

		(ids.Length >= 2).AssertTrue("two charts should produce two drawing ids");
		(ids.Length == ids.Distinct().Count()).AssertTrue(
			$"drawing ids must be unique within the worksheet drawing, got [{ids.Select(id => id.ToString()).JoinComma()}]");
	}

	#endregion
}
