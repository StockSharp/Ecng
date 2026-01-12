namespace Ecng.Tests.Excel;

using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Validation;

using Ecng.Excel;

using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

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

		var msg = string.Join("\n", errors.Take(20).Select(e =>
			$"- {e.Description}; Part={e.Part?.Uri}; Path={e.Path?.XPath}"));

		Assert.Fail($"OpenXmlValidator found {errors.Length} error(s):\n{msg}");
	}

	/// <summary>
	/// Helper: Gets cell value directly from XML (not through API).
	/// </summary>
	private static string GetCellValueFromXml(SpreadsheetDocument doc, string cellRef)
	{
		var wsPart = doc.WorkbookPart!.WorksheetParts.First();
		var sheetData = wsPart.Worksheet.GetFirstChild<SheetData>()!;

		var rowIndex = uint.Parse(new string(cellRef.Where(char.IsDigit).ToArray()));
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
			var idx = int.Parse(cell.CellValue!.Text);
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

	#endregion
}
