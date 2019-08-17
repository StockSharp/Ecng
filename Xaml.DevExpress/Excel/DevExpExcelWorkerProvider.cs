namespace Ecng.Xaml.DevExp.Excel
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Windows.Media;

	using DevExpress.Export.Xl;

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Interop;

	using MoreLinq;

	public class DevExpExcelWorkerProvider : IExcelWorkerProvider
	{
		private class DevExpExcelWorker : IExcelWorker
		{
			private readonly IXlExporter _exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);
			private readonly IXlDocument _document;
			private readonly PairSet<string, IXlSheet> _sheets = new PairSet<string, IXlSheet>(StringComparer.InvariantCultureIgnoreCase);
			private IXlSheet _currSheet;

			public DevExpExcelWorker(Stream stream)
			{
				_document = _exporter.CreateDocument(stream);
			}

			void IDisposable.Dispose()
			{
				_sheets.Values.ForEach(s => s.Dispose());
				_sheets.Clear();
				_document.Dispose();
			}

			IExcelWorker IExcelWorker.SetCell<T>(int col, int row, T value)
			{
				throw new NotImplementedException();
			}

			T IExcelWorker.GetCell<T>(int col, int row)
			{
				throw new NotImplementedException();
			}

			IExcelWorker IExcelWorker.SetStyle(int col, Type type)
			{
				throw new NotImplementedException();
			}

			IExcelWorker IExcelWorker.SetStyle(int col, string format)
			{
				throw new NotImplementedException();
			}

			IExcelWorker IExcelWorker.SetConditionalFormatting(int col, ComparisonOperator op, string condition, Color? bgColor, Color? fgColor)
			{
				throw new NotImplementedException();
			}

			IExcelWorker IExcelWorker.RenameSheet(string name)
			{
				_sheets.RemoveByValue(_currSheet);
				_currSheet.Name = name;
				_sheets.Add(name, _currSheet);
				return this;
			}

			IExcelWorker IExcelWorker.AddSheet()
			{
				_currSheet = _document.CreateSheet();
				return this;
			}

			bool IExcelWorker.ContainsSheet(string name)
			{
				return _sheets.ContainsKey(name);
			}

			IExcelWorker IExcelWorker.SwitchSheet(string name)
			{
				_currSheet = _sheets[name];
				return this;
			}

			int IExcelWorker.GetColumnsCount()
			{
				return _currSheet.ColumnRange.BottomRight.Column;
			}

			int IExcelWorker.GetRowsCount()
			{
				return _currSheet.ColumnRange.BottomRight.Row;
			}
		}

		IExcelWorker IExcelWorkerProvider.CreateNew(Stream stream, bool readOnly)
		{
			return new DevExpExcelWorker(stream);
		}

		IExcelWorker IExcelWorkerProvider.OpenExist(Stream stream)
		{
			return new DevExpExcelWorker(stream);
		}
	}
}