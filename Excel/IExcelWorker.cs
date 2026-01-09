namespace Ecng.Excel;

using System;
using System.Collections.Generic;
using System.IO;

using Ecng.Common;

/// <summary>
/// Defines a contract for working with Excel files, providing methods to manipulate cells, styles, sheets, and formatting.
/// </summary>
public interface IExcelWorker : IDisposable
{
	/// <summary>
	/// Sets the value of a cell at the specified column and row.
	/// </summary>
	/// <typeparam name="T">The type of the value to set.</typeparam>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="row">The row index (1-based).</param>
	/// <param name="value">The value to set in the cell.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetCell<T>(int col, int row, T value);

	/// <summary>
	/// Gets the value of a cell at the specified column and row.
	/// </summary>
	/// <typeparam name="T">The type of the value to retrieve.</typeparam>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="row">The row index (1-based).</param>
	/// <returns>The value of the cell cast to type <typeparamref name="T"/>.</returns>
	T GetCell<T>(int col, int row);

	/// <summary>
	/// Sets the style of a column based on a specified type.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="type">The <see cref="Type"/> that determines the style.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetStyle(int col, Type type);

	/// <summary>
	/// Sets the style of a column using a custom format string.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="format">The format string to apply to the column.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetStyle(int col, string format);

	/// <summary>
	/// Sets conditional formatting for a column based on a condition.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="op">The <see cref="ComparisonOperator"/> to use for the condition.</param>
	/// <param name="condition">The condition value as a string.</param>
	/// <param name="bgColor">The background color to apply if the condition is met (e.g., hex code or name).</param>
	/// <param name="fgColor">The foreground (text) color to apply if the condition is met (e.g., hex code or name).</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor);

	/// <summary>
	/// Renames the current sheet to the specified name.
	/// </summary>
	/// <param name="name">The new name for the sheet.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker RenameSheet(string name);

	/// <summary>
	/// Adds a new sheet to the workbook.
	/// </summary>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddSheet();

	/// <summary>
	/// Checks if a sheet with the specified name exists in the workbook.
	/// </summary>
	/// <param name="name">The name of the sheet to check.</param>
	/// <returns><c>true</c> if the sheet exists; otherwise, <c>false</c>.</returns>
	bool ContainsSheet(string name);

	/// <summary>
	/// Switches the active sheet to the one with the specified name.
	/// </summary>
	/// <param name="name">The name of the sheet to switch to.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SwitchSheet(string name);

	/// <summary>
	/// Gets the total number of columns in the current sheet.
	/// </summary>
	/// <returns>The number of columns.</returns>
	int GetColumnsCount();

	/// <summary>
	/// Gets the total number of rows in the current sheet.
	/// </summary>
	/// <returns>The number of rows.</returns>
	int GetRowsCount();

	/// <summary>
	/// Sets the width of a column.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="width">The width in characters.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetColumnWidth(int col, double width);

	/// <summary>
	/// Sets the height of a row.
	/// </summary>
	/// <param name="row">The row index (1-based).</param>
	/// <param name="height">The height in points.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetRowHeight(int row, double height);

	/// <summary>
	/// Auto-fits the column width based on content.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AutoFitColumn(int col);

	/// <summary>
	/// Freezes the specified number of top rows.
	/// </summary>
	/// <param name="count">The number of rows to freeze.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker FreezeRows(int count);

	/// <summary>
	/// Freezes the specified number of left columns.
	/// </summary>
	/// <param name="count">The number of columns to freeze.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker FreezeCols(int count);

	/// <summary>
	/// Merges cells in the specified range.
	/// </summary>
	/// <param name="startCol">The starting column index (1-based).</param>
	/// <param name="startRow">The starting row index (1-based).</param>
	/// <param name="endCol">The ending column index (1-based).</param>
	/// <param name="endRow">The ending row index (1-based).</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker MergeCells(int startCol, int startRow, int endCol, int endRow);

