namespace Ecng.Interop.NPOI
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Reflection;

	using NPOI.HSSF.UserModel;
	using NPOI.HSSF.Util;
	using NPOI.SS.Util;
	using NPOI.XSSF.UserModel;

	using Color = System.Windows.Media.Color;
	using ICellStyle = NPOI.SS.UserModel;

	public class NPOIExcelWorker : Disposable, IExcelWorker
	{
		private static readonly CachedSynchronizedDictionary<short, Color> _colors = new CachedSynchronizedDictionary<short, Color>();

		static NPOIExcelWorker()
		{
			var replaces = new Dictionary<string, string>
			{
				{ "OliveGreen", "Olive" },
				{ "DarkTeal", "Teal" },
				{ "Grey25Percent", "LightGray" },
				{ "Grey40Percent", "Gray" },
				{ "Grey50Percent", "DarkGray" },
				{ "Grey80Percent", "DimGray" },
				{ "DarkYellow", "YellowGreen" },
				{ "BlueGrey", "SlateGray" },
				{ "LightOrange", "Orange" },
				{ "BrightGreen", "GreenYellow" },
				{ "Rose", "RosyBrown" },
				{ "LightTurquoise", "Turquoise" },
				{ "PaleBlue", "PaleTurquoise" },
				{ "LightCornflowerBlue", "LightBlue" }
			};

			//Colors.LightBlue

			_colors.AddRange(typeof(HSSFColor).Assembly
				.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(HSSFColor)) && t.Name != "Automatic" && t.Name != "CustomColor")
				.ToDictionary(
					t => (short)t.GetField("Index").GetValue(null),
					t => typeof(Colors).GetValue<VoidType, Color>(replaces.TryGetValue(t.Name) ?? t.Name, null)));
		}

		private ICellStyle.ISheet _currentSheet;
		private string _fileName;

		private readonly Dictionary<int, HashSet<int>> _indecies = new Dictionary<int, HashSet<int>>();

		private readonly Dictionary<string, ICellStyle.ICellStyle> _cellStyleCache =
			new Dictionary<string, ICellStyle.ICellStyle>();

		private FileStream _stream;

		/// <summary>
		/// Create <see cref="ExcelWorker"/>.
		/// </summary>
		public ExcelWorker()
		{
			Workbook = new XSSFWorkbook();
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
				throw new ArgumentOutOfRangeException(nameof(name));

			_stream = new FileStream(name, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
			Workbook = ICellStyle.WorkbookFactory.Create(_stream);
		}

		[CLSCompliant(false)]
		public ICellStyle.IWorkbook Workbook { get; }

		//#region Import

		//public List<ICellStyle.ICell> cells = new List<ICellStyle.ICell>();

		//public ExcelWorker importXLS(string path)
		//{
		//	while (Workbook.NumberOfSheets != 0)
		//	{
		//		Workbook.RemoveSheetAt(Workbook.ActiveSheetIndex);
		//	}

		//	using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
		//	{
		//		//                Workbook = new XSSFWorkbook(file);

		//		Workbook = new HSSFWorkbook(file);
		//	}

		//	if (Workbook.NumberOfSheets > 0)

		//		for (int i = 0; i < Workbook.NumberOfSheets; i++)
		//		{
		//			_currentSheet = Workbook.GetSheetAt(i);
		//			IEnumerator rows = _currentSheet.GetRowEnumerator();
		//			int it = 0;
		//			while (rows.MoveNext())
		//			{
		//				ICellStyle.IRow row = (ICellStyle.IRow)rows.Current;
		//				foreach (var cell in row.Cells)
		//				{
		//					cells.Add(InternalGetCell(cell.ColumnIndex, row.RowNum));
		//				}
		//			}
		//		}

		//	return this;
		//}


		//public IEnumerable<T> GetColumn<T>(int col)
		//{
		//	try
		//	{
		//		return cells
		//			.Where(cc => cc.ColumnIndex == 1)
		//			.Select(cell => GetCell<T>(cell.ColumnIndex, cell.RowIndex))
		//			.ToArray();
		//	}
		//	catch (Exception e)
		//	{
		//		throw new ArgumentNullException(e.ToString());
		//	}
		//}

		//#endregion

		public NPOIExcelWorker MergeCells(int firstRow, int lastRow, int firstCol, int lastCol, bool isborder = false)
		{
			var region = new CellRangeAddress(firstRow, lastRow, firstCol, lastCol);
			_currentSheet.AddMergedRegion(region);

			if (isborder)
				((HSSFSheet)_currentSheet).SetEnclosedBorderOfRegion(region, ICellStyle.BorderStyle.Dotted,
					HSSFColor.Red.Index);

			return this;
		}

		public NPOIExcelWorker SetAligmentCell(int colInd, int rowInd,
			VerticalAlignment? verAligment = VerticalAlignment.Center,
			HorizontalAlignment? horAligment = HorizontalAlignment.Center)
		{
			var ccel = InternalGetCell(colInd, rowInd);
			var style = Workbook.CreateCellStyle();
			style.VerticalAlignment = verAligment == null ? ICellStyle.VerticalAlignment.None : ToExcel(verAligment.Value);
			style.Alignment = horAligment == null ? ICellStyle.HorizontalAlignment.General : ToExcel(horAligment.Value);
			ccel.CellStyle = style;
			return this;
		}

		private ICellStyle.HorizontalAlignment ToExcel(HorizontalAlignment aligment)
		{
			switch (aligment)
			{
				case HorizontalAlignment.Left:
					return ICellStyle.HorizontalAlignment.Left;
				case HorizontalAlignment.Center:
					return ICellStyle.HorizontalAlignment.Center;
				case HorizontalAlignment.Right:
					return ICellStyle.HorizontalAlignment.Right;
				case HorizontalAlignment.Stretch:
					return ICellStyle.HorizontalAlignment.Justify;
				default:
					throw new ArgumentOutOfRangeException(nameof(aligment), aligment, null);
			}
		}

		private ICellStyle.VerticalAlignment ToExcel(VerticalAlignment aligment)
		{
			switch (aligment)
			{
				case VerticalAlignment.Top:
					return ICellStyle.VerticalAlignment.Top;
				case VerticalAlignment.Center:
					return ICellStyle.VerticalAlignment.Center;
				case VerticalAlignment.Bottom:
					return ICellStyle.VerticalAlignment.Bottom;
				case VerticalAlignment.Stretch:
					return ICellStyle.VerticalAlignment.Justify;
				default:
					throw new ArgumentOutOfRangeException(nameof(aligment), aligment, null);
			}
		}

		public NPOIExcelWorker SetWidthAndHeight(int colInd, int rowInd, int widthCol, short heighRow)
		{
			InternalGetCell(colInd, rowInd);
			_currentSheet.SetColumnWidth(colInd, widthCol);
			_currentSheet.GetRow(rowInd).Height = heighRow;
			return this;
		}

		public NPOIExcelWorker AddHyperLink(int col, int row, string value, Color color)
		{
			var style = Workbook.CreateCellStyle();
			var font = Workbook.CreateFont();
			font.Underline = ICellStyle.FontUnderlineType.Single;
			font.Color = ToHSSFColorIndex(color);
			style.SetFont(font);

			var cell = InternalGetCell(col, row);

			var link = new XSSFHyperlink(ICellStyle.HyperlinkType.Url) { Address = (value) };

			cell.Hyperlink = link;
			cell.CellStyle = style;
			return this;
		}

		public NPOIExcelWorker SetCell(int col, int row, string value, DataFormat dataFormat)
		{
			var cell = InternalGetCell(col, row);
			var cStyle = Workbook.CreateCellStyle();
			cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("@");
			cell.CellStyle = cStyle;
			cell.SetCellValue(value);
			return this;
		}

		public NPOIExcelWorker SetCell(int col, int row, decimal value, DataFormat dataFormat)
		{
			SetCell(col, row, (double)value, dataFormat);
			return this;
		}

		public NPOIExcelWorker SetCell(int col, int row, int value, DataFormat dataFormat)
		{
			SetCell(col, row, (double)value, dataFormat);
			return this;
		}

		public NPOIExcelWorker SetCell(int col, int row, TimeSpan value, DataFormat dataFormat)
		{
			return SetCell(col, row, DateTime.Today + value, dataFormat);
		}

		public NPOIExcelWorker SetCell(int col, int row, DateTime value, DataFormat dataFormat)
		{
			var cell = InternalGetCell(col, row);
			var cStyle = Workbook.CreateCellStyle();
			cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat(GetDataType(dataFormat));
			cell.CellStyle = cStyle;
			cell.SetCellValue(value);
			return this;
		}

		private static string GetDataType(DataFormat format)
		{
			switch (format)
			{
				case DataFormat.Date1:
				{
					return "m/d/yy";
				}
				case DataFormat.Date2:
				{
					return "d-mmm-yy";
				}
				case DataFormat.Date3:
				{
					return "d-mmm";
				}
				case DataFormat.Time1:
				{
					return "mmm-yy";
				}
				case DataFormat.Time2:
				{
					return "h:mm AM/PM";
				}
				case DataFormat.Time3:
				{
					return "h:mm:ss AM/PM";
				}
				case DataFormat.Time4:
				{
					return "h:mm";
				}
				case DataFormat.Time5:
				{
					return "h:mm:ss";
				}
				case DataFormat.DateTime:
				{
					return "m/d/yy h:mm";
				}

				case DataFormat.Numeric1:
				{
					return "0";
				}
				case DataFormat.Numeric2:
				{
					return "0.00";
				}
				case DataFormat.Numeric3:
				{
					return "#,##0";
				}
				case DataFormat.Numeric4:
				{
					return "#,##0.00";
				}
				case DataFormat.Money1:
				{
					return "\"$\"#,##0_);(\"$\"#,##0)";
				}
				case DataFormat.Money2:
				{
					return "\"$\"#,##0_);[Red](\"$\"#,##0)";
				}
				case DataFormat.Money3:
				{
					return "\"$\"#,##0.00_);(\"$\"#,##0.00)";
				}
				case DataFormat.Money4:
				{
					return "\"$\"#,##0.00_);[Red](\"$\"#,##0.00)";
				}
				case DataFormat.Fractional1:
				{
					return "# ?/?";
				}
				case DataFormat.Fractional2:
				{
					return "# ??/??";
				}
				case DataFormat.Exponential:
				{
					return "0.00E+00";
				}
				case DataFormat.Percentage1:
				{
					return "0%";
				}
				case DataFormat.Percentage2:
				{
					return "0.00%";
				}
				case DataFormat.UniversalText:
				{
					return "@";
				}
				//case DataFormat.Default:
				default:
					return string.Empty;
			}
		}

		public NPOIExcelWorker SetCell(int col, int row, double value, DataFormat nomber = DataFormat.UniversalText)
		{
			var dataFormat = GetDataType(nomber);
			var cell = InternalGetCell(col, row);
			var cStyle = Workbook.CreateCellStyle();
			cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat(dataFormat);
			cell.CellStyle = cStyle;
			cell.SetCellValue(value);
			return this;
		}

		/// <summary>
		/// Sets color on background on cell
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="color"></param>
		/// <param name="fillPatternSolidForeground"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetBackGroundColor(int col, int row, Color color,
			ICellStyle.FillPattern fillPatternSolidForeground = ICellStyle.FillPattern.SolidForeground)
		{
			var style = Workbook.CreateCellStyle();
			style.FillBackgroundColor = ToHSSFColorIndex(color);
			//style.FillForegroundColor = hssfColorIndex;
			style.FillPattern = fillPatternSolidForeground;
			var cell = InternalGetCell(col, row);
			cell.CellStyle = style;
			_currentSheet.GetRow(row).GetCell(col).CellStyle = style;
			return this;
		}

		/// <summary>
		/// Grouping the range of Cloumns 
		/// </summary>
		/// <param name="from">from</param>
		/// <param name="to">to</param>
		/// <returns></returns>
		public NPOIExcelWorker GroupCloumns(int from, int to)
		{
			_currentSheet.GroupColumn(from, to);
			return this;
		}

		/// <summary>
		/// Grouping the range of Rows 
		/// </summary>
		/// <param name="from">from</param>
		/// <param name="to">to</param>
		/// <returns></returns>
		public NPOIExcelWorker GroupRows(int from, int to)
		{
			_currentSheet.GroupRow(from, to);
			return this;
		}

		/// <summary>
		/// Auto Sizing columns
		/// </summary>
		/// <param name="column">Nomber of column</param>
		/// <param name="useMergetCells">whether to use the contents of merged cells when 
		/// calculating the width of the column . Defoult is false</param> 
		/// <returns></returns>
		public NPOIExcelWorker AutoSizeColumn(int column, bool useMergetCells = false)
		{
			if (useMergetCells == false)
				_currentSheet.AutoSizeColumn(column);
			else
				_currentSheet.AutoSizeColumn(column, true);

			return this;
		}

		/// <summary>
		/// Сopying current sheet
		/// </summary>
		/// <param name="name">Name of new sheet</param>
		/// <param name="copyStyle">To copy style from current sheet</param>
		/// <returns></returns>
		public NPOIExcelWorker CopySheet(string name, bool copyStyle = true)
		{
			_currentSheet.CopySheet(name, copyStyle);
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sourceIndex"></param>
		/// <param name="targetIndex"></param>
		/// <returns></returns>
		public NPOIExcelWorker CopyRow(int sourceIndex, int targetIndex)
		{
			_currentSheet.CopyRow(sourceIndex, targetIndex);
			return this;
		}

		public NPOIExcelWorker CopyCell(int rowIndex, int colIndex, int targetIndex)
		{
			InternalGetCell(colIndex, rowIndex);
			_currentSheet.GetRow(rowIndex).CopyCell(colIndex, targetIndex);
			return this;
		}

		/// <summary>
		/// Name of current sheet. Null, if current sheet doesn't exist.
		/// </summary>
		public string CurrentSheetName => _currentSheet != null ? _currentSheet.SheetName : string.Empty;

		/// <summary>
		/// Create worksheet with specified name.
		/// </summary>
		/// <param name="sheetName">New name of worksheet. Must be unique.</param>
		/// <returns>Exporter.</returns>
		public NPOIExcelWorker AddSheet(string sheetName)
		{
			if (ContainsSheet(sheetName))
				return this;

			var sheet = Workbook.CreateSheet(sheetName);

			if (_currentSheet == null)
				_currentSheet = sheet;

			return this;
		}

		/// <summary>
		/// Removes current worksheet.
		/// </summary>
		/// <returns>Exporter.</returns>
		public NPOIExcelWorker RemoveSheet()
		{
			ThrowIfCurrentSheetIsEmpty();

			Workbook.RemoveSheetAt(Workbook.GetSheetIndex(_currentSheet));
			_currentSheet = null;
			_indecies.Clear();

			return this;
		}

		/// <summary>
		/// Removes specified worksheet.
		/// </summary>
		/// <param name="sheetName">Name of the sheet to remove.</param>
		/// <returns>Exporter.</returns>
		public NPOIExcelWorker RemoveSheet(string sheetName)
		{
			if (ContainsSheet(sheetName))
			{
				Workbook.RemoveSheetAt(Workbook.GetSheetIndex(sheetName));

				if (_currentSheet != null && _currentSheet.SheetName == sheetName)
					_currentSheet = null;

				_indecies.Clear();
			}

			return this;
		}

		/// <summary>
		/// Set name of the tab sheet.
		/// </summary>
		/// <param name="sheetName">New name of the sheet.</param>
		/// <returns></returns>
		public NPOIExcelWorker RenameSheet(string sheetName)
		{
			if (sheetName.IsEmpty())
				throw new ArgumentNullException(nameof(sheetName));

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
				throw new ArgumentNullException(nameof(sheetName));

			return Workbook.GetSheetIndex(sheetName) != -1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sheetName"></param>
		/// <returns></returns>
		public NPOIExcelWorker SwitchSheet(string sheetName)
		{
			if (ContainsSheet(sheetName))
			{
				_currentSheet = Workbook.GetSheet(sheetName);
				_indecies.Clear();
			}

			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="width"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetWidth(int col, int width)
		{
			ThrowIfCurrentSheetIsEmpty();
			_currentSheet.SetColumnWidth(col, width);
			return this;
		}

		public NPOIExcelWorker SetHeight(int row, short height)
		{
			ThrowIfCurrentSheetIsEmpty();
			_currentSheet.GetRow(row).Height = height;
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="color"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetBackColor(int col, int row, Color color)
		{
			var cell = InternalGetCell(col, row);
			var style = (XSSFCellStyle)Workbook.CreateCellStyle();
			style.FillBackgroundColorColor = ToExcelColor(color);
			cell.CellStyle = style;
			return this;
		}

		public Color GetBackColor(int col, int row)
		{
			return ToWpfColor((XSSFColor)InternalGetCell(col, row).CellStyle.FillBackgroundColorColor);
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
		public NPOIExcelWorker SetForeColor(int col, int row, Color color)
		{
			var cell = InternalGetCell(col, row);
			var style = (XSSFCellStyle)Workbook.CreateCellStyle();
			style.FillForegroundColorColor = ToExcelColor(color);
			cell.CellStyle = style;
			return this;
		}

		public Color GetForeColor(int col, int row)
		{
			var color = (XSSFColor)InternalGetCell(col, row).CellStyle.FillForegroundColorColor;
			return ToWpfColor(color);
		}

		private static ICellStyle.IColor ToExcelColor(Color color)
		{
			return new XSSFColor(new[] { color.R, color.G, color.B });
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstRow"></param>
		/// <param name="lastRow"></param>
		/// <param name="firstColumn"></param>
		/// <param name="lasColumn"></param>
		/// <param name="backGroungColor"></param>
		/// <param name="formula1"></param>
		/// <param name="formula2"></param>
		/// <param name="comparison"></param>
		/// <param name="isUseComparision"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetConditionalFormatting(int firstColumn, int lasColumn, int firstRow, int lastRow,
			Color backGroungColor, string formula1, string formula2 = "",
			ComparisonOperator comparison = ComparisonOperator.Equal, bool isUseComparision = false)
		{
			CellRangeAddress[] myDataRange =
			{
				new CellRangeAddress(firstRow, lastRow, firstColumn, lasColumn)
			};
			var sheetCf = _currentSheet.SheetConditionalFormatting;

			ICellStyle.IConditionalFormattingRule rule;
			if (isUseComparision == false)
				rule = sheetCf.CreateConditionalFormattingRule(formula1);
			else
			{
				var comparisonOperator = ToExcelOperator(comparison);
				rule = (comparisonOperator == ICellStyle.ComparisonOperator.Between || comparisonOperator == ICellStyle.ComparisonOperator.NotBetween) ? sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1, formula2) : sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1);
			}

			var fill = rule.CreatePatternFormatting();
			fill.FillBackgroundColor = ToHSSFColorIndex(backGroungColor);
			fill.FillPattern = (short)ICellStyle.FillPattern.SolidForeground;
			sheetCf.AddConditionalFormatting(myDataRange, rule);
			return this;
		}

		/// <summary>
		///Поддерживает только стандартные цвета Aqua,Black,Blue,Brown,CornflowerBlue,DarkBlue,
		/// DarkGreen,DarkRed,Gold,Green,Indigo,Lavender,LemonChiffon,LightBlue,LightGreen,
		/// LightYellow,Lime,Maroon,Orange,Orchid,Pink,Plum,Red,RoyalBlue,SeaGreen,SkyBlue,
		/// Tan,Teal,Turquoise Violet ,White ,Yellow
		///fontColor - не работает так как GoreGroundColor - является вторым цветом заливки , у данного типа заливки цвет один
		/// </summary>
		/// <param name="col"></param>
		/// <param name="comparison"></param>
		/// <param name="formula1"></param>
		/// <param name="bgColor"></param>
		/// <param name="fgColor"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetConditionalFormatting(int col, ComparisonOperator comparison, string formula1,
			Color? bgColor, Color? fgColor)
		{
			return SetConditionalFormatting(col, col, 0, ushort.MaxValue, comparison, formula1, formula1, bgColor, fgColor);
		}

		private static short ToHSSFColorIndex(Color color)
		{
			return _colors.CachedPairs.First(v => v.Value == color).Key;
		}

		/// <summary>
		///Поддерживает только стандартные цвета Aqua,Black,Blue,Brown,CornflowerBlue,DarkBlue,
		/// DarkGreen,DarkRed,Gold,Green,Indigo,Lavender,LemonChiffon,LightBlue,LightGreen,
		/// LightYellow,Lime,Maroon,Orange,Orchid,Pink,Plum,Red,RoyalBlue,SeaGreen,SkyBlue,
		/// Tan,Teal,Turquoise Violet ,White ,Yellow
		/// </summary>
		/// <param name="colStart"></param>
		/// <param name="rowStart"></param>
		/// <param name="colEnd"></param>
		/// <param name="rowEnd"></param>
		/// <param name="comparison"></param>
		/// <param name="formula1"></param>
		/// <param name="formula2"></param>
		/// <param name="bgColor"></param>
		/// <param name="fontColor"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetConditionalFormatting(int colStart, int colEnd, int rowStart, int rowEnd,
			ComparisonOperator comparison, string formula1, string formula2, Color? bgColor, Color? fontColor)
		{
			CellRangeAddress[] dataRange =
			{
				new CellRangeAddress(rowStart, rowEnd, colStart, colEnd)
			};

			var sheetCf = _currentSheet.SheetConditionalFormatting;

			var comparisonOperator = ToExcelOperator(comparison);

			var rule = (comparisonOperator == ICellStyle.ComparisonOperator.Between || comparisonOperator == ICellStyle.ComparisonOperator.NotBetween) ? sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1, formula2) : sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1);

			var fill = rule.CreatePatternFormatting();
			var fontColorIndex = fontColor == null ? (short?)null : ToHSSFColorIndex(fontColor.Value);
			if (fontColorIndex != null)
			{
				var font = rule.CreateFontFormatting();
				font.SetFontStyle(false, true);
				font.FontColorIndex = (short)fontColorIndex;
			}

			var bgColorIndex = bgColor == null ? (short?)null : ToHSSFColorIndex(bgColor.Value);
			if (bgColorIndex != null)
			{
				fill.FillBackgroundColor = (short)bgColorIndex;
				fill.FillPattern = (short)ICellStyle.FillPattern.SolidForeground;
			}

			//			 fill.FillForegroundColor = (short) fgColorIndex;

			//			fill.FillPattern = fontColorIndex == null ? (short) FillPattern.SolidForeground : (short) FillPattern.Bricks;
			sheetCf.AddConditionalFormatting(dataRange, rule);

			return this;
		}

		private static Color ToWpfColor(XSSFColor color)
		{
			if (color == null)
				throw new ArgumentNullException(nameof(color));

			var argb = color.GetARgb();
			return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
		}

		private static ICellStyle.ComparisonOperator ToExcelOperator(ComparisonOperator comparison)
		{
			switch (comparison)
			{
				case ComparisonOperator.Equal:
					return ICellStyle.ComparisonOperator.Equal;
				case ComparisonOperator.NotEqual:
					return ICellStyle.ComparisonOperator.NotEqual;
				case ComparisonOperator.Greater:
					return ICellStyle.ComparisonOperator.GreaterThan;
				case ComparisonOperator.GreaterOrEqual:
					return ICellStyle.ComparisonOperator.GreaterThanOrEqual;
				case ComparisonOperator.Less:
					return ICellStyle.ComparisonOperator.LessThan;
				case ComparisonOperator.LessOrEqual:
					return ICellStyle.ComparisonOperator.LessThanOrEqual;
				case ComparisonOperator.Any:
					return ICellStyle.ComparisonOperator.NoComparison;
				default:
					throw new ArgumentOutOfRangeException(nameof(comparison));
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="font"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetFont(int col, int row, Font font)
		{
			if (font == null)
				throw new ArgumentNullException(nameof(font));

			var cellFont = InternalGetCell(col, row).CellStyle.GetFont(Workbook);

			//cellFont.Color = font;
			cellFont.IsItalic = font.Italic;
			cellFont.FontName = font.Name;
			cellFont.FontHeight = (short)font.Size;

			return this;
		}

		public NPOIExcelWorker SetStyle(int col, Type dataType)
		{
			return SetStyle(col, GetDefaultDataFormat(dataType));
		}

		public NPOIExcelWorker SetStyle(int col, string dataFormat)
		{
			if (!_cellStyleCache.ContainsKey(dataFormat))
			{
				var style = _currentSheet.Workbook.CreateCellStyle();

				// check if this is a built-in format
				// TODO
				var builtinFormatId = (short)-1; //XSSFDataFormat.GetBuiltinFormat(dataFormat);

				if (builtinFormatId != -1)
					style.DataFormat = builtinFormatId;
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
				return "General";

			switch (Type.GetTypeCode(dataType))
			{
				case TypeCode.Empty:
				case TypeCode.Object:
				case TypeCode.DBNull:
				case TypeCode.Char:
				case TypeCode.String:
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public NPOIExcelWorker SetCell(int col, int row, object value)
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
			else if (value is ICellStyle.IRichTextString)
				cell.SetCellValue((ICellStyle.IRichTextString)value);
			else if (value is double || value is int)
				cell.SetCellValue(value.To<double>());
			else
				cell.SetCellValue(value.ToString());

			return this;
		}

		public NPOIExcelWorker SetFormula(int col, int row, string formula)
		{
			var cell = InternalGetCell(col, row);
			cell.CellFormula = formula;
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		public T GetCell<T>(int col, int row)
		{
			var cell = InternalGetCell(col, row, false);

			if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(uint) || typeof(T) == typeof(ulong) || typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(double) || typeof(T) == typeof(float) || typeof(T) == typeof(decimal))
				return cell.NumericCellValue.To<T>();
			if (typeof(T) == typeof(DateTime))
				return cell.DateCellValue.To<T>();
			if (typeof(T) == typeof(TimeSpan))
				return cell.DateCellValue.TimeOfDay.To<T>();
			if (typeof(T) == typeof(string))
				return cell.StringCellValue.To<T>();
			if (typeof(T) == typeof(bool))
				return cell.BooleanCellValue.To<T>();
			switch (cell.CellType)
			{
				case ICellStyle.CellType.Unknown:
					return cell.RichStringCellValue.To<T>();
				case ICellStyle.CellType.Numeric:
					return cell.NumericCellValue.To<T>();
				case ICellStyle.CellType.String:
					return cell.StringCellValue.To<T>();
				case ICellStyle.CellType.Formula:
					return cell.CellFormula.To<T>();
				case ICellStyle.CellType.Blank:
					return default(T);
				case ICellStyle.CellType.Boolean:
					return cell.BooleanCellValue.To<T>();
				case ICellStyle.CellType.Error:
					return cell.ErrorCellValue.To<T>();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public ICellStyle.ICell InternalGetCell(int colInd, int rowInd, bool createIfNotExists = true)
		{
			if (colInd < 0)
				throw new ArgumentOutOfRangeException(nameof(colInd), colInd, "Column index must be greater than zero.");

			if (rowInd < 0)
				throw new ArgumentOutOfRangeException(nameof(rowInd), rowInd, "Row index must be greater than zero.");

			var rowCount = GetRowsCount();

			if (rowInd >= rowCount)
			{
				if (!createIfNotExists)
					throw new ArgumentOutOfRangeException(nameof(rowInd), rowInd, "Row count {0} isn't enought for operation.".Put(rowCount));

				for (var i = rowCount; i <= rowInd; i++)
					_currentSheet.CreateRow(i);
			}

			var row = _currentSheet.GetRow(rowInd);
			var cellIndecies = _indecies.SafeAdd(rowInd, key => new HashSet<int>(row.Cells.Select(c => c.ColumnIndex)));

			if (!cellIndecies.Contains(colInd))
			{
				if (!createIfNotExists)
					throw new ArgumentOutOfRangeException(nameof(colInd), colInd, "Cell count {0} isn't enought for operation.".Put(cellIndecies.Count));
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
		/// <param name="autoSizeColumns"></param>
		/// <returns></returns>
		public NPOIExcelWorker Save(string fileName, bool autoSizeColumns)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			if (autoSizeColumns)
			{
				for (var i = 0; i < Workbook.NumberOfSheets; i++)
				{
					var sheet = Workbook.GetSheetAt(i);

					// TODO
					for (var j = 0; j < 20; j++)
						sheet.AutoSizeColumn(j);
				}
			}

			using (var file = new FileStream(fileName, FileMode.Create))
				Workbook.Write(file);

			_fileName = fileName;
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public NPOIExcelWorker Open()
		{
			Process.Start(_fileName);
			return this;
		}

		public NPOIExcelWorker Protect(string password)
		{
			_currentSheet.ProtectSheet(password);
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
			if (_stream != null)
			{
				_stream.Dispose();
				_stream = null;
			}

			base.DisposeManaged();
		}

		public enum DataFormat
		{
			/// <summary>
			/// "0", //1 Числовой    2000000  -2000000
			/// </summary>
			Numeric1,

			/// <summary>
			/// "0.00", //2 Числовой с 2мя знаками после запятой   2000000,35 -2000000,35
			/// </summary>
			Numeric2,

			/// <summary>
			/// "#,##0", //3 Числовой с разделителями  2 000 000  -2 000 000
			/// </summary>
			Numeric3,

			/// <summary>
			/// //4 Числовой с разделителями с 2мя знаками после запятой   2 000 000,35   -2 000 000,35
			/// </summary>
			Numeric4,

			/// <summary>
			/// //5 Денежный 2 000 000 ₽ -2 000 000 ₽
			/// </summary>
			Money1,

			/// <summary>
			/// //6 Денежный  с красными отрицательными 2 000 000 ₽  -2 000 000 ₽
			/// </summary>
			Money2,

			/// <summary>
			/// //7 Денежный с двумя знаками после запятой 2 000 000,35 ₽ -2 000 000,35 ₽
			/// </summary>
			Money3,

			/// <summary>
			/// //8 Денежный с двумя знаками после запятой 2 000 000,35 ₽ -2 000 000,35 ₽
			/// </summary>
			Money4,

			/// <summary>
			/// //9 Процентный 200000035% -200000035% (Некорректно отржаются десятичные знаки)
			/// </summary>
			Percentage1,

			/// <summary>
			/// "0.00%", //10 Процентный 200000034,54% -200000034,54% (Некорректно отржаются десятичные знаки)
			/// </summary>
			Percentage2,

			/// <summary>
			/// "0.00E+00", //11 Экспоненциальный  2,00E+06 -2,00E+06
			/// </summary>
			Exponential,

			/// <summary>
			/// "# ?/?", //12 Дробный простыми дробями 2000000 1/3  -2000000 1/3
			/// </summary>
			Fractional1,

			/// <summary>
			/// "# ??/??", //13 Дробный до двух знаков 2000000 19/55 -2000000 19/55
			/// </summary>
			Fractional2,

			/// <summary>
			/// "m/d/yy", //14  Дата 23.10.7375
			/// </summary>
			Date1,

			/// <summary>
			/// "d-mmm-yy", //15 Дата кастомная  23.окт.75
			/// </summary>
			Date2,

			/// <summary>
			/// "d-mmm", //16 Дата кастомная окт.75
			/// </summary>
			Date3,

			/// <summary>
			/// "mmm-yy", //17 Время кастомное 8:17 AM
			/// </summary>
			Time1,

			/// <summary>
			/// "h:mm AM/PM", //18 Время кастомное 8:17 AM
			/// </summary>
			Time2,

			/// <summary>
			/// "h:mm:ss AM/PM", //19  Время кастомное 8:17: 26 AM
			/// </summary>
			Time3,

			/// <summary>
			/// "h:mm", //20 Время кастомное 8:17
			/// </summary>
			Time4,

			/// <summary>
			/// "h:mm:ss", //21 Время кастомное 8:17:26
			/// </summary>
			Time5,

			/// <summary>
			/// "m/d/yy h:mm", //22 ДатаВремя кастомное  23.10.7375 8:17
			/// </summary>
			DateTime,

			/// <summary>
			/// "@", //35 Текстовый 2000000,345 -2000000,345
			/// </summary>
			UniversalText,

			/// <summary>
			/// 
			/// </summary>
			Default
		}

		#region DataTypes

		//        List<string> types1 = new List<string>()
		//            {
		//                "0", //1 Числовой    2000000  -2000000 
		//                "0.00", //2 Числовой с 2мя знаками после запятой   2000000,35 -2000000,35  
		//                "#,##0", //3 Числовой с разделителями  2 000 000  -2 000 000
		////                "#,##0.000000", //4 Числовой с разделителями с 2мя знаками после запятой   2 000 000,35   -2 000 000,35
		//                "#,##0.00", //4 Числовой с разделителями с 2мя знаками после запятой   2 000 000,35   -2 000 000,35
		//                "\"$\"#,##0_);(\"$\"#,##0)", //5 Денежный 2 000 000 ₽ -2 000 000 ₽
		//                "\"$\"#,##0_);[Red](\"$\"#,##0)", //6 Денежный  с красными отрицательными 2 000 000 ₽  -2 000 000 ₽
		//                "\"$\"#,##0.00_);(\"$\"#,##0.00)",
		//                //7 Денежный с двумя знаками после запятой 2 000 000,35 ₽ -2 000 000,35 ₽
		//                "\"$\"#,##0.00_);[Red](\"$\"#,##0.00)",
		//                //8 Денежный с двумя знаками после запятой 2 000 000,35 ₽ -2 000 000,35 ₽
		//                "0%", //9 Процентный 200000035% -200000035% (Некорректно отржаются десятичные знаки)
		//
		//                "0.00%", //10 Процентный 200000034,54% -200000034,54% (Некорректно отржаются десятичные знаки)
		//
		//                "0.00E+00", //11 Экспоненциальный  2,00E+06 -2,00E+06
		//
		//                "# ?/?", //12 Дробный простыми дробями 2000000 1/3  -2000000 1/3
		//
		//                "# ??/??", //13 Дробный до двух знаков 2000000 19/55 -2000000 19/55
		//                "m/d/yy", //14  Дата 23.10.7375
		//                "d-mmm-yy", //15 Дата кастомная  23.окт.75
		//                "d-mmm", //16 Дата кастомная окт.75
		//                "mmm-yy", //17 Время кастомное 8:17 AM
		//                "h:mm AM/PM", //18 Время кастомное 8:17 AM
		//                "h:mm:ss AM/PM", //19  Время кастомное 8:17: 26 AM
		//                "h:mm", //20 Время кастомное 8:17
		//                "h:mm:ss", //21 Время кастомное 8:17:26
		//                "m/d/yy h:mm", //22 ДатаВремя кастомное  23.10.7375 8:17
		//                "#,##0_);(#,##0)", //23 Кастомное  2 000 000  -2 000 000  
		//                "#,##0_);[Red](#,##0)", //24Кастомное с красными отрицательными  2 000 000  -2 000 000  
		//                "#,##0.00_);(#,##0.00)", //25Кастомное 2 000 000,35  -2 000 000,35  
		//                "#,##0.00_);[Red](#,##0.00)", //26Кастомное с красным 2 000 000,35  -2 000 000,35  
		//                "_(\"$\"* #,##0_);_(\"$\"* (#,##0);_(\"$\"* \"-\"_);_(@_)", //Финансовый 27  2 000 000   -2 000 000   
		//                "_(* #,##0_);_(* (#,##0);_(* \"-\"_);_(@_)", //28 Финансовый с рублями 2 000 000 ₽ -2 000 000 ₽ 
		//                "_(* #,##0.00_);_(* (#,##0.00);_(* \"-\"??_);_(@_)", //29 Финансовый   2 000 000,35   -2 000 000,35   
		//                "_(\"$\"* #,##0.00_);_(\"$\"* (#,##0.00);_(\"$\"* \"-\"??_);_(@_)", //30 
		//                "mm:ss", //31 Время кастомное 17:26
		//                "[h]:mm:ss", //32 Время кастомное 17:26 48000008:17:26
		//                "mm:ss.0", //33  Время кастомное 17:25,7
		//                "##0.0E+0", //34 Кастомное 2,0E+6 -2,0E+6
		//                "@", //35 Текстовый 2000000,345 -2000000,345
		//            };

		#endregion
	}
}