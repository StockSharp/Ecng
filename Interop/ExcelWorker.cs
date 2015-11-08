using System.Collections;
using System.Windows.Media;

namespace Ecng.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing;
	using System.IO;
	using System.Linq;
	using Color = System.Windows.Media.Color;
	using NPOI.HSSF.UserModel;
	using Ecng.Common;
	using Ecng.Collections;
	using ICellStyle = NPOI.SS.UserModel;
	using NPOI.XSSF.UserModel;
	using NPOI.SS.UserModel;
	using NPOI.SS.Util;

	using NPOI.HSSF.Util;

	public class ExcelWorker : Disposable
	{
		private ISheet _currentSheet;
		private string _fileName;

		private readonly Dictionary<int,HashSet<int>> _indecies = new Dictionary<int,HashSet<int>>();

		private readonly Dictionary<string, NPOI.SS.UserModel.ICellStyle> _cellStyleCache =
			new Dictionary<string, NPOI.SS.UserModel.ICellStyle>();

		private FileStream _stream;

		#region Import
		public List<ICell> cells = new List<ICell>();

		public ExcelWorker importXLS(string path)
		{
			while (Workbook.NumberOfSheets != 0)
			{
				Workbook.RemoveSheetAt(Workbook.ActiveSheetIndex);
			}

			using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				//                Workbook = new XSSFWorkbook(file);

				Workbook = new HSSFWorkbook(file);
			}

			if (Workbook.NumberOfSheets > 0)

				for (int i = 0; i < Workbook.NumberOfSheets; i++)
				{
					_currentSheet = Workbook.GetSheetAt(i);
					System.Collections.IEnumerator rows = (IEnumerator) _currentSheet.GetRowEnumerator();
					int it = 0;
					while (rows.MoveNext())
					{
						IRow row = (IRow) rows.Current;
						foreach (var cell in row.Cells)
						{
							cells.Add(InternalGetCell(cell.ColumnIndex, row.RowNum));
						}
					}
				}
			return this;
		}


		public IEnumerable<T> GetColumn<T>(int col)
		{
			try
			{
				List<T> TT = new List<T>();
				foreach (var cell in cells.Where(cc => cc.ColumnIndex == 1)) TT.Add(GetCell<T>(cell.ColumnIndex, cell.RowIndex));
				return TT;
			}
			catch (Exception e)
			{
				throw new ArgumentNullException(e.ToString());
			}
		}

		#endregion

		public ExcelWorker MergeCells(int firstRow, int lastRow, int firstCol, int lastCol, bool isborder = false)
		{
			var region = new CellRangeAddress(firstRow, lastRow, firstCol, lastCol);
			_currentSheet.AddMergedRegion(region);
			if (isborder)
				((HSSFSheet) _currentSheet).SetEnclosedBorderOfRegion(region, BorderStyle.Dotted,
					NPOI.HSSF.Util.HSSFColor.Red.Index);
			return this;
		}

		public ExcelWorker SetAligmentCell(int colInd, int rowInd,
			VerticalAlignment verAligment = VerticalAlignment.Center,
			HorizontalAlignment horAligment = HorizontalAlignment.Center)
		{
			var ccel = InternalGetCell(colInd, rowInd);
			var style = Workbook.CreateCellStyle();
			style.VerticalAlignment = verAligment;
			style.Alignment = horAligment;
			ccel.CellStyle = style;
			return this;
		}

		public ExcelWorker SetWidthAndHeight(int colInd, int rowInd, int widthCol, short heighRow)
		{
			InternalGetCell(colInd, rowInd);
			_currentSheet.SetColumnWidth(colInd, widthCol);
			_currentSheet.GetRow(rowInd).Height = heighRow;
			return this;
		}

		public ExcelWorker AddHyperLink(int col, int row, string value, short HSSFColorIndex = HSSFColor.Blue.Index)
		{
			var hlink_style = Workbook.CreateCellStyle();
			IFont hlink_font = Workbook.CreateFont();
			hlink_font.Underline = FontUnderlineType.Single;
			hlink_font.Color = HSSFColorIndex;
			hlink_style.SetFont(hlink_font);

			var cell = InternalGetCell(col, row);

			XSSFHyperlink link = new XSSFHyperlink(HyperlinkType.Url);
			link.Address = (value);

			cell.Hyperlink = link;
			cell.CellStyle = hlink_style;
			return this;
		}

		public ExcelWorker SetCell(int col,int row,string value,DataFormat dataFormat)
		{
			var cell = InternalGetCell(col, row);
			var cStyle = Workbook.CreateCellStyle();
			cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("@");
			cell.CellStyle = cStyle;
			cell.SetCellValue(value);
			return this;
		}

		public ExcelWorker SetCell(int col,int row,decimal value,DataFormat dataFormat)
		{
			SetCell(col,row,(double)value,dataFormat);
			return this;
		}

		public ExcelWorker SetCell(int col,int row,int value,DataFormat dataFormat)
		{
			SetCell(col,row,(double)value,dataFormat);
			return this;
		}

		public ExcelWorker SetCell(int col, int row, TimeSpan value,DataFormat dataFormat)
		{
			return SetCell(col, row, DateTime.Today + value, dataFormat);
		}

		public ExcelWorker SetCell(int col, int row, DateTime value, DataFormat dataFormat)
		{
			var cell = InternalGetCell(col, row);
			var cStyle = Workbook.CreateCellStyle();
			cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat(GetDataType(dataFormat));
			cell.CellStyle = cStyle;
			cell.SetCellValue(value);
			return this;
		}

		private string GetDataType(DataFormat typeDate)
		{
			switch (typeDate)
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

				case DataFormat.Nuneric1:
				{
					return "0";
				}
				case DataFormat.Nuneric2:
				{
					return "0.00";
				}
				case DataFormat.Nuneric3:
				{
					return "#,##0";
				}
				case DataFormat.Nuneric4:
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
			}
			return "";
		}

		public ExcelWorker SetCell(int col, int row, double value,
			DataFormat nomber = DataFormat.UniversalText)
		{
			string dataFormat = GetDataType(nomber);
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
		/// <param name="nomCol"></param>
		/// <param name="nomRow"></param>
		/// <param name="hssfColorIndex"></param>
		/// <param name="fillPatternSolidForeground"></param>
		/// <returns></returns>
		public ExcelWorker SetBackGroundColor(int col, int row, short hssfColorIndex,
			FillPattern fillPatternSolidForeground = FillPattern.SolidForeground)
		{
			var style = Workbook.CreateCellStyle();
			style.FillBackgroundColor = hssfColorIndex;
			style.FillForegroundColor = hssfColorIndex;
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
		public ExcelWorker GroupCloumns(int from, int to)
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
		public ExcelWorker GroupRows(int from, int to)
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
		public ExcelWorker AutoSizeColumn(int column, bool useMergetCells = false)
		{
			if (useMergetCells == false)
			{
				_currentSheet.AutoSizeColumn(column);
			}
			else
			{
				_currentSheet.AutoSizeColumn(column, true);
			}
			return this;
		}

		/// <summary>
		/// Сopying current sheet
		/// </summary>
		/// <param name="name">Name of new sheet</param>
		/// <param name="copyStyle">To copy style from current sheet</param>
		/// <returns></returns>
		public ExcelWorker CopySheet(string name, bool copyStyle = true)
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
		public ExcelWorker CopyRow(int sourceIndex, int targetIndex)
		{
			_currentSheet.CopyRow(sourceIndex, targetIndex);
			return this;
		}

		public ExcelWorker CopyCell(int rowIndex, int colIndex, int targetIndex)
		{
			InternalGetCell(colIndex, rowIndex);
			_currentSheet.GetRow(rowIndex).CopyCell(colIndex, targetIndex);
			return this;
		}

		/// <summary>
		/// Create <see cref="ExcelWorker"/>.
		/// </summary>
		public ExcelWorker()
		{
			Workbook = new XSSFWorkbook();
			//            Workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
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

			_stream = new FileStream(name, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
			Workbook = NPOI.SS.UserModel.WorkbookFactory.Create(_stream);
		}

		[CLSCompliant(false)]
		public NPOI.SS.UserModel.IWorkbook Workbook { get; private set; }

		/// <summary>
		/// Name of current sheet. Null, if current sheet doesn't exist.
		/// </summary>
		public string CurrentSheetName
		{
			get { return _currentSheet != null ? _currentSheet.SheetName : string.Empty; }
		}

		/// <summary>
		/// Create worksheet with specified name.
		/// </summary>
		/// <param name="sheetName">New name of worksheet. Must be unique.</param>
		/// <returns>Exporter.</returns>
		public ExcelWorker AddSheet(string sheetName)
		{
			if (ContainsSheet(sheetName))
				return this;

			var sheet = Workbook.CreateSheet(sheetName);
			if (_currentSheet == null)
			{
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
			//            _currentSheet. InternalGetCell
			var style = (XSSFCellStyle) Workbook.CreateCellStyle();
			style.FillBackgroundColorColor = ToExcelColor(color);
			cell.CellStyle = style;
			return this;
		}

		public Color GetBackColor(int col, int row)
		{
			return ToWpfColor((XSSFColor) InternalGetCell(col, row).CellStyle.FillBackgroundColorColor);
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
		public ExcelWorker SetForeColor(int col,int row,Color color)
		{
			var cell = InternalGetCell(col,row);
			var style = (XSSFCellStyle)Workbook.CreateCellStyle();
			style.FillForegroundColorColor = ToExcelColor(color);
			cell.CellStyle = style;
			return this;
		}

		public Color GetForeColor(int col,int row)
		{
			var color = (XSSFColor)InternalGetCell(col,row).CellStyle.FillForegroundColorColor;
			return ToWpfColor(color);
		}

		private static IColor ToExcelColor(Color color)
		{
			return new XSSFColor(new[] {color.R, color.G, color.B});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="firstRow"></param>
		/// <param name="lastRow"></param>
		/// <param name="firstColumn"></param>
		/// <param name="lasColumn"></param>
		/// <param name="backGroungColorInd"></param>
		/// <param name="formula1"></param>
		/// <param name="formula2"></param>
		/// <param name="comparisonOperator"></param>
		/// <param name="isUseComparision"></param>
		/// <returns></returns>
		public ExcelWorker SetConditionalFormatting(int firstColumn, int lasColumn ,int firstRow, int lastRow
			, short backGroungColorInd, string formula1, string formula2 = "",
			ComparisonOperator comparisonOperator = ComparisonOperator.Equal, bool isUseComparision = false)
		{
			NPOI.SS.Util.CellRangeAddress[] my_data_range =
			{
				new NPOI.SS.Util.CellRangeAddress(firstRow, lastRow, firstColumn, lasColumn)
			};
			var sheetCf = _currentSheet.SheetConditionalFormatting;

			IConditionalFormattingRule rule;
			if (isUseComparision == false)
			{
				rule = sheetCf.CreateConditionalFormattingRule(formula1);
			}
			else
			{
				rule = (comparisonOperator == ComparisonOperator.Between ||
				        comparisonOperator == ComparisonOperator.NotBetween)
					? sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1, formula2)
					: sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1);
			}

			var fill = rule.CreatePatternFormatting();
			fill.FillBackgroundColor = backGroungColorInd;
			fill.FillPattern = (short) FillPattern.SolidForeground;
			sheetCf.AddConditionalFormatting(my_data_range, rule);
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
		public ExcelWorker SetConditionalFormatting(int col, Ecng.ComponentModel.ComparisonOperator comparison,
			string formula1,Color? bgColor,Color? fontColor)
		{

			return SetConditionalFormatting(col,col,0,ushort.MaxValue,comparison,formula1,formula1,bgColor,fontColor);
		}

		private short? ToHSSFColorIndex(Color? Color)
		{
			if (Color == null) return null;
			Color col = (Color) Color;
			if (col == Colors.Aqua) return HSSFColor.Aqua.Index;
			if (col == Colors.Black) return HSSFColor.Black.Index;
			if (col == Colors.Blue) return HSSFColor.Blue.Index;
			if (col == Colors.Brown) return HSSFColor.Brown.Index;
			if (col == Colors.CornflowerBlue) return HSSFColor.CornflowerBlue.Index;
			if (col == Colors.DarkBlue) return HSSFColor.DarkBlue.Index;
			if (col == Colors.DarkGreen) return HSSFColor.DarkGreen.Index;
			if (col == Colors.DarkRed) return HSSFColor.DarkRed.Index;
			if (col == Colors.Gold) return HSSFColor.Gold.Index;
			if (col == Colors.Green) return HSSFColor.Green.Index;
			if (col == Colors.Indigo) return HSSFColor.Indigo.Index;
			if (col == Colors.Lavender) return HSSFColor.Lavender.Index;
			if (col == Colors.LemonChiffon) return HSSFColor.LemonChiffon.Index;
			if (col == Colors.LightBlue) return HSSFColor.LightBlue.Index;
			if (col == Colors.LightGreen) return HSSFColor.LightGreen.Index;
			if (col == Colors.LightYellow) return HSSFColor.LightYellow.Index;
			if (col == Colors.Lime) return HSSFColor.Lime.Index;
			if (col == Colors.Maroon) return HSSFColor.Maroon.Index;
			if (col == Colors.Orange) return HSSFColor.Orange.Index;
			if (col == Colors.Orchid) return HSSFColor.Orchid.Index;
			if (col == Colors.Pink) return HSSFColor.Pink.Index;
			if (col == Colors.Plum) return HSSFColor.Plum.Index;
			if (col == Colors.Red) return HSSFColor.Red.Index;
			if (col == Colors.RoyalBlue) return HSSFColor.RoyalBlue.Index;
			if (col == Colors.SeaGreen) return HSSFColor.SeaGreen.Index;
			if (col == Colors.SkyBlue) return HSSFColor.SkyBlue.Index;
			if (col == Colors.Tan) return HSSFColor.Tan.Index;
			if (col == Colors.Teal) return HSSFColor.Teal.Index;
			if (col == Colors.Turquoise) return HSSFColor.Turquoise.Index;
			if (col == Colors.Violet) return HSSFColor.Violet.Index;
			if (col == Colors.White) return HSSFColor.White.Index;
			if (col == Colors.Yellow) return HSSFColor.Yellow.Index;
			if (col == Colors.Green) return HSSFColor.Green.Index;
			if (col == Colors.Red) return HSSFColor.Red.Index;
			return null;
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
		public ExcelWorker SetConditionalFormatting(int colStart, int colEnd, int rowStart, int rowEnd,
			Ecng.ComponentModel.ComparisonOperator comparison, string formula1, string formula2, Color? bgColor,
			Color? fontColor)
		{
			NPOI.SS.Util.CellRangeAddress[] my_data_range =
			{
				new NPOI.SS.Util.CellRangeAddress(rowStart, rowEnd, colStart, colEnd)
			};
			var sheetCf = _currentSheet.SheetConditionalFormatting;

			var comparisonOperator = ToExcelOperator(comparison);
			IConditionalFormattingRule rule;

			rule = (comparisonOperator == ComparisonOperator.Between ||
			        comparisonOperator == ComparisonOperator.NotBetween)
				? sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1, formula2)
				: sheetCf.CreateConditionalFormattingRule(comparisonOperator, formula1);
			var fill = rule.CreatePatternFormatting();
			var fontColorIndex = ToHSSFColorIndex(fontColor);
			if (fontColorIndex != null)
			{
				IFontFormatting font = rule.CreateFontFormatting();
				font.SetFontStyle(false,true);
				font.FontColorIndex = (short)fontColorIndex;
			}

			short? bgColorIndex = ToHSSFColorIndex(bgColor);
			if (bgColorIndex != null)
			{
				fill.FillBackgroundColor = (short) bgColorIndex;
				fill.FillPattern = (short) FillPattern.SolidForeground;
			}

//			 fill.FillForegroundColor = (short) fgColorIndex;
			
//			fill.FillPattern = fontColorIndex == null ? (short) FillPattern.SolidForeground : (short) FillPattern.Bricks;
			sheetCf.AddConditionalFormatting(my_data_range, rule);




			return this;
		}

		private static Color ToWpfColor(XSSFColor color)
		{
			if (color == null)
				throw new ArgumentNullException("color");

			var argb = color.GetARgb();
			return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
		}
				private static NPOI.SS.UserModel.ComparisonOperator ToExcelOperator(
			Ecng.ComponentModel.ComparisonOperator comparison)
		{
			switch (comparison)
			{
				case ComponentModel.ComparisonOperator.Equal:
					return ComparisonOperator.Equal;
				case ComponentModel.ComparisonOperator.NotEqual:
					return ComparisonOperator.NotEqual;
				case ComponentModel.ComparisonOperator.Greater:
					return ComparisonOperator.GreaterThan;
				case ComponentModel.ComparisonOperator.GreaterOrEqual:
					return ComparisonOperator.GreaterThanOrEqual;
				case ComponentModel.ComparisonOperator.Less:
					return ComparisonOperator.LessThan;
				case ComponentModel.ComparisonOperator.LessOrEqual:
					return ComparisonOperator.LessThanOrEqual;
				case ComponentModel.ComparisonOperator.Any:
					return ComparisonOperator.NoComparison;
				default:
					throw new ArgumentOutOfRangeException("comparison");
			}
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
			cellFont.FontHeight = (short) font.Size;

			return this;
		}

		public ExcelWorker SetStyle(int col, System.Type dataType)
		{
			return this.SetStyle(col, GetDefaultDataFormat(dataType));
		}

		public ExcelWorker SetStyle(int col, string dataFormat)
		{
			if (!_cellStyleCache.ContainsKey(dataFormat))
			{
				var style = _currentSheet.Workbook.CreateCellStyle();

				// check if this is a built-in format
				// TODO
				var builtinFormatId = (short) -1; //XSSFDataFormat.GetBuiltinFormat(dataFormat);

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

		private static string GetDefaultDataFormat(System.Type dataType)
		{
			if (dataType == null)
			{
				return "General";
			}
			switch (System.Type.GetTypeCode(dataType))
			{
				case System.TypeCode.Empty:
				case System.TypeCode.Object:
				case System.TypeCode.DBNull:
				case System.TypeCode.Char:
				case System.TypeCode.String:
					return "text";

				case System.TypeCode.Boolean:
					return "[=0]\"Yes\";[=1]\"No\"";

				case System.TypeCode.SByte:
				case System.TypeCode.Byte:
				case System.TypeCode.Int16:
				case System.TypeCode.UInt16:
				case System.TypeCode.Int32:
				case System.TypeCode.UInt32:
				case System.TypeCode.Int64:
				case System.TypeCode.UInt64:
					return "0";

				case System.TypeCode.Single:
				case System.TypeCode.Double:
				case System.TypeCode.Decimal:
					return "0.00";

				case System.TypeCode.DateTime:
					return "yyyy.MM.dd HH:mm";
			}
			throw new System.ArgumentOutOfRangeException();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public ExcelWorker SetCell(int col,int row,object value)
		{
			var cell = InternalGetCell(col,row);

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
			ICell cell = InternalGetCell(col, row);
			cell.CellFormula = formula;
			return this;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns></returns>
		public T GetCell<T>(int col,int row)
		{
			var cell = InternalGetCell(col,row,false);

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
					case CellType.Numeric:
						return cell.NumericCellValue.To<T>();
					case CellType.String:
						return cell.StringCellValue.To<T>();
					case CellType.Formula:
						return cell.CellFormula.To<T>();
					case CellType.Blank:
						return default(T);
					case CellType.Boolean:
						return cell.BooleanCellValue.To<T>();
					case CellType.Error:
						return cell.ErrorCellValue.To<T>();
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public ICell InternalGetCell(int colInd,int rowInd,bool createIfNotExists = true)
		{
			if (colInd < 0)
				throw new ArgumentOutOfRangeException("colInd",colInd,"Column index must be greater than zero.");

			if (rowInd < 0)
				throw new ArgumentOutOfRangeException("rowInd",rowInd,"Row index must be greater than zero.");

			var rowCount = GetRowsCount();

			if (rowInd >= rowCount)
			{
				if (!createIfNotExists)
					throw new ArgumentOutOfRangeException("rowInd",rowInd,
						"Row count {0} isn't enought for operation.".Put(rowCount));

				for (var i = rowCount;i <= rowInd;i++)
					_currentSheet.CreateRow(i);
			}
			var row = _currentSheet.GetRow(rowInd);
			var cellIndecies = _indecies.SafeAdd(rowInd,key => new HashSet<int>(row.Cells.Select(c => c.ColumnIndex)));

			if (!cellIndecies.Contains(colInd))
			{
				if (!createIfNotExists)
					throw new ArgumentOutOfRangeException("colInd",colInd,
						"Cell count {0} isn't enought for operation.".Put(cellIndecies.Count));
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
		public ExcelWorker Save(string fileName, bool autoSizeColumns)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

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
		public ExcelWorker Open()
		{
			Process.Start(_fileName);
			return this;
		}

		public ExcelWorker Protect(string password)
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
			Nuneric1,

			/// <summary>
			/// "0.00", //2 Числовой с 2мя знаками после запятой   2000000,35 -2000000,35
			/// </summary>
			Nuneric2,

			/// <summary>
			/// "#,##0", //3 Числовой с разделителями  2 000 000  -2 000 000
			/// </summary>
			Nuneric3,

			/// <summary>
			/// //4 Числовой с разделителями с 2мя знаками после запятой   2 000 000,35   -2 000 000,35
			/// </summary>
			Nuneric4,

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
			Defoult
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