namespace Ecng.Interop
{
	using System;
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
}