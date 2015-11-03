using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Ecng.Common;
using Ecng.Interop;

//using NPOI.HSSF.UserModel;
//using NPOI.HSSF.Util;
//using NPOI.SS.UserModel;
//using NPOI.SS.Util;
//using NPOI.XSSF.Model;
//using NPOI.XSSF.UserModel;
//using NPOI.XSSF.UserModel.Extensions;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.Util;
using Color = System.Drawing.Color;
using ComparisonOperator = Ecng.ComponentModel.ComparisonOperator;

using Ecng.Interop;
//using Color = System.Windows.Media.Color;

namespace ExcelExporterNPOIMyIS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private ExcelWorkerMyis worker;
        private string path = @"";
//        private string path = @"C:\Users\Dell\Desktop\";


        private int _indexRow;

        private void Click_SheetConditionalFormatting(object sender, RoutedEventArgs e)
        {
            worker = new ExcelWorkerMyis();
            worker.AddSheet("Same Cell");
            worker.SwitchSheet("Same Cell");

            worker.RemoveSheet("Sheet0");
//            _indexRow = 0;

            //Conditional Formatting
//            worker.SetCell1("Conditional Formatting A2>4").NextCol();
            worker.SetCell1(0, _indexRow, "Conditional Formatting A2>4");
//            _indexRow++;
            AddData(worker);
            worker.SetConditionalFormatting(0, 10, 0, 10, HSSFColor.Blue.Index, @"A2>4");


            //Conditional Formatting with between values.
            worker.AddSheet(@"Second");
            worker.SwitchSheet(@"Second");
            _indexRow = 0;
            worker.SetCell1(0, _indexRow, "Conditional Formatting Between 10-50");
            _indexRow++;
            AddData(worker);
            worker.SetConditionalFormatting(0, 10, 0, 10, HSSFColor.Brown.Index, @"10", "50",
                NPOI.SS.UserModel.ComparisonOperator.Between, true);
            worker.Save(path + "1.xlsx", true);
            Process.Start(path + "1.xlsx");
        }

        private void Click_GroupColumnsAndRows2(object sender, RoutedEventArgs e)
        {
//            _indexRow = 0;
            worker = new ExcelWorkerMyis();
            AddData(worker);
            worker.GroupCloumns(0, 1);
            worker.GroupRows(0, 1);
            worker.Save(path + "2.xlsx", true);
            Process.Start(path + "2.xlsx");
        }

        private void Click_SetBackGroudColor3(object sender, RoutedEventArgs e)
        {
            _indexRow = 0;
            worker = new ExcelWorkerMyis();
            worker.SetCell1(0, _indexRow, "ColorBlue")
                .SetBackGroundColor(_indexRow, 0, NPOI.HSSF.Util.HSSFColor.Blue.Index);
            _indexRow++;

            AddData(worker);

            worker.Save(path + "3.xlsx", true);
            Process.Start(path + "3.xlsx");
        }

        private void Click_Copy_Row_Cell4(object sender, RoutedEventArgs e)
        {
            _indexRow = 0;
            worker = new ExcelWorkerMyis();
            AddData(worker);

            worker.SetCell1(0, 5, "Copied raw");
            worker = worker.CopyRow(0, 6);

            worker.SetCell1(5, 0, "Copied cell");
            worker = worker.CopyCell(0, 1, 6);


            worker.Save(path + "4.xlsx", true);
            Process.Start(path + "4.xlsx");
        }

        private void Click_Copy_Rename_Switch_Sheet5(object sender, RoutedEventArgs e)
        {
            _indexRow = 0;
            worker = new ExcelWorkerMyis();
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
            worker = new ExcelWorkerMyis();
            DateTime dt = DateTime.Now;


            worker.SetCell1(_indexRow, 1, "DateTime").SetBackGroundColor(_indexRow, 1,HSSFColor.Red.Index); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Date1); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Date2); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Date3); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time1); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time2); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time3); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time4); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time5); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.UniversalText); _indexRow++;


            
            worker. SetCell1(_indexRow, 1, "DateTime").SetBackGroundColor(_indexRow, 1,HSSFColor.Red.Index); _indexRow++;
