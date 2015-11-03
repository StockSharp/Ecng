

using System.Text;
using System.Windows.Markup;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
// NPOI.XSSF.UserModel


namespace Ecng.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Color = System.Windows.Media.Color;

    using Ecng.Common;
    using Ecng.Collections;

    using ICellStyle = NPOI.SS.UserModel;
    //      using    NPOI.XSSF.UserModel = XSSFWorkbook;
    using NPOI.XSSF.UserModel;
    using NPOI.SS.UserModel;
    using NPOI.SS.Util;

    using NPOI.HSSF.Util;

    public partial class ExcelWorkerMyis : Disposable
    {
        #region Import

        public void importXLS(string path)
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

            _indecies1.Clear();

            if (Workbook.NumberOfSheets > 0)

                for (int i = 0; i < Workbook.NumberOfSheets; i++)
                {
                    _currentSheet = Workbook.GetSheetAt(i);
                    _indecies1.Add(_currentSheet.SheetName,
                        new Dictionary<int, HashSet<int>>());
                    System.Collections.IEnumerator rows = _currentSheet.GetRowEnumerator();

                    while (rows.MoveNext())
                    {
                        IRow row = (IRow)rows.Current;

                        for (int i1 = 0; i1 < row.LastCellNum; i1++)
                        {
                           var ccel = InternalGetCell1(i1, i);
                        }
                    }
                }
        }

        #endregion

        #region  Myis


        public static String ConvertNumToColString(int col)
        {
            // Excel counts column A as the 1st column, we
            //  treat it as the 0th one
            int excelColNum = col + 1;

            StringBuilder colRef = new StringBuilder(2);
            int colRemain = excelColNum;

            while (colRemain > 0)
            {
                int thisPart = colRemain % 26;
                if (thisPart == 0) { thisPart = 26; }
                colRemain = (colRemain - thisPart) / 26;

                // The letter A is at 65
                char colChar = (char)(thisPart + 64);
                colRef.Insert(0, colChar);
            }

            return colRef.ToString();
        }


        public ExcelWorkerMyis MergeCells(int firstRow,int lastRow,int firstCol, int lastCol,bool isborder = false)
        {
            var region = new CellRangeAddress(firstRow, lastRow, firstCol, lastCol);
            _currentSheet.AddMergedRegion(region);
            if (isborder)
                ((HSSFSheet) _currentSheet).SetEnclosedBorderOfRegion(region, BorderStyle.Dotted,
                    NPOI.HSSF.Util.HSSFColor.Red.Index);
            return this;
        }

