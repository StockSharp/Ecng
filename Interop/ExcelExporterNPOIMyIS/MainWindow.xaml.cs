using System.Collections.Generic;
using System.Windows.Documents;

namespace ExcelExporterNPOIMyIS
{
	using System;
	using System.Diagnostics;
	using System.Windows;
	using Ecng.Interop;
	using NPOI.HSSF.Util;
	using NPOI.SS.Util;

	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private ExcelWorker worker;
		private string path = @"";


		private int _indexRow;

		private void Click_SheetConditionalFormatting(object sender, RoutedEventArgs e)
		{
			worker = new ExcelWorker();
			_indexRow = 0;
			//Conditional Formatting
			worker.SetCell(0, _indexRow, "Conditional Formatting A2>4");
			AddData(worker);
			worker.SetConditionalFormatting(0, 2, 0, 4, HSSFColor.Blue.Index, @"A2>4");

			//Conditional Formatting with between values.
			_indexRow ++;
			worker.SetCell(0, _indexRow, "Conditional Formatting Between 10-50");
//			_indexRow++;
			AddData(worker);
			worker.SetConditionalFormatting(0,2,5,9,HSSFColor.Brown.Index,@"10","50",
				NPOI.SS.UserModel.ComparisonOperator.Between, isUseComparision: true);
			worker.Save(path + "1.xlsx", true);
			Process.Start(path + "1.xlsx");
		}

		private void Click_GroupColumnsAndRows2(object sender, RoutedEventArgs e)
		{
			worker = new ExcelWorker();
			AddData(worker);
			worker.GroupCloumns(0, 1);
			worker.GroupRows(0, 1);
			worker.Save(path + "2.xlsx", true);
			Process.Start(path + "2.xlsx");
		}

		private void Click_SetBackGroudColor3(object sender, RoutedEventArgs e)
		{
			_indexRow = 0;
			worker = new ExcelWorker();
			worker.SetCell(0, _indexRow, "ColorBlue")
				.SetBackGroundColor(_indexRow, 0, NPOI.HSSF.Util.HSSFColor.Blue.Index);
			_indexRow++;

			AddData(worker);

			worker.Save(path + "3.xlsx", true);
			Process.Start(path + "3.xlsx");
		}

		private void Click_Copy_Row_Cell4(object sender, RoutedEventArgs e)
		{
			_indexRow = 0;
			worker = new ExcelWorker();
			AddData(worker);

			worker.SetCell(0, 5, "Copied raw");
			worker = worker.CopyRow(0, 6);

			worker.SetCell(5, 0, "Copied cell");
			worker = worker.CopyCell(0, 1, 6);

			worker.Save(path + "4.xlsx", true);
			Process.Start(path + "4.xlsx");
		}

		private void Click_Copy_Rename_Switch_Sheet5(object sender, RoutedEventArgs e)
		{
			_indexRow = 0;
			worker = new ExcelWorker();
			worker.CopySheet(@"CopiedSheet");
			worker.CopySheet(@"CopiedSheet2");
			worker.RenameSheet(@"RenamedSheet");
			AddData(worker);

			worker.SwitchSheet("CopiedSheet");
			AddData(worker);

			worker.Save(path + "5.xlsx", true);
			Process.Start(path + "5.xlsx");
		}