//            worker.SetCell1(_indexRow, 1, "DateTime").SetBackGroundColor(_indexRow, 1,HSSFColor.Red.Index); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Date1); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Date2); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Date3); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time1); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time2); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time3); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time4); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.Time5); _indexRow++;
            worker.SetCell1(_indexRow, 1, dt, ExcelWorkerMyis.TypeDateTimeXLS.UniversalText); _indexRow++;

            _indexRow = 0;
            int nom = 321412412;
            worker.SetCell1(_indexRow, 2, "Nomber").SetBackGroundColor(_indexRow, 2, HSSFColor.Red.Index); ; _indexRow++;
            worker.SetCell1(_indexRow, 2, nom) ; _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Exponential); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Fractional1); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Fractional2); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Money1); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Money2); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Money3); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Money4); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Nuneric1); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Nuneric2); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Nuneric3); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Nuneric4); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Percentage1); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.Percentage2); _indexRow++;
            worker.SetCell1(_indexRow, 2, nom, ExcelWorkerMyis.TypeNumberXLS.UniversalText); _indexRow++;
         
            worker.Save(path + "6.xlsx", true);
            Process.Start(path + "6.xlsx");
        }

        private void Click_SetFormulas7(object sender, RoutedEventArgs e)
        {
            _indexRow = 0;
            worker = new ExcelWorkerMyis();
            AddData(worker);

            worker.SetCell1(3, 0, "Formula");
            worker.SetFormula(3, 1, "B4+A4+A2+B2");

            worker.SetFormula(4, 1, string.Format("{0}4 + {1}4", ExcelWorkerMyis.ConvertNumToColString(1), ExcelWorkerMyis.ConvertNumToColString(0)));// == B4+A4


            worker.Save(path + "7.xlsx", true);
            Process.Start(path + "7.xlsx");
        }

        private void Click_AddHyperLink8(object sender, RoutedEventArgs e)
        {
            worker = new ExcelWorkerMyis();
            _indexRow = 0;
            worker.SetCell1(0, _indexRow, "HyperLink").AddHyperLink(0, _indexRow, "http://poi.apache.org/");
            worker.Save(path + "8.xlsx", true);
            Process.Start(path + "8.xlsx");
        }

        private void Click_SetWidthAndHeigh_Aligment_Merge_Cell(object sender, RoutedEventArgs e)
        {
            worker = new ExcelWorkerMyis();
            _indexRow = 0;

            AddData(worker);
            worker.SetWidthAndHeight(0, 1, 2000, 3000);
            worker.SetWidthAndHeight(1, 2, 200, 3000);

            worker.SetAligmentCell(0, 1, NPOI.SS.UserModel.VerticalAlignment.Center,
                NPOI.SS.UserModel.HorizontalAlignment.Right);



            worker.MergeCells(5, 7, 0, 5);
            worker.SetCell1(5, 0, "MergedCell");
            worker.SetAligmentCell(5,7, NPOI.SS.UserModel.VerticalAlignment.Center,
                NPOI.SS.UserModel.HorizontalAlignment.Right);

            _indexRow = 0;

            worker.Save(path + "9.xlsx",false);
            Process.Start(path + "9.xlsx");
        }


        private void Click_Aligment_Merge_Cell(object sender, RoutedEventArgs e)
        {
            worker = new ExcelWorkerMyis();
            _indexRow = 0;
            
            worker.MergeCells(5, 7, 0, 5);
            worker.SetCell1(0, 5, "MergedCell");
            worker.SetAligmentCell(0, 5, NPOI.SS.UserModel.VerticalAlignment.Center,
                NPOI.SS.UserModel.HorizontalAlignment.Right);
            
            _indexRow = 0;

            worker.Save(path + "10.xlsx", false);
            Process.Start(path + "10.xlsx");
        }

        private void AddData(ExcelWorkerMyis excelWorkerMyis)
        {
            excelWorkerMyis.SetCell1(0, _indexRow, -5);
            excelWorkerMyis.SetCell1(1, _indexRow, 1);
            _indexRow ++;

            excelWorkerMyis.SetCell1(0, _indexRow, 3);
            excelWorkerMyis.SetCell1(1, _indexRow, 10);
            _indexRow ++;

            excelWorkerMyis.SetCell1(0, _indexRow, 8);
            excelWorkerMyis.SetCell1(1, _indexRow, 6);
            _indexRow ++;

            excelWorkerMyis.SetCell1(0, _indexRow, 54);
            excelWorkerMyis.SetCell1(1, _indexRow, 12);
            _indexRow ++;
        }

        private void AddTitle(ExcelWorkerMyis excelWorkerMyis)
        {
            excelWorkerMyis.SetCell1(0, _indexRow, "Example");
            excelWorkerMyis.SetCell1(1, _indexRow, "Example");
            _indexRow += 2;
        }

        private void Click_ImportCells(object sender, RoutedEventArgs e)
        {
            worker = new ExcelWorkerMyis();
            worker.importXLS(path + "Book1.xls");
        }
    }
}
