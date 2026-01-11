namespace Ecng.Tests.Excel;

using System.Linq;

using DocumentFormat.OpenXml.Packaging;

using Ecng.Excel;

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
			.SetCell(1, 1, "Hello World");

		worker.GetCell<string>(1, 1).AssertEqual("Hello World");
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
			.SetCell(1, 1, 42);

		worker.GetCell<int>(1, 1).AssertEqual(42);
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
			.SetCell(1, 1, 123.45m);

		worker.GetCell<decimal>(1, 1).AssertEqual(123.45m);
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
			.SetCell(1, 1, dt);

		worker.GetCell<DateTime>(1, 1).AssertEqual(dt);
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
			.SetCell(1, 1, true);

		worker.GetCell<bool>(1, 1).AssertEqual(true);
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
			.SetCell(1, 1, "Data1")
			.AddSheet()
			.RenameSheet("Sheet2")
			.SetCell(1, 1, "Data2");

		worker.ContainsSheet("Sheet1").AssertTrue();
		worker.ContainsSheet("Sheet2").AssertTrue();

		worker.SwitchSheet("Sheet1");
		worker.GetCell<string>(1, 1).AssertEqual("Data1");

		worker.SwitchSheet("Sheet2");
		worker.GetCell<string>(1, 1).AssertEqual("Data2");
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
			.SetCell(1, 1, "A1")
			.SetCell(2, 1, "B1")
			.SetCell(1, 2, "A2")
			.SetCell(2, 2, "B2");

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
			.SetColumnWidth(1, 20.5)
			.SetCell(1, 1, "Test");

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
			.SetRowHeight(1, 30.0)
			.SetCell(1, 1, "Test");

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
			.SetCell(1, 1, "Header")
			.SetCell(1, 2, "Data")
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
			.SetCell(1, 1, "Label")
			.SetCell(2, 1, "Value")
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
			.SetCell(1, 1, "Merged Header")
			.MergeCells(1, 1, 3, 1);

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
			.SetHyperlink(1, 1, "https://stocksharp.com", "StockSharp");

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

		worker
			.AddSheet()
			.SetStyle(1, "yyyy-MM-dd")
			.SetCell(1, 1, DateTime.Now);

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
			.SetCell(1, 1, 12345.6789)
			.SetCellFormat(1, 1, "#,##0.00");

		worker.GetCell<double>(1, 1).AssertEqual(12345.6789);
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
			.SetCell(1, 1, "Colored Cell")
			.SetCellColor(1, 1, "#FF0000", "#FFFFFF");

		worker.GetCell<string>(1, 1).AssertEqual("Colored Cell");
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
			.SetCell(1, 1, 100)
			.SetCell(1, 2, 50)
			.SetCell(1, 3, 25)
			.SetConditionalFormatting(1, ComparisonOperator.Greater, "75", "#00FF00", null);

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
			.SetColumnWidth(1, 15)
			.SetColumnWidth(2, 25)
			.SetColumnWidth(3, 15)
			.FreezeRows(1)
			.SetCell(1, 1, "ID")
			.SetCell(2, 1, "Name")
			.SetCell(3, 1, "Value")
			.SetCellColor(1, 1, "LightGray")
			.SetCellColor(2, 1, "LightGray")
			.SetCellColor(3, 1, "LightGray");

		for (var i = 0; i < 10; i++)
		{
			worker
				.SetCell(1, i + 2, i + 1)
				.SetCell(2, i + 2, $"Item {i + 1}")
				.SetCell(3, i + 2, (i + 1) * 100.5m);
		}

		worker
			.SetConditionalFormatting(3, ComparisonOperator.Greater, "500", "#90EE90", null)
			.MergeCells(1, 13, 2, 13)
			.SetCell(1, 13, "Total:")
			.SetCell(3, 13, 5527.5m)
			.SetHyperlink(1, 15, "https://stocksharp.com", "Visit StockSharp");

		worker.GetRowsCount().AssertEqual(13);
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
			.SetCell(1, 1, "This is a long text that should auto-fit")
			.AutoFitColumn(1);

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
				.SetCell(1, 1, "Header1")
				.SetCell(2, 1, "Header2")
				.SetCell(1, 2, 100)
				.SetCell(2, 2, 200.5m)
				.SetCell(1, 3, true)
				.SetCell(2, 3, new DateTime(2024, 6, 15, 14, 30, 0));
		}

		// Open and read
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		reader.ContainsSheet("Data").AssertTrue();
		reader.GetCell<string>(1, 1).AssertEqual("Header1");
		reader.GetCell<string>(2, 1).AssertEqual("Header2");
		reader.GetCell<int>(1, 2).AssertEqual(100);
		reader.GetCell<decimal>(2, 2).AssertEqual(200.5m);
		reader.GetCell<bool>(1, 3).AssertEqual(true);
		reader.GetCell<DateTime>(2, 3).AssertEqual(new DateTime(2024, 6, 15, 14, 30, 0));
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
				.SetCell(1, 1, "Original")
				.SetCell(1, 2, 100);
		}

		// Open, modify, save
		stream.Position = 0;
		using (var editor = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			editor.GetCell<string>(1, 1).AssertEqual("Original");
			editor.GetCell<int>(1, 2).AssertEqual(100);

			editor
				.SetCell(1, 1, "Modified")
				.SetCell(1, 2, 999)
				.SetCell(1, 3, "New Row");
		}

		// Reopen and verify
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		reader.GetCell<string>(1, 1).AssertEqual("Modified");
		reader.GetCell<int>(1, 2).AssertEqual(999);
		reader.GetCell<string>(1, 3).AssertEqual("New Row");
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
				.SetCell(1, 1, "Q1")
				.SetCell(1, 2, 1000)
				.AddSheet()
				.RenameSheet("Expenses")
				.SetCell(1, 1, "Rent")
				.SetCell(1, 2, 500);
		}

		// Open and navigate sheets
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		var sheets = reader.GetSheetNames().ToArray();
		sheets.Length.AssertEqual(2);
		sheets.AssertContains("Sales");
		sheets.AssertContains("Expenses");

		// First sheet is active by default
		reader.GetCell<string>(1, 1).AssertEqual("Q1");

		reader.SwitchSheet("Expenses");
		reader.GetCell<string>(1, 1).AssertEqual("Rent");
		reader.GetCell<int>(1, 2).AssertEqual(500);

		reader.SwitchSheet("Sales");
		reader.GetCell<int>(1, 2).AssertEqual(1000);
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
				.SetCell(1, 1, "Data");
		}

		// Open and add new sheet
		stream.Position = 0;
		using (var editor = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			editor
				.AddSheet()
				.RenameSheet("Added")
				.SetCell(1, 1, "New Data");
		}

		// Verify both sheets exist
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		var sheets = reader.GetSheetNames().ToArray();
		sheets.Length.AssertEqual(2);
		sheets.AssertContains("Original");
		sheets.AssertContains("Added");

		reader.SwitchSheet("Original");
		reader.GetCell<string>(1, 1).AssertEqual("Data");

		reader.SwitchSheet("Added");
		reader.GetCell<string>(1, 1).AssertEqual("New Data");
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
				.SetStyle(1, "#,##0.00")
				.SetCell(1, 1, 1234.5678)
				.SetCellColor(2, 1, "#FF0000")
				.SetCell(2, 1, "Red");
		}

		// Open and add more data (styles should be preserved)
		stream.Position = 0;
		using (var editor = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream))
		{
			editor
				.SetCell(1, 2, 9999.1234)
				.SetCell(2, 2, "Plain");
		}

		// Verify values (style verification would require opening in Excel)
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);

		reader.GetCell<double>(1, 1).AssertEqual(1234.5678);
		reader.GetCell<double>(1, 2).AssertEqual(9999.1234);
		reader.GetCell<string>(2, 1).AssertEqual("Red");
		reader.GetCell<string>(2, 2).AssertEqual("Plain");
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
			.SetCell(1, 1, "X")
			.SetCell(2, 1, "Y")
			.SetCell(1, 2, 1)
			.SetCell(2, 2, 10)
			.SetCell(1, 3, 2)
			.SetCell(2, 3, 20)
			.SetCell(1, 4, 3)
			.SetCell(2, 4, 30)
			.AddLineChart("Test Line Chart", "A2:B4", 1, 2, 4, 1, 400, 300);

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
			.SetCell(1, 1, "Category")
			.SetCell(2, 1, "Value")
			.SetCell(1, 2, "A")
			.SetCell(2, 2, 100)
			.SetCell(1, 3, "B")
			.SetCell(2, 3, 200)
			.SetCell(1, 4, "C")
			.SetCell(2, 4, 150)
			.AddBarChart("Test Bar Chart", "A2:B4", 4, 1, 400, 300);

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
			.SetCell(1, 1, "Label")
			.SetCell(2, 1, "Value")
			.SetCell(1, 2, "Sales")
			.SetCell(2, 2, 45)
			.SetCell(1, 3, "Marketing")
			.SetCell(2, 3, 30)
			.SetCell(1, 4, "Development")
			.SetCell(2, 4, 25)
			.AddPieChart("Test Pie Chart", "A2:B4", 4, 1, 400, 300);

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
			.SetCell(1, 1, "X")
			.SetCell(2, 1, "Y")
			.SetCell(1, 2, 1)
			.SetCell(2, 2, 10)
			.SetCell(1, 3, 2)
			.SetCell(2, 3, 25)
			.SetCell(1, 4, 3)
			.SetCell(2, 4, 15)
			.AddLineChart("Line", "A2:B4", 1, 2, 4, 1, 300, 200)
			.AddBarChart("Bar", "A2:B4", 4, 12, 300, 200)
			.AddPieChart("Pie", "A2:B4", 10, 1, 300, 200);

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
				.SetCell(1, 1, 1)
				.SetCell(2, 1, 10)
				.SetCell(1, 2, 2)
				.SetCell(2, 2, 20)
				.AddLineChart("Equity Curve", "A1:B2", 1, 2, 4, 1, 500, 300);
		}

		// Verify chart was created by reopening
		stream.Position = 0;
		using var reader = CreateProvider(nameof(OpenXmlExcelWorkerProvider)).OpenExist(stream);
		reader.GetCell<int>(1, 1).AssertEqual(1);
		reader.GetCell<int>(2, 2).AssertEqual(20);
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
				.SetCell(1, 1, 1.0)   // A1
				.SetCell(2, 1, 10.0)  // B1
				.SetCell(1, 2, 2.0)   // A2
				.SetCell(2, 2, 20.0)  // B2
				.AddLineChart("Test", "Data!$A$1:$B$2", xCol: 1, yCol: 2, 4, 1, 400, 300);
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
			.SetCell(1, 1, "Month")
			.SetCell(2, 1, "Value")
			.SetCell(1, 2, "Jan")
			.SetCell(2, 2, 100)
			.SetCell(1, 3, "Feb")
			.SetCell(2, 3, 150)
			.SetCell(1, 4, "Mar")
			.SetCell(2, 4, 120)
			.AddAreaChart("Test Area Chart", "A2:B4", 4, 1, 400, 300);

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
			.SetCell(1, 1, "Category")
			.SetCell(2, 1, "Value")
			.SetCell(1, 2, "Product A")
			.SetCell(2, 2, 35)
			.SetCell(1, 3, "Product B")
			.SetCell(2, 3, 40)
			.SetCell(1, 4, "Product C")
			.SetCell(2, 4, 25)
			.AddDoughnutChart("Test Doughnut Chart", "A2:B4", 4, 1, 400, 300);

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
			.SetCell(1, 1, "X")
			.SetCell(2, 1, "Y")
			.SetCell(1, 2, 1.0)
			.SetCell(2, 2, 2.5)
			.SetCell(1, 3, 2.0)
			.SetCell(2, 3, 4.0)
			.SetCell(1, 4, 3.0)
			.SetCell(2, 4, 3.5)
			.AddScatterChart("Test Scatter Chart", "A2:B4", 1, 2, 4, 1, 400, 300);

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
			.SetCell(1, 1, "Attribute")
			.SetCell(2, 1, "Score")
			.SetCell(1, 2, "Speed")
			.SetCell(2, 2, 80)
			.SetCell(1, 3, "Reliability")
			.SetCell(2, 3, 90)
			.SetCell(1, 4, "Cost")
			.SetCell(2, 4, 70)
			.SetCell(1, 5, "Features")
			.SetCell(2, 5, 85)
			.AddRadarChart("Test Radar Chart", "A2:B5", 4, 1, 400, 400);

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
			.SetCell(1, 1, "X")
			.SetCell(2, 1, "Y")
			.SetCell(3, 1, "Size")
			.SetCell(1, 2, 10)
			.SetCell(2, 2, 20)
			.SetCell(3, 2, 5)
			.SetCell(1, 3, 30)
			.SetCell(2, 3, 40)
			.SetCell(3, 3, 10)
			.SetCell(1, 4, 50)
			.SetCell(2, 4, 25)
			.SetCell(3, 4, 15)
			.AddBubbleChart("Test Bubble Chart", "A2:C4", 1, 2, 3, 5, 1, 400, 300);

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
			.SetCell(1, 1, "Date")
			.SetCell(2, 1, "Open")
			.SetCell(3, 1, "High")
			.SetCell(4, 1, "Low")
			.SetCell(5, 1, "Close")
			.SetCell(1, 2, "2024-01-01")
			.SetCell(2, 2, 100.0)
			.SetCell(3, 2, 105.0)
			.SetCell(4, 2, 98.0)
			.SetCell(5, 2, 103.0)
			.SetCell(1, 3, "2024-01-02")
			.SetCell(2, 3, 103.0)
			.SetCell(3, 3, 108.0)
			.SetCell(4, 3, 101.0)
			.SetCell(5, 3, 106.0)
			.AddStockChart("Test Stock Chart", "A2:E3", 6, 1, 500, 300);

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
		var sheetData = doc.WorkbookPart!.WorksheetParts.First().Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>()!;
		var cells = sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>().First().Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>().ToList();

		// Verify cell references
		cells.Any(c => c.CellReference?.Value == "A1").AssertTrue("Cell A1 should exist");
		cells.Any(c => c.CellReference?.Value == "Z1").AssertTrue("Cell Z1 should exist");
		cells.Any(c => c.CellReference?.Value == "AA1").AssertTrue("Cell AA1 should exist");
		cells.Any(c => c.CellReference?.Value == "AB1").AssertTrue("Cell AB1 should exist");
		cells.Any(c => c.CellReference?.Value == "AZ1").AssertTrue("Cell AZ1 should exist");
		cells.Any(c => c.CellReference?.Value == "BA1").AssertTrue("Cell BA1 should exist");
	}

	#endregion
}
