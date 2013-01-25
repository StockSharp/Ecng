namespace Ecng.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Linq;

	using NPOI.HSSF.Record;
	using NPOI.SS.Util;

	using Color = System.Windows.Media.Color;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	using NPOI.HSSF.UserModel;
	using NPOI.HSSF.Util;
	using NPOI.SS.UserModel;

	public class ExcelWorker : Disposable
	{
		private static readonly SynchronizedPairSet<Color, short> _colors = new SynchronizedPairSet<Color, short>();

		static ExcelWorker()
		{
			var colors = typeof(HSSFColor).GetValue<VoidType, HSSFColor[]>("GetAllColors", null);

			foreach (var color in colors)
			{
				var rgb = color.GetTriplet();
				_colors.Add(Color.FromRgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]), color.GetIndex());
			}
		}

		private ISheet _currentSheet;
		private string _fileName;
		private readonly Dictionary<int, HashSet<int>> _indecies = new Dictionary<int, HashSet<int>>();
		private readonly Dictionary<string, ICellStyle> _cellStyleCache = new Dictionary<string, ICellStyle>();

		/// <summary>
		/// Create <see cref="ExcelWorker"/>.
		/// </summary>
		public ExcelWorker()
		{
			Workbook = new HSSFWorkbook();
			_currentSheet = Workbook.CreateSheet();
		}

		/// <summary>
		/// Create <see cref="ExcelWorker"/>.
		/// </summary>
		/// <param name="name">Name of workbook.</param>
		/// <param name="readOnly"></param>
		public ExcelWorker(string name, bool readOnly = false)
		{
			if (name.IsEmpty())
				throw new ArgumentOutOfRangeException("name");

			Workbook = new HSSFWorkbook(new FileStream(name, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite));
		}

		[CLSCompliant(false)]
		public IWorkbook Workbook { get; private set; }

		/// <summary>
		/// Name of current sheet. Null, if current sheet doesn't exist.
		/// </summary>
		public string CurrentSheetName
		{
			get
			{
				return _currentSheet != null ? _currentSheet.SheetName : string.Empty;
			}
		}

		/// <summary>
		/// Create worksheet with specified name.
		/// </summary>
		/// <param name="sheetName">New name of worksheet. Must be unique.</param>
		/// <returns>Exporter.</returns>
		public ExcelWorker AddSheet(string sheetName)
		{
			if (!ContainsSheet(sheetName))
			{
				var sheet = Workbook.CreateSheet(sheetName);

				if (_currentSheet == null)
					_currentSheet = sheet;
			}

			return this;
		}

		/// <summary>
		/// Removes current worksheet.
		/// </summary>
		/// <returns>Exporter.</returns>
		public ExcelWorker RemoveSheet()
		{
			ThrowIfCurrentSheetIsEmpty();

			Workbook.RemoveSheetAt(Workbook.GetSheetIndex(_currentSheet));
			_currentSheet = null;

			return this;
		}

		/// <summary>
		/// Removes specified worksheet.
		/// </summary>
		/// <param name="sheetName">Name of the sheet to remove.</param>
		/// <returns>Exporter.</returns>
		public ExcelWorker RemoveSheet(string sheetName)
		{
			if (ContainsSheet(sheetName))
			{
				Workbook.RemoveSheetAt(Workbook.GetSheetIndex(sheetName));

				if (_currentSheet != null && _currentSheet.SheetName == sheetName)
					_currentSheet = null;
			}

			return this;
		}

		/// <summary>
		/// Set name of the tab sheet.
		/// </summary>
		/// <param name="sheetName">New name of the sheet.</param>
		/// <returns></returns>
		public ExcelWorker RenameSheet(string sheetName)
		{
			if (sheetName.IsEmpty())
				throw new ArgumentNullException("sheetName");

			ThrowIfCurrentSheetIsEmpty();

			Workbook.SetSheetName(Workbook.GetSheetIndex(_currentSheet), sheetName);
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sheetName"></param>
		/// <returns></returns>
		public bool ContainsSheet(string sheetName)
		{
			if (sheetName.IsEmpty())
				throw new ArgumentNullException("sheetName");

			return Workbook.GetSheetIndex(sheetName) != -1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sheetName"></param>
		/// <returns></returns>
		public ExcelWorker SwitchSheet(string sheetName)
		{
			if (ContainsSheet(sheetName))
				_currentSheet = Workbook.GetSheet(sheetName);

			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public ExcelWorker SetWidth(int col, int width)
		{
			ThrowIfCurrentSheetIsEmpty();
			_currentSheet.SetColumnWidth(col, width);
			return this;
		}

		public ExcelWorker SetHeight(int row, short height)
		{
			ThrowIfCurrentSheetIsEmpty();
			_currentSheet.GetRow(row).Height = height;
			return this;
		}

		public ExcelWorker SetConditionalFormatting(int col, Ecng.ComponentModel.ComparisonOperator comparison, string formula1, Color? bgColor, Color? fgColor)
		{
			return SetConditionalFormatting(col, 0, col, ushort.MaxValue, comparison, formula1, formula1, bgColor, fgColor);
		}

		public ExcelWorker SetConditionalFormatting(int colStart, int rowStart, int colEnd, int rowEnd, Ecng.ComponentModel.ComparisonOperator comparison, string formula1, string formula2, Color? bgColor, Color? fgColor)
		{
			var hscf = ((HSSFSheet)_currentSheet).SheetConditionalFormatting;

			var rule = hscf.CreateConditionalFormattingRule(ToExcelOperator(comparison), formula1, formula2);

			if (bgColor != null)
				rule.CreatePatternFormatting().FillBackgroundColor = _colors[(Color)bgColor];

			if (fgColor != null)
				rule.CreateFontFormatting().FontColorIndex = _colors[(Color)fgColor];

			hscf.AddConditionalFormatting(new[] { new CellRangeAddress(rowStart, rowEnd, colStart, colEnd) }, new[] { rule });

			return this;
		}

		private static ComparisonOperator ToExcelOperator(Ecng.ComponentModel.ComparisonOperator comparison)
		{
			switch (comparison)
			{
				case ComponentModel.ComparisonOperator.Equal:
					return ComparisonOperator.EQUAL;
				case ComponentModel.ComparisonOperator.NotEqual:
					return ComparisonOperator.NOT_EQUAL;
				case ComponentModel.ComparisonOperator.Greater:
					return ComparisonOperator.GT;
				case ComponentModel.ComparisonOperator.GreaterOrEqual:
					return ComparisonOperator.GE;
				case ComponentModel.ComparisonOperator.Less:
					return ComparisonOperator.LT;
				case ComponentModel.ComparisonOperator.LessOrEqual:
					return ComparisonOperator.LE;
				case ComponentModel.ComparisonOperator.Any:
					return ComparisonOperator.NO_COMPARISON;
				default:
					throw new ArgumentOutOfRangeException("comparison");
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public ExcelWorker SetBackColor(int col, int row, Color color)
		{
			var cell = InternalGetCell(col, row);
			var style = Workbook.CreateCellStyle();
			style.FillBackgroundColor = _colors[color];
			cell.CellStyle = style;
			return this;
		}

		public Color GetBackColor(int col, int row)
		{
			return _colors[InternalGetCell(col, row).CellStyle.FillBackgroundColor];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Number of rows</returns>
		public int GetRowsCount()
		{
			ThrowIfCurrentSheetIsEmpty();
			return _currentSheet.PhysicalNumberOfRows;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Number of columns</returns>
		public int GetColumnsCount()
		{
			if (GetRowsCount() > 0)
				return _currentSheet.GetRow(0).PhysicalNumberOfCells;

			return 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public ExcelWorker SetForeColor(int col, int row, Color color)
		{
			var cell = InternalGetCell(col, row);
			var style = Workbook.CreateCellStyle();
			var font = Workbook.CreateFont();
			font.Color = _colors[color];
			style.SetFont(font);
			cell.CellStyle = style;
			return this;
		}

		public Color GetForeColor(int col, int row)
		{
			return _colors[InternalGetCell(col, row).CellStyle.GetFont(Workbook).Color];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		public ExcelWorker SetFont(int col, int row, Font font)
		{
			if (font == null)
				throw new ArgumentNullException("font");

			var cellFont = InternalGetCell(col, row).CellStyle.GetFont(Workbook);
			
			//cellFont.Color = font;
			cellFont.IsItalic = font.Italic;
			cellFont.FontName = font.Name;
			cellFont.FontHeight = (short)font.Size;

			return this;
		}

		public ExcelWorker SetStyle(int col, Type dataType)
		{
			return SetStyle(col, GetDefaultDataFormat(dataType));
		}

		public ExcelWorker SetStyle(int col, string dataFormat)
		{
			if (!_cellStyleCache.ContainsKey(dataFormat))
			{
				var style = _currentSheet.Workbook.CreateCellStyle();

				// check if this is a built-in format
				var builtinFormatId = HSSFDataFormat.GetBuiltinFormat(dataFormat);

				if (builtinFormatId != -1)
				{
					style.DataFormat = builtinFormatId;
				}
				else
				{
					// not a built-in format, so create a new one
					var newDataFormat = _currentSheet.Workbook.CreateDataFormat();
					style.DataFormat = newDataFormat.GetFormat(dataFormat);
				}

				_cellStyleCache[dataFormat] = style;
			}

			_currentSheet.SetDefaultColumnStyle(col, _cellStyleCache[dataFormat]);
			return this;
		}

		private static string GetDefaultDataFormat(Type dataType)
		{
			if (dataType == null)
			{
				return "General";
			}
			else
			{
				switch (Type.GetTypeCode(dataType))
				{
					case TypeCode.Empty:
					case TypeCode.Object:
					case TypeCode.DBNull:
					case TypeCode.String:
					case TypeCode.Char:
						return "text";
					case TypeCode.Boolean:
						return "[=0]\"Yes\";[=1]\"No\"";
					case TypeCode.SByte:
					case TypeCode.Byte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
						return "0";
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.Decimal:
						return "0.00";
					case TypeCode.DateTime:
						return "yyyy.MM.dd HH:mm";
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public ExcelWorker SetCell(int col, int row, object value)
		{
			var cell = InternalGetCell(col, row);

			if (value == null)
			{
				cell.SetCellValue(string.Empty);
				return this;
			}

			if (value is DateTime)
				cell.SetCellValue((DateTime)value);
			else if (value is TimeSpan)
				cell.SetCellValue(DateTime.Today + (TimeSpan)value);
			else if (value is string)
				cell.SetCellValue((string)value);
			else if (value is bool)
				cell.SetCellValue((bool)value);
			else if (value is IRichTextString)
				cell.SetCellValue((IRichTextString)value);
			else if (value is double || value is int)
				cell.SetCellValue(value.To<double>());
			else
				cell.SetCellValue(value.ToString());

			return this;
		}

		public ExcelWorker SetFormula(int col, int row, string formula)
		{
			InternalGetCell(col, row).CellFormula = formula;
			return this;
		}

		//public T GetCell<T>(int col, int row)
		//{
		//    return GetCell(col, row).To<T>();
		//}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		public T GetCell<T>(int col, int row)
		{
			var cell = InternalGetCell(col, row, false);

			if (
					typeof(T) == typeof(int) ||
					typeof(T) == typeof(long) ||
					typeof(T) == typeof(uint) ||
					typeof(T) == typeof(ulong) ||
					typeof(T) == typeof(short) ||
					typeof(T) == typeof(ushort) ||
					typeof(T) == typeof(double) ||
					typeof(T) == typeof(float) ||
					typeof(T) == typeof(decimal)
				)
			{
				return cell.NumericCellValue.To<T>();
			}
			else if (typeof(T) == typeof(DateTime))
				return cell.DateCellValue.To<T>();
			else if (typeof(T) == typeof(TimeSpan))
				return cell.DateCellValue.TimeOfDay.To<T>();
			else if (typeof(T) == typeof(string))
				return cell.StringCellValue.To<T>();
			else if (typeof(T) == typeof(bool))
				return cell.BooleanCellValue.To<T>();
			else
			{
				switch (cell.CellType)
				{
					case CellType.Unknown:
						return cell.RichStringCellValue.To<T>();
					case CellType.NUMERIC:
						return cell.NumericCellValue.To<T>();
					case CellType.STRING:
						return cell.StringCellValue.To<T>();
					case CellType.FORMULA:
						return cell.CellFormula.To<T>();
					case CellType.BLANK:
						return default(T);
					case CellType.BOOLEAN:
						return cell.BooleanCellValue.To<T>();
					case CellType.ERROR:
						return cell.ErrorCellValue.To<T>();
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private ICell InternalGetCell(int colInd, int rowInd, bool createIfNotExists = true)
		{
			if (colInd < 0)
				throw new ArgumentOutOfRangeException("colInd", colInd, "Column index must be greater than zero.");

			if (rowInd < 0)
				throw new ArgumentOutOfRangeException("rowInd", rowInd, "Row index must be greater than zero.");

			var rowCount = GetRowsCount();

			if (rowInd >= rowCount)
			{
				if (!createIfNotExists)
					throw new ArgumentOutOfRangeException("rowInd", rowInd, "Row count {0} isn't enought for operation.".Put(rowCount));

				for (var i = rowCount; i <= rowInd; i++)
					_currentSheet.CreateRow(i);
			}

			var row = _currentSheet.GetRow(rowInd);

			//var cellCount = row.FirstCellNum == -1 ? 0 : (row.LastCellNum - row.FirstCellNum) + 1;

			var cellIndecies = _indecies.SafeAdd(rowInd, key => new HashSet<int>(row.Cells.Select(c => c.ColumnIndex)));

			if (!cellIndecies.Contains(colInd))
			{
				if (!createIfNotExists)
					throw new ArgumentOutOfRangeException("colInd", colInd, "Cell count {0} isn't enought for operation.".Put(cellIndecies.Count));

				//for (int i = cellCount; i <= colInd; i++)
				row.CreateCell(colInd);
				
				if (!cellIndecies.Add(colInd))
					throw new InvalidOperationException("Cell {0} already exists.".Put(colInd));
			}

			var cell = row.GetCell(colInd);

			if (cell == null)
				throw new InvalidOperationException("Cell doesn't exist.");

			return cell;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		public ExcelWorker Save(string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

			using (var file = new FileStream(fileName, FileMode.Create))
				Workbook.Write(file);

			_fileName = fileName;
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ExcelWorker Open()
		{
			Process.Start(_fileName);
			return this;
		}

		public ExcelWorker Protect(string password)
		{
			((HSSFSheet)_currentSheet).ProtectSheet(password);
			return this;
		}

		private void ThrowIfCurrentSheetIsEmpty()
		{
			if (_currentSheet == null)
				throw new InvalidOperationException("Current sheet is empty.");
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void DisposeManaged()
		{
			//Workbook.Close();
			//_excelEngine.Dispose();
			Workbook.Dispose();
			base.DisposeManaged();
		}
	}
}