//        public ExcelWorkerMyis ProtectCell(int colInd, int rowInd)
//        {
//            var ccel = InternalGetCell1(colInd, rowInd);
//            var style = Workbook.CreateCellStyle();
//            style.IsLocked = true;
//            ccel.CellStyle = style;
//            return this;
//        }

        public ExcelWorkerMyis SetAligmentCell(int colInd, int rowInd, VerticalAlignment verAligment = VerticalAlignment.Center, HorizontalAlignment horAligment = HorizontalAlignment.Center)
        {
            var ccel = InternalGetCell1(colInd, rowInd);
            var style = Workbook.CreateCellStyle();
            style.VerticalAlignment = verAligment;
            style.Alignment = horAligment;
            ccel.CellStyle = style;
            return this;
        }

        public ExcelWorkerMyis SetWidthAndHeight(int colInd, int rowInd, int widthCol, short heighRow)
        {
            InternalGetCell1(colInd, rowInd);
            _currentSheet.SetColumnWidth(colInd, widthCol);
            _currentSheet.GetRow(rowInd).Height = heighRow;
            return this;
    }


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

        public ICell InternalGetCell1(int colInd, int rowInd, bool createIfNotExists = true) //+++
        //        private ICell InternalGetCell(int colInd, int rowInd, bool createIfNotExists = true)
        {
            if (colInd < 0)
                throw new ArgumentOutOfRangeException("colInd", colInd, "Column index must be greater than zero.");

            if (rowInd < 0)
                throw new ArgumentOutOfRangeException("rowInd", rowInd, "Row index must be greater than zero.");

            var rowCount = GetRowsCount();

            if (rowInd >= rowCount)
            {
                if (!createIfNotExists)
                    throw new ArgumentOutOfRangeException("rowInd", rowInd,
                        "Row count {0} isn't enought for operation.".Put(rowCount));

                for (var i = rowCount; i <= rowInd; i++)
                    _currentSheet.CreateRow(i);
            }

            var row = _currentSheet.GetRow(rowInd);

            //var cellCount = row.FirstCellNum == -1 ? 0 : (row.LastCellNum - row.FirstCellNum) + 1;

            var cellIndecies = _indecies1[_currentSheet.SheetName].SafeAdd(rowInd,
                key => new HashSet<int>(row.Cells.Select(c => c.ColumnIndex)));

            if (!cellIndecies.Contains(colInd))
            {
                if (!createIfNotExists)
                    throw new ArgumentOutOfRangeException("colInd", colInd,
                        "Cell count {0} isn't enought for operation.".Put(cellIndecies.Count));

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





        #region Old

        public enum TypeNumberXLS
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
            /// "@", //35 Текстовый 2000000,345 -2000000,345
            /// </summary>
            UniversalText
        }

        public enum TypeDateTimeXLS
        {
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
        }

        public ExcelWorkerMyis AddHyperLink(int col, int row, string value, short HSSFColorIndex = HSSFColor.Blue.Index)
        {
            var hlink_style = Workbook.CreateCellStyle();
            IFont hlink_font = Workbook.CreateFont();
            hlink_font.Underline = FontUnderlineType.Single;
            hlink_font.Color = HSSFColorIndex;
            hlink_style.SetFont(hlink_font);

            var cell = InternalGetCell1(col, row);

            XSSFHyperlink link = new XSSFHyperlink(HyperlinkType.Url);
            link.Address = (value);

            cell.Hyperlink = link;
            cell.CellStyle = hlink_style;
            return this;
        }



        public ExcelWorkerMyis SetCell1(int col, int row, string value)
        {
            var cell = InternalGetCell1(col, row);
            var cStyle = Workbook.CreateCellStyle();
            cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat("@");
            cell.CellStyle = cStyle;
            cell.SetCellValue(value);
            return this;
        }

        public ExcelWorkerMyis SetCell1(int col, int row, int value,
            TypeNumberXLS typeNumberXLS = TypeNumberXLS.UniversalText)
        {

            SetCell1(col, row, (double) value, typeNumberXLS);
            return this;
        }

        public ExcelWorkerMyis SetCell1(int col, int row, double value,
            TypeNumberXLS typeNumberXLS = TypeNumberXLS.UniversalText)
        {
            string dataFormat = "";
            switch (typeNumberXLS)
            {
                case TypeNumberXLS.Nuneric1:
                {
                    dataFormat = "0";
                    break;
                }
                case TypeNumberXLS.Nuneric2:
                {
                    dataFormat = "0.00";
                    break;
                }
                case TypeNumberXLS.Nuneric3:
                {
                    dataFormat = "#,##0";
                    break;
                }
                case TypeNumberXLS.Nuneric4:
                {
                    dataFormat = "#,##0.00";
                    break;
                }
                case TypeNumberXLS.Money1:
                {
                    dataFormat = "\"$\"#,##0_);(\"$\"#,##0)";
                    break;
                }
                case TypeNumberXLS.Money2:
                {
                    dataFormat = "\"$\"#,##0_);[Red](\"$\"#,##0)";
                    break;
                }
                case TypeNumberXLS.Money3:
                {
                    dataFormat = "\"$\"#,##0.00_);(\"$\"#,##0.00)";
                    break;
                }
                case TypeNumberXLS.Money4:
                {
                    dataFormat = "\"$\"#,##0.00_);[Red](\"$\"#,##0.00)";
                    break;
                }
                case TypeNumberXLS.Fractional1:
                {
                    dataFormat = "# ?/?";
                    break;
                }
                case TypeNumberXLS.Fractional2:
                {
                    dataFormat = "# ??/??";
                    break;
                }
                case TypeNumberXLS.Exponential:
                {
                    dataFormat = "0.00E+00";
                    break;
                }
                case TypeNumberXLS.Percentage1:
                {
                    dataFormat = "0%";
                    break;
                }
                case TypeNumberXLS.Percentage2:
                {
                    dataFormat = "0.00%";
                    break;
                }
                case TypeNumberXLS.UniversalText:
                {
                    dataFormat = "@";
                    break;
                }
            }
            var cell = InternalGetCell1(col, row);
            var cStyle = Workbook.CreateCellStyle();
            cStyle.DataFormat = HSSFDataFormat.GetBuiltinFormat(dataFormat);
            cell.CellStyle = cStyle;
            cell.SetCellValue(value);
            return this;
        }

        public ExcelWorkerMyis SetCell1(int col, int row, TimeSpan value,
            TypeDateTimeXLS TypeDateTimeXLS = TypeDateTimeXLS.UniversalText)
        {
            return SetCell1(col, row, DateTime.Today + value, TypeDateTimeXLS);
        }

        public ExcelWorkerMyis SetCell1(int col, int row, DateTime value,
            TypeDateTimeXLS TypeDateTimeXLS = TypeDateTimeXLS.DateTime)
        {
            string dataFormat = "";
            switch (TypeDateTimeXLS)
            {
                case TypeDateTimeXLS.Date1:
                    {
                        dataFormat = "m/d/yy";
                        break;
                    }
                case TypeDateTimeXLS.Date2:
                    {
                        dataFormat = "d-mmm-yy";
                        break;
                    }
                case TypeDateTimeXLS.Date3:
                    {
                        dataFormat = "d-mmm";
                        break;
                    }
                case TypeDateTimeXLS.Time1:
                    {
                        dataFormat = "mmm-yy";
                        break;
                    }
                case TypeDateTimeXLS.Time2:
                    {
                        dataFormat = "h:mm AM/PM";
                        break;
                    }
                case TypeDateTimeXLS.Time3:
                    {
                        dataFormat = "h:mm:ss AM/PM";
                        break;
                    }
                case TypeDateTimeXLS.Time4:
                    {
                        dataFormat = "h:mm";
                        break;
                    }
                case TypeDateTimeXLS.Time5:
                    {
                        dataFormat = "h:mm:ss";
                        break;
                    }
                case TypeDateTimeXLS.DateTime:
                    {
                        dataFormat = "m/d/yy h:mm";
                        break;
                    }
                case TypeDateTimeXLS.UniversalText:
                    {
                        dataFormat = "@";
                        break;
                    }
            }

            var cell = InternalGetCell1(col, row);
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
        /// <param name="HSSFColorIndex"></param>
        /// <param name="FillPatternSolidForeground"></param>
        /// <returns></returns>
        public ExcelWorkerMyis SetBackGroundColor(int col, int row, short HSSFColorIndex,
            FillPattern FillPatternSolidForeground = FillPattern.SolidForeground)
        {


            var style = Workbook.CreateCellStyle();
            style.FillBackgroundColor = HSSFColorIndex;
            style.FillForegroundColor = HSSFColorIndex;
            style.FillPattern = FillPatternSolidForeground;
            var cell = InternalGetCell1(col, row);
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
        public ExcelWorkerMyis GroupCloumns(int from, int to)
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
        public ExcelWorkerMyis GroupRows(int from, int to)
        {
            _currentSheet.GroupRow(from, to);
            return this;
        }


        #endregion


        /// <summary>
        /// Auto Sizing columns
        /// </summary>
        /// <param name="column">Nomber of column</param>
        /// <param name="useMergetCells">whether to use the contents of merged cells when 
        /// calculating the width of the column . defoult is false</param> 
        /// <returns></returns>
        public ExcelWorkerMyis AutoSizeColumn(int column, bool useMergetCells = false)
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
        public ExcelWorkerMyis CopySheet(string name, bool copyStyle = true)
        {
            _currentSheet.CopySheet(name, copyStyle);
            if (!_indecies1.ContainsKey(name))
                _indecies1.Add(name, new Dictionary<int, HashSet<int>>());

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        /// <returns></returns>
        public ExcelWorkerMyis CopyRow(int sourceIndex, int targetIndex)
        {
            _currentSheet.CopyRow(sourceIndex, targetIndex);
            return this;
        }

        public ExcelWorkerMyis CopyCell(int rowIndex, int colIndex, int targetIndex)
        {
            InternalGetCell1(colIndex, rowIndex);
            _currentSheet.GetRow(rowIndex).CopyCell(colIndex, targetIndex);
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstRow"></param>
        /// <param name="lastRow"></param>
        /// <param name="firstColumn"></param>
        /// <param name="lasColumn"></param>
        /// <param name="indexedColor"></param>
        /// <param name="formula1"></param>
        /// <param name="formula2"></param>
        /// <param name="comparisonOperator"></param>
        /// <param name="isUseComparision"></param>
        /// <returns></returns>
        public ExcelWorkerMyis SetConditionalFormatting(int firstRow, int lastRow, int firstColumn, int lasColumn
            , short indexedColor, string formula1, string formula2 = "",
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
            fill.FillBackgroundColor = indexedColor;
            fill.FillPattern = (short)FillPattern.SolidForeground;
            sheetCf.AddConditionalFormatting(my_data_range, rule);

            return this;
        }

        #endregion

        //private static readonly SynchronizedPairSet<Color, short> _colors = new SynchronizedPairSet<Color, short>();

        //static ExcelWorkerMyis()
        //{
        //	var colors = typeof(XSSFColor).GetValue<VoidType, XSSFColor[]>("GetAllColors", null);

        //	foreach (var color in colors)
        //	{
        //		var rgb = color.GetTriplet();
        //		_colors.Add(Color.FromRgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]), color.GetIndex());
        //	}
        //}

        public ISheet _currentSheet;
        //        public NPOI.SS.UserModel.ISheet _currentSheet;
        //        public ISheet _currentSheet;
        private string _fileName;
        private readonly Dictionary<int, HashSet<int>> _indecies = new Dictionary<int, HashSet<int>>();

        private readonly Dictionary<string, Dictionary<int, HashSet<int>>> _indecies1 =
            new Dictionary<string, Dictionary<int, HashSet<int>>>();

        private readonly Dictionary<string, NPOI.SS.UserModel.ICellStyle> _cellStyleCache =
            new Dictionary<string, NPOI.SS.UserModel.ICellStyle>();

        private FileStream _stream;

        /// <summary>
        /// Create <see cref="ExcelWorkerMyis"/>.
        /// </summary>
        public ExcelWorkerMyis()
        {
            Workbook = new XSSFWorkbook();
            //            Workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
            _currentSheet = Workbook.CreateSheet();
            _indecies1.Add(_currentSheet.SheetName, new Dictionary<int, HashSet<int>>()); //+++
        }

        /// <summary>
        /// Create <see cref="ExcelWorkerMyis"/>.
        /// </summary>
        /// <param name="name">Name of workbook.</param>
        /// <param name="readOnly"></param>
        public ExcelWorkerMyis(string name, bool readOnly = false)
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
        public ExcelWorkerMyis AddSheet(string sheetName)
        {
            if (ContainsSheet(sheetName))
                return this;

            var sheet = Workbook.CreateSheet(sheetName);
            if (!_indecies1.ContainsKey(sheetName))
                _indecies1.Add(sheetName, new Dictionary<int, HashSet<int>>()); //+++
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
        public ExcelWorkerMyis RemoveSheet()
        {
            ThrowIfCurrentSheetIsEmpty();

            Workbook.RemoveSheetAt(Workbook.GetSheetIndex(_currentSheet));
            if (_indecies1.ContainsKey(_currentSheet.SheetName)) _indecies1.Remove(_currentSheet.SheetName); //+++
            _currentSheet = null;

            return this;
        }

        /// <summary>
        /// Removes specified worksheet.
        /// </summary>
        /// <param name="sheetName">Name of the sheet to remove.</param>
        /// <returns>Exporter.</returns>
        public ExcelWorkerMyis RemoveSheet(string sheetName)
        {
            if (ContainsSheet(sheetName))
            {
                Workbook.RemoveSheetAt(Workbook.GetSheetIndex(sheetName));

                if (_indecies1.ContainsKey(sheetName)) _indecies1.Remove(sheetName);
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
        public ExcelWorkerMyis RenameSheet(string sheetName)
        {
            if (sheetName.IsEmpty())
                throw new ArgumentNullException("sheetName");
            //+++
            if (_indecies1.ContainsKey(_currentSheet.SheetName))
            {
                var val = _indecies1[_currentSheet.SheetName];
                _indecies1.Remove(_currentSheet.SheetName);
                _indecies1.Add(sheetName, val);
            }
            //+++
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
        public ExcelWorkerMyis SwitchSheet(string sheetName)
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
        public ExcelWorkerMyis SetWidth(int col, int width)
        {
            ThrowIfCurrentSheetIsEmpty();
            _currentSheet.SetColumnWidth(col, width);
            return this;
        }

        public ExcelWorkerMyis SetHeight(int row, short height)
        {
            ThrowIfCurrentSheetIsEmpty();
            _currentSheet.GetRow(row).Height = height;
            return this;
        }

        public ExcelWorkerMyis SetConditionalFormatting(int col, Ecng.ComponentModel.ComparisonOperator comparison,
            string formula1, Color? bgColor, Color? fgColor)
        {
            return SetConditionalFormatting(col, 0, col, ushort.MaxValue, comparison, formula1, formula1, bgColor,
                fgColor);
        }

        public ExcelWorkerMyis SetConditionalFormatting(int colStart, int rowStart, int colEnd, int rowEnd,
            Ecng.ComponentModel.ComparisonOperator comparison, string formula1, string formula2, Color? bgColor,
            Color? fgColor)
        {
            var hscf = _currentSheet.SheetConditionalFormatting;

            var rule = hscf.CreateConditionalFormattingRule(ToExcelOperator(comparison), formula1, formula2);

            // TODO
            //if (bgColor != null)
            //	rule.CreatePatternFormatting().FillBackgroundColor = _colors[(Color)bgColor];

            //if (fgColor != null)
            //	rule.CreateFontFormatting().FontColorIndex = _colors[(Color)fgColor];

            hscf.AddConditionalFormatting(
                new[] { new NPOI.SS.Util.CellRangeAddress(rowStart, rowEnd, colStart, colEnd) }, new[] { rule });

            return this;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public ExcelWorkerMyis SetBackColor(int col, int row, Color color)
        {
            var cell = InternalGetCell(col, row);
            //            _currentSheet. InternalGetCell
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
        public ExcelWorkerMyis SetForeColor(int col, int row, Color color)
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

        public IColor ToExcelColor(Color color) //+++
        //        private static IColor ToExcelColor(Color color)
        {
            return new XSSFColor(new[] { color.R, color.G, color.B });
        }

        private static Color ToWpfColor(XSSFColor color)
        {
            if (color == null)
                throw new ArgumentNullException("color");

            var argb = color.GetARgb();
            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public ExcelWorkerMyis SetFont(int col, int row, Font font)
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

        public ExcelWorkerMyis SetStyle(int col, Type dataType)
        {
            return SetStyle(col, GetDefaultDataFormat(dataType));
        }

        public ExcelWorkerMyis SetStyle(int col, string dataFormat)
        {
            if (!_cellStyleCache.ContainsKey(dataFormat))
            {
                var style = _currentSheet.Workbook.CreateCellStyle();

                // check if this is a built-in format
                // TODO
                var builtinFormatId = (short)-1; //XSSFDataFormat.GetBuiltinFormat(dataFormat);

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
        public ExcelWorkerMyis SetCell(int col, int row, object value)
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

        public ExcelWorkerMyis SetFormula(int col, int row, string formula)
        {
            ICell cell =  InternalGetCell1(col, row);
            cell.CellFormula = formula;
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

        public ICell InternalGetCell(int colInd, int rowInd, bool createIfNotExists = true) //+++
        //        private ICell InternalGetCell(int colInd, int rowInd, bool createIfNotExists = true)
        {
            if (colInd < 0)
                throw new ArgumentOutOfRangeException("colInd", colInd, "Column index must be greater than zero.");

            if (rowInd < 0)
                throw new ArgumentOutOfRangeException("rowInd", rowInd, "Row index must be greater than zero.");

            var rowCount = GetRowsCount();

            if (rowInd >= rowCount)
            {
                if (!createIfNotExists)
                    throw new ArgumentOutOfRangeException("rowInd", rowInd,
                        "Row count {0} isn't enought for operation.".Put(rowCount));

                for (var i = rowCount; i <= rowInd; i++)
                    _currentSheet.CreateRow(i);
            }

            var row = _currentSheet.GetRow(rowInd);

            //var cellCount = row.FirstCellNum == -1 ? 0 : (row.LastCellNum - row.FirstCellNum) + 1;

            var cellIndecies = _indecies.SafeAdd(rowInd, key => new HashSet<int>(row.Cells.Select(c => c.ColumnIndex)));

            if (!cellIndecies.Contains(colInd))
            {
                if (!createIfNotExists)
                    throw new ArgumentOutOfRangeException("colInd", colInd,
                        "Cell count {0} isn't enought for operation.".Put(cellIndecies.Count));

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
        /// <param name="autoSizeColumns"></param>
        /// <returns></returns>
        public ExcelWorkerMyis Save(string fileName, bool autoSizeColumns)
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
        public ExcelWorkerMyis Open()
        {
            Process.Start(_fileName);
            return this;
        }

        public ExcelWorkerMyis Protect(string password)
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
    }
}