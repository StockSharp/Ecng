namespace Ecng.Interop;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using DocumentFormat.OpenXml;

using Ecng.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

/// <summary>
/// The <see cref="IExcelWorkerProvider"/> implementation based on Open XML SDK.
/// It can open and modify an existing XLSX template (keeping styles, charts, conditional formatting),
/// and only inject new values into cells.
/// </summary>
/// <remarks>
/// Key notes:
/// <list type="bullet">
/// <item>Open XML SDK does not calculate formulas. To make formulas recalc when user opens the file in Excel,
/// this provider sets <c>fullCalcOnLoad</c> flag.</item>
/// <item>To keep number/date/time formatting for newly created rows, the provider captures <c>StyleIndex</c>
/// from a "sample" data row (by default row 3) and applies it to new cells in the same column.</item>
/// </list>
/// </remarks>
public sealed class OpenXmlExcelWorkerProvider : IExcelWorkerProvider
{
	/// <inheritdoc />
	public IExcelWorker CreateNew(Stream stream, bool readOnly) => new OpenXmlExcelWorker(stream, createNew: true);

	/// <inheritdoc />
	public IExcelWorker OpenExist(Stream stream) => new OpenXmlExcelWorker(stream, createNew: false);

	private sealed class OpenXmlExcelWorker : IExcelWorker
	{
		private readonly Stream _targetStream;
		private readonly MemoryStream _workStream;
		private readonly SpreadsheetDocument _doc;
		private readonly WorkbookPart _workbookPart;

		private WorksheetPart _currentWorksheetPart;
		private Sheet _currentSheet;

		// Column style map captured from a sample row (e.g. Trades/Orders data row).
		private Dictionary<int, uint> _columnStyleIndex = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenXmlExcelWorker"/>.
		/// </summary>
		/// <param name="stream">The target stream.</param>
		/// <param name="createNew">Create a new workbook if <see langword="true"/>; otherwise open existing XLSX from <paramref name="stream"/>.</param>
		public OpenXmlExcelWorker(Stream stream, bool createNew)
		{
			_targetStream = stream ?? throw new ArgumentNullException(nameof(stream));
			_workStream = new MemoryStream();

			if (createNew)
			{
				_doc = SpreadsheetDocument.Create(_workStream, SpreadsheetDocumentType.Workbook, autoSave: true);
				_workbookPart = _doc.AddWorkbookPart();
				_workbookPart.Workbook = new Workbook(new Sheets());

				// Ensure calc on open (useful if later template formulas are added).
				EnsureFullCalcOnLoad(_workbookPart.Workbook);

				// Do not create initial sheet - AddSheet() will create the first one.
			}
			else
			{
				// Copy existing XLSX into memory and open for editing.
				if (_targetStream.CanSeek)
					_targetStream.Position = 0;

				_targetStream.CopyTo(_workStream);
				_workStream.Position = 0;

				_doc = SpreadsheetDocument.Open(_workStream, isEditable: true);
				_workbookPart = _doc.WorkbookPart ?? throw new InvalidOperationException("WorkbookPart is missing.");

				// Ensure calc on open (Excel will recalc formulas when opening the file).
				EnsureFullCalcOnLoad(_workbookPart.Workbook);

				var firstSheet = _workbookPart.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault()
					?? throw new InvalidOperationException("Workbook contains no sheets.");

				_currentSheet = firstSheet;
				_currentWorksheetPart = (WorksheetPart)_workbookPart.GetPartById(firstSheet.Id!);

				CaptureColumnStyles();
			}
		}

		/// <inheritdoc />
		public bool ContainsSheet(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			return _workbookPart.Workbook.Sheets!.Elements<Sheet>()
				.Any(s => string.Equals(s.Name?.Value, name, StringComparison.OrdinalIgnoreCase));
		}

		/// <inheritdoc />
		public IExcelWorker AddSheet()
		{
			var sheets = _workbookPart.Workbook.Sheets ?? _workbookPart.Workbook.AppendChild(new Sheets());
			var nextId = sheets.Elements<Sheet>().Select(s => s.SheetId!.Value).DefaultIfEmpty(0u).Max() + 1;

			var wsPart = _workbookPart.AddNewPart<WorksheetPart>();
			wsPart.Worksheet = new Worksheet(new SheetData());

			var relId = _workbookPart.GetIdOfPart(wsPart);
			var sheet = new Sheet { Id = relId, SheetId = nextId, Name = $"Sheet{nextId}" };
			sheets.Append(sheet);

			_currentWorksheetPart = wsPart;
			_currentSheet = sheet;

			CaptureColumnStyles();
			return this;
		}

		/// <inheritdoc />
		public IExcelWorker RenameSheet(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_currentSheet.Name = name;
			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SwitchSheet(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			var sheet = _workbookPart.Workbook.Sheets!.Elements<Sheet>()
				.FirstOrDefault(s => string.Equals(s.Name?.Value, name, StringComparison.OrdinalIgnoreCase));

			if (sheet == null)
				throw new InvalidOperationException($"Sheet '{name}' not found.");

			_currentSheet = sheet;
			_currentWorksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id!);

			CaptureColumnStyles();
			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetCell<T>(int col, int row, T value)
		{
			SetCellValue(col, row, value);
			return this;
		}

		/// <inheritdoc />
		public T GetCell<T>(int col, int row)
		{
			var cell = GetCell(col, row, createIfMissing: false);
			if (cell == null)
				return default;

			string raw;

			// Handle InlineString
			if (cell.DataType?.Value == CellValues.InlineString)
			{
				raw = cell.InlineString?.Text?.Text;
			}
			else
			{
				raw = cell.CellValue?.Text;
			}

			if (raw == null)
				return default;

			// Handle Boolean conversion (Excel stores as "1" or "0")
			if (typeof(T) == typeof(bool))
			{
				return (T)(object)(raw == "1");
			}

			// Handle DateTime conversion (Excel stores as OADate double)
			if (typeof(T) == typeof(DateTime))
			{
				if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate))
					return (T)(object)DateTime.FromOADate(oaDate);
			}

			return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
		}

		/// <inheritdoc />
		public IExcelWorker SetStyle(int col, Type type) => this;

		/// <inheritdoc />
		public IExcelWorker SetStyle(int col, string format) => this;

		/// <inheritdoc />
		public IExcelWorker SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor) => this;

		/// <inheritdoc />
		public int GetColumnsCount() => 0;

		/// <inheritdoc />
		public int GetRowsCount() => 0;

		/// <inheritdoc />
		public IExcelWorker SetColumnWidth(int col, double width) => this;

		/// <inheritdoc />
		public IExcelWorker SetRowHeight(int row, double height) => this;

		/// <inheritdoc />
		public IExcelWorker AutoFitColumn(int col) => this;

		/// <inheritdoc />
		public IExcelWorker FreezeRows(int count) => this;

		/// <inheritdoc />
		public IExcelWorker FreezeCols(int count) => this;

		/// <inheritdoc />
		public IExcelWorker MergeCells(int startCol, int startRow, int endCol, int endRow) => this;

		/// <inheritdoc />
		public IExcelWorker SetHyperlink(int col, int row, string url, string text) => this;

		/// <inheritdoc />
		public IExcelWorker SetCellFormat(int col, int row, string format) => this;

		/// <inheritdoc />
		public IExcelWorker SetCellColor(int col, int row, string bgColor, string fgColor) => this;

		/// <inheritdoc />
		public IEnumerable<string> GetSheetNames()
			=> _workbookPart.Workbook.Sheets!.Elements<Sheet>().Select(s => s.Name?.Value).Where(n => !string.IsNullOrWhiteSpace(n));

		/// <inheritdoc />
		public IExcelWorker DeleteSheet(string name) => this;

		/// <inheritdoc />
		public void Dispose()
		{
			_doc.Save();
			_doc.Dispose();

			if (_targetStream.CanSeek)
				_targetStream.Position = 0;

			_targetStream.SetLength(0);
			_workStream.Position = 0;
			_workStream.CopyTo(_targetStream);

			if (_targetStream.CanSeek)
				_targetStream.Position = 0;

			_workStream.Dispose();
		}

		private static void EnsureFullCalcOnLoad(Workbook workbook)
		{
			var calcPr = workbook.CalculationProperties;
			if (calcPr == null)
			{
				calcPr = new CalculationProperties();
				workbook.CalculationProperties = calcPr;
			}

			calcPr.FullCalculationOnLoad = true;
		}

		private void CaptureColumnStyles()
		{
			_columnStyleIndex = new Dictionary<int, uint>();

			// Try to capture from row 3 (1-based) => index 2 (0-based):
			// it matches your generator data start rowIndex=2 (0-based).
			const int sampleRow0 = 2;

			var sheetData = _currentWorksheetPart.Worksheet.GetFirstChild<SheetData>();
			if (sheetData == null)
				return;

			var sampleRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == (uint)(sampleRow0 + 1));
			if (sampleRow == null)
				return;

			foreach (var cell in sampleRow.Elements<Cell>())
			{
				var (colIndex, _) = ParseCellReference(cell.CellReference?.Value);
				if (colIndex < 0)
					continue;

				if (cell.StyleIndex != null)
					_columnStyleIndex[colIndex] = cell.StyleIndex.Value;
			}
		}

		private void SetCellValue(int col, int row, object value)
		{
			var cell = GetCell(col, row, createIfMissing: true);

			// If this cell is newly created (no StyleIndex), apply column style from sample row.
			if (cell.StyleIndex == null && _columnStyleIndex.TryGetValue(col, out var styleIdx))
				cell.StyleIndex = styleIdx;

			cell.RemoveAllChildren<CellValue>();
			cell.InlineString = null;

			if (value == null)
			{
				cell.DataType = null;
				return;
			}

			switch (value)
			{
				case bool b:
					cell.DataType = CellValues.Boolean;
					cell.CellValue = new CellValue(b ? "1" : "0");
					return;

				case DateTime dt:
					// Excel stores DateTime as OADate (double). Formatting comes from cell StyleIndex in the template.
					cell.DataType = null;
					cell.CellValue = new CellValue(dt.ToOADate().ToString(CultureInfo.InvariantCulture));
					return;

				case DateTimeOffset dto:
					// Preserve local clock time (do NOT force UTC).
					var localDt = dto.LocalDateTime;
					cell.DataType = null;
					cell.CellValue = new CellValue(localDt.ToOADate().ToString(CultureInfo.InvariantCulture));
					return;
			}

			var type = value.GetType();

			// Keep IDs as text if you want to avoid any double precision issues in Excel itself.
			// (Excel also shows only ~15 significant digits for numbers.)
			if (value is long l && (Math.Abs(l) >= 9_000_000_000_000_00L))
			{
				WriteInlineString(cell, l.ToString(CultureInfo.InvariantCulture));
				return;
			}

			if (type.IsEnum)
			{
				WriteInlineString(cell, value.ToString());
				return;
			}

			if (IsNumeric(type))
			{
				cell.DataType = null;
				cell.CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture));
				return;
			}

			WriteInlineString(cell, Convert.ToString(value, CultureInfo.InvariantCulture));
		}

		private static void WriteInlineString(Cell cell, string text)
		{
			cell.DataType = CellValues.InlineString;
			cell.InlineString = new InlineString(new Text(text ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve });
		}

		private Cell GetCell(int col, int row, bool createIfMissing)
		{
			var sheetData = _currentWorksheetPart.Worksheet.GetFirstChild<SheetData>()
				?? _currentWorksheetPart.Worksheet.AppendChild(new SheetData());

			var rowIndex = (uint)(row + 1);
			var rowRef = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);

			if (rowRef == null)
			{
				if (!createIfMissing)
					return null;

				rowRef = new Row { RowIndex = rowIndex };

				// Keep rows sorted
				var nextRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex.Value > rowIndex);
				if (nextRow != null)
					sheetData.InsertBefore(rowRef, nextRow);
				else
					sheetData.Append(rowRef);
			}

			var cellRef = ToCellReference(col, row);
			var cell = rowRef.Elements<Cell>().FirstOrDefault(c => string.Equals(c.CellReference?.Value, cellRef, StringComparison.OrdinalIgnoreCase));

			if (cell == null)
			{
				if (!createIfMissing)
					return null;

				cell = new Cell { CellReference = cellRef };

				// Keep cells sorted within the row
				var nextCell = rowRef.Elements<Cell>()
					.FirstOrDefault(c => string.Compare(c.CellReference?.Value, cellRef, StringComparison.OrdinalIgnoreCase) > 0);

				if (nextCell != null)
					rowRef.InsertBefore(cell, nextCell);
				else
					rowRef.Append(cell);
			}

			return cell;
		}

		private static string ToCellReference(int col, int row)
			=> $"{ToColumnName(col)}{row + 1}";

		private static string ToColumnName(int zeroBasedIndex)
		{
			var dividend = zeroBasedIndex + 1;
			var columnName = string.Empty;

			while (dividend > 0)
			{
				var modulo = (dividend - 1) % 26;
				columnName = Convert.ToChar('A' + modulo) + columnName;
				dividend = (dividend - modulo) / 26;
				dividend--;
			}

			return columnName;
		}

		private static (int col, int row) ParseCellReference(string cellRef)
		{
			if (string.IsNullOrWhiteSpace(cellRef))
				return (-1, -1);

			int i = 0;
			while (i < cellRef.Length && char.IsLetter(cellRef[i]))
				i++;

			if (i == 0 || i == cellRef.Length)
				return (-1, -1);

			var colPart = cellRef.Substring(0, i).ToUpperInvariant();
			var rowPart = cellRef.Substring(i);

			if (!uint.TryParse(rowPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var row1))
				return (-1, -1);

			int col0 = 0;
			for (int k = 0; k < colPart.Length; k++)
			{
				col0 *= 26;
				col0 += (colPart[k] - 'A' + 1);
			}
			col0 -= 1;

			return (col0, (int)row1 - 1);
		}

		private static bool IsNumeric(Type t)
		{
			return t == typeof(byte) || t == typeof(sbyte)
				|| t == typeof(short) || t == typeof(ushort)
				|| t == typeof(int) || t == typeof(uint)
				|| t == typeof(long) || t == typeof(ulong)
				|| t == typeof(float) || t == typeof(double)
				|| t == typeof(decimal);
		}
	}
}