	/// <summary>
	/// Sets a hyperlink in a cell.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="row">The row index (1-based).</param>
	/// <param name="url">The URL for the hyperlink.</param>
	/// <param name="text">The display text for the hyperlink. If null, the URL is used.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetHyperlink(int col, int row, string url, string text = null);

	/// <summary>
	/// Sets the format of a specific cell.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="row">The row index (1-based).</param>
	/// <param name="format">The format string to apply.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetCellFormat(int col, int row, string format);

	/// <summary>
	/// Sets the colors of a specific cell.
	/// </summary>
	/// <param name="col">The column index (1-based).</param>
	/// <param name="row">The row index (1-based).</param>
	/// <param name="bgColor">The background color (e.g., hex code or name).</param>
	/// <param name="fgColor">The foreground (text) color. If null, default is used.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker SetCellColor(int col, int row, string bgColor, string fgColor = null);

	/// <summary>
	/// Gets the names of all sheets in the workbook.
	/// </summary>
	/// <returns>An enumerable of sheet names.</returns>
	IEnumerable<string> GetSheetNames();

	/// <summary>
	/// Deletes a sheet by name.
	/// </summary>
	/// <param name="name">The name of the sheet to delete.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker DeleteSheet(string name);

	/// <summary>
	/// Adds a line chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="xCol">Column index for X-axis values (1-based).</param>
	/// <param name="yCol">Column index for Y-axis values (1-based).</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddLineChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a bar/column chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddBarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a pie chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddPieChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds an area chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddAreaChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a doughnut chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddDoughnutChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a scatter (XY) chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="xCol">Column index for X-axis values (1-based).</param>
	/// <param name="yCol">Column index for Y-axis values (1-based).</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddScatterChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a radar (spider) chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:B100").</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddRadarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a bubble chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation (e.g., "A1:C100") with X, Y, and size columns.</param>
	/// <param name="xCol">Column index for X-axis values (1-based).</param>
	/// <param name="yCol">Column index for Y-axis values (1-based).</param>
	/// <param name="sizeCol">Column index for bubble size values (1-based).</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddBubbleChart(string name, string dataRange, int xCol, int yCol, int sizeCol, int anchorCol, int anchorRow, int width, int height);

	/// <summary>
	/// Adds a stock (OHLC) chart to the current sheet.
	/// </summary>
	/// <param name="name">Chart title.</param>
	/// <param name="dataRange">Data range in A1 notation with Open, High, Low, Close columns.</param>
	/// <param name="anchorCol">Column where chart is anchored (1-based).</param>
	/// <param name="anchorRow">Row where chart is anchored (1-based).</param>
	/// <param name="width">Chart width in pixels.</param>
	/// <param name="height">Chart height in pixels.</param>
	/// <returns>The current <see cref="IExcelWorker"/> instance for method chaining.</returns>
	IExcelWorker AddStockChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height);
}

/// <summary>
/// Defines a contract for creating and opening Excel worker instances from streams.
/// </summary>
public interface IExcelWorkerProvider
{
	/// <summary>
	/// Creates a new Excel workbook and returns an <see cref="IExcelWorker"/> instance to interact with it.
	/// </summary>
	/// <param name="stream">The stream to write the new workbook to.</param>
	/// <param name="readOnly">If <c>true</c>, the workbook is opened in read-only mode; otherwise, it is writable.</param>
	/// <returns>An <see cref="IExcelWorker"/> instance for the new workbook.</returns>
	IExcelWorker CreateNew(Stream stream, bool readOnly = false);

	/// <summary>
	/// Opens an existing Excel workbook from a stream and returns an <see cref="IExcelWorker"/> instance to interact with it.
	/// </summary>
	/// <param name="stream">The stream containing the existing workbook data.</param>
	/// <returns>An <see cref="IExcelWorker"/> instance for the opened workbook.</returns>
	IExcelWorker OpenExist(Stream stream);
}
