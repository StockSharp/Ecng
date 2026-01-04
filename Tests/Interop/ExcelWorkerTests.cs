namespace Ecng.Tests.Interop;

using Ecng.Interop;

[TestClass]
[TestCategory("Integration")]
public class ExcelWorkerTests : BaseTestClass
{
	private static IExcelWorkerProvider GetProvider() => new DevExpExcelWorkerProvider();

	[TestMethod]
	public void CreateNew_AddSheet_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.RenameSheet("TestSheet");

		worker.ContainsSheet("TestSheet").AssertTrue();
	}

	[TestMethod]
	public void SetCell_GetCell_StringValue_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, "Hello World");

		worker.GetCell<string>(1, 1).AssertEqual("Hello World");
	}

	[TestMethod]
	public void SetCell_GetCell_IntValue_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, 42);

		worker.GetCell<int>(1, 1).AssertEqual(42);
	}

	[TestMethod]
	public void SetCell_GetCell_DecimalValue_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, 123.45m);

		worker.GetCell<decimal>(1, 1).AssertEqual(123.45m);
	}

	[TestMethod]
	public void SetCell_GetCell_DateTimeValue_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		var dt = new DateTime(2024, 1, 15, 10, 30, 0);
		worker
			.AddSheet()
			.SetCell(1, 1, dt);

		worker.GetCell<DateTime>(1, 1).AssertEqual(dt);
	}

	[TestMethod]
	public void SetCell_GetCell_BoolValue_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, true);

		worker.GetCell<bool>(1, 1).AssertEqual(true);
	}

	[TestMethod]
	public void SetStyle_ColumnFormat_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetStyle(1, "yyyy-MM-dd")
			.SetCell(1, 1, DateTime.Now);

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	public void MultipleCells_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

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
	public void MultipleSheets_SwitchSheet_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

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
	public void SetColumnWidth_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetColumnWidth(1, 20.5)
			.SetCell(1, 1, "Test");

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	public void SetRowHeight_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetRowHeight(1, 30.0)
			.SetCell(1, 1, "Test");

		worker.GetRowsCount().AssertEqual(1);
	}

	[TestMethod]
	public void AutoFitColumn_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, "This is a long text that should auto-fit")
			.AutoFitColumn(1);

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	public void FreezeRows_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, "Header")
			.SetCell(1, 2, "Data")
			.FreezeRows(1);

		worker.GetRowsCount().AssertEqual(2);
	}

	[TestMethod]
	public void FreezeCols_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, "Label")
			.SetCell(2, 1, "Value")
			.FreezeCols(1);

		worker.GetColumnsCount().AssertEqual(2);
	}

	[TestMethod]
	public void MergeCells_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, "Merged Header")
			.MergeCells(1, 1, 3, 1);

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	public void SetHyperlink_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetHyperlink(1, 1, "https://stocksharp.com", "StockSharp");

		worker.GetColumnsCount().AssertEqual(1);
	}

	[TestMethod]
	public void SetCellFormat_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, 12345.6789)
			.SetCellFormat(1, 1, "#,##0.00");

		worker.GetCell<double>(1, 1).AssertEqual(12345.6789);
	}

	[TestMethod]
	public void SetCellColor_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, "Colored Cell")
			.SetCellColor(1, 1, "#FF0000", "#FFFFFF");

		worker.GetCell<string>(1, 1).AssertEqual("Colored Cell");
	}

	[TestMethod]
	public void GetSheetNames_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

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
	public void DeleteSheet_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

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

	[TestMethod]
	public void SetConditionalFormatting_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

		worker
			.AddSheet()
			.SetCell(1, 1, 100)
			.SetCell(1, 2, 50)
			.SetCell(1, 3, 25)
			.SetConditionalFormatting(1, ComparisonOperator.Greater, "75", "#00FF00", null);

		worker.GetRowsCount().AssertEqual(3);
	}

	[TestMethod]
	public void ComplexWorkflow_Success()
	{
		using var stream = new MemoryStream();
		using var worker = GetProvider().CreateNew(stream);

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
}
