namespace Ecng.Tests.Interop;

using Ecng.Interop;
using Ecng.Interop.Excel;

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
}