		private void Click_SetDateTime6(object sender, RoutedEventArgs e)
		{
			_indexRow = 0;
			worker = new ExcelWorker();
			DateTime dt = DateTime.Now;

			worker.SetCell(_indexRow, 1, "DateTime").SetBackGroundColor(_indexRow, 1, HSSFColor.Red.Index);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Date1);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Date2);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Date3);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time1);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time2);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time3);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time4);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time5);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.UniversalText);
			_indexRow++;

			worker.SetCell(_indexRow, 1, "DateTime").SetBackGroundColor(_indexRow, 1, HSSFColor.Red.Index);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Date1);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Date2);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Date3);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time1);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time2);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time3);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time4);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.Time5);
			_indexRow++;
			worker.SetCell(_indexRow, 1, dt, ExcelWorker.DataFormat.UniversalText);
			_indexRow++;

			_indexRow = 0;
			int nom = 321412412;
			worker.SetCell(_indexRow, 2, "Nomber").SetBackGroundColor(_indexRow, 2, HSSFColor.Red.Index);
			;
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Exponential);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Fractional1);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Fractional2);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Money1);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Money2);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Money3);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Money4);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Nuneric1);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Nuneric2);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Nuneric3);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Nuneric4);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Percentage1);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.Percentage2);
			_indexRow++;
			worker.SetCell(_indexRow, 2, nom, ExcelWorker.DataFormat.UniversalText);
			_indexRow++;

			worker.Save(path + "6.xlsx", true);
			Process.Start(path + "6.xlsx");
		}

		private void Click_SetFormulas7(object sender, RoutedEventArgs e)
		{
			_indexRow = 0;
			worker = new ExcelWorker();
			AddData(worker);
			worker.SetCell(3, 0, "Formula");
			worker.SetFormula(3, 1, "B4+A4+A2+B2");
			worker.SetFormula(4, 1,
				string.Format("{0}4 + {1}4", CellReference.ConvertNumToColString(1), CellReference.ConvertNumToColString(0)));
			// == B4+A4
			worker.Save(path + "7.xlsx", true);
			Process.Start(path + "7.xlsx");
		}

		private void Click_AddHyperLink8(object sender, RoutedEventArgs e)
		{
			worker = new ExcelWorker();
			_indexRow = 0;
			worker.SetCell(0, _indexRow, "HyperLink").AddHyperLink(0, _indexRow, "http://poi.apache.org/");
			worker.Save(path + "8.xlsx", true);
			Process.Start(path + "8.xlsx");
		}

		private void Click_SetWidthAndHeigh_Aligment_Merge_Cell(object sender, RoutedEventArgs e)
		{
			worker = new ExcelWorker();
			_indexRow = 0;

			AddData(worker);
			worker.SetWidthAndHeight(0, 1, 2000, 3000);
			worker.SetWidthAndHeight(1, 2, 200, 3000);

			worker.SetAligmentCell(0, 1, NPOI.SS.UserModel.VerticalAlignment.Center,
				NPOI.SS.UserModel.HorizontalAlignment.Right);

			worker.MergeCells(5, 7, 0, 5);
			worker.SetCell(5, 0, "MergedCell");
			worker.SetAligmentCell(5, 7, NPOI.SS.UserModel.VerticalAlignment.Center,
				NPOI.SS.UserModel.HorizontalAlignment.Right);

			_indexRow = 0;

			worker.Save(path + "9.xlsx", false);
			Process.Start(path + "9.xlsx");
		}

		private void Click_Aligment_Merge_Cell(object sender, RoutedEventArgs e)
		{
			worker = new ExcelWorker();
			_indexRow = 0;

			worker.MergeCells(5, 7, 0, 5);
			worker.SetCell(0, 5, "MergedCell");
			worker.SetAligmentCell(0, 5, NPOI.SS.UserModel.VerticalAlignment.Center,
				NPOI.SS.UserModel.HorizontalAlignment.Right);

			_indexRow = 0;

			worker.Save(path + "10.xlsx", false);
			Process.Start(path + "10.xlsx");
		}

		private void AddData(ExcelWorker excelWorker)
		{
			excelWorker.SetCell(0, _indexRow, -5);
			excelWorker.SetCell(1, _indexRow, 1);
			_indexRow ++;

			excelWorker.SetCell(0, _indexRow, 3);
			excelWorker.SetCell(1, _indexRow, 10);
			_indexRow ++;

			excelWorker.SetCell(0, _indexRow, 8);
			excelWorker.SetCell(1, _indexRow, 6);
			_indexRow ++;

			excelWorker.SetCell(0, _indexRow, 54);
			excelWorker.SetCell(1, _indexRow, 12);
			_indexRow ++;
		}

		private void Click_ImportCells(object sender, RoutedEventArgs e)
		{
			worker = new ExcelWorker();
			worker = worker.importXLS(path + "Book1.xls");
			worker.SwitchSheet("Sheet1");
			DateTime str = worker.GetCell<DateTime>(0,1);
			IEnumerable<object> cols = worker.GetColumn<object>(0);
			string str1 = "First column   "  + str.ToString();
			foreach (var col in cols) str1 += ";" + col;
			MessageBox.Show(str1);
		}
	}
}
