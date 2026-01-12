namespace Ecng.Excel;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

using DevExpress.Export.Xl;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// Implementation of the <see cref="IExcelWorkerProvider"/> that works with DevExpress Excel processors.
/// </summary>
public class DevExpExcelWorkerProvider : IExcelWorkerProvider
{
	private class DevExpExcelWorker : IExcelWorker
	{
		private class CellData
		{
			public object Value { get; set; }
			public string Format { get; set; }
			public string BgColor { get; set; }
			public string FgColor { get; set; }
			public string HyperlinkUrl { get; set; }
			public string HyperlinkText { get; set; }
		}

		private class MergeRange
		{
			public int StartCol { get; set; }
			public int StartRow { get; set; }
			public int EndCol { get; set; }
			public int EndRow { get; set; }
		}

		private class SheetData(DevExpExcelWorker worker) : IDisposable
		{
			private readonly DevExpExcelWorker _worker = worker ?? throw new ArgumentNullException(nameof(worker));
			private readonly Dictionary<int, SortedDictionary<int, CellData>> _cells = [];

			public readonly SortedDictionary<int, (Type type, string format)> Columns = [];
			public readonly Dictionary<int, double> ColumnWidths = [];
			public readonly Dictionary<int, double> RowHeights = [];
			public readonly HashSet<int> AutoFitColumns = [];
			public readonly HashSet<int> Rows = [];
			public readonly List<MergeRange> MergeRanges = [];

			public string Name { get; set; }
			public int FreezeRowCount { get; set; }
			public int FreezeColCount { get; set; }

			public void SetCell<T>(int col, int row, T value)
			{
				Columns.TryAdd(col, new());
				Rows.Add(row);
				var cellData = GetOrCreateCellData(col, row);
				cellData.Value = value;
			}

			public T GetCell<T>(int col, int row)
			{
				var cellData = TryGetCellData(col, row);
				return cellData?.Value is T val ? val : default;
			}

			public void SetCellFormat(int col, int row, string format)
			{
				var cellData = GetOrCreateCellData(col, row);
				cellData.Format = format;
			}

			public void SetCellColor(int col, int row, string bgColor, string fgColor)
			{
				var cellData = GetOrCreateCellData(col, row);
				cellData.BgColor = bgColor;
				cellData.FgColor = fgColor;
			}

			public void SetHyperlink(int col, int row, string url, string text)
			{
				var cellData = GetOrCreateCellData(col, row);
				cellData.HyperlinkUrl = url;
				cellData.HyperlinkText = text;
			}

			private CellData GetOrCreateCellData(int col, int row)
			{
				Columns.TryAdd(col, new());
				Rows.Add(row);
				var rowDict = _cells.SafeAdd(row, key => []);
				if (!rowDict.TryGetValue(col, out var cellData))
				{
					cellData = new CellData();
					rowDict[col] = cellData;
				}
				return cellData;
			}

			private CellData TryGetCellData(int col, int row)
			{
				if (!_cells.TryGetValue(row, out var rowDict))
					return null;
				return rowDict.TryGetValue(col, out var cellData) ? cellData : null;
			}

			public void Dispose()
			{
				using (var sheet = _worker._document.CreateSheet())
				{
					if (!Name.IsEmpty())
						sheet.Name = Name;

					// Apply freeze panes
					if (FreezeRowCount > 0 || FreezeColCount > 0)
					{
						sheet.SplitPosition = new XlCellPosition(FreezeColCount, FreezeRowCount);
					}

					// Create columns with formatting and widths
					foreach (var pair in Columns)
					{
						using var xlCol = sheet.CreateColumn(pair.Key);

						if (ColumnWidths.TryGetValue(pair.Key, out var width))
						{
							xlCol.WidthInCharacters = (float)width;
						}

						if (pair.Value.type != null)
						{
							// Type-based formatting - not directly supported
						}
						else if (!pair.Value.format.IsEmpty())
						{
							xlCol.Formatting = new XlCellFormatting
							{
								IsDateTimeFormatString = true,
								NetFormatString = pair.Value.format,
							};
						}
					}

					// Create rows with data
					foreach (var row in Rows.OrderBy())
					{
						if (!_cells.TryGetValue(row, out var dict))
							continue;

						using var xlRow = sheet.CreateRow(row);

						if (RowHeights.TryGetValue(row, out var height))
						{
							xlRow.HeightInPoints = (float)height;
						}

						foreach (var pair in dict)
						{
							var cellData = pair.Value;
							if (cellData.Value == null && cellData.HyperlinkUrl == null)
								continue;

							using var cell = xlRow.CreateCell(pair.Key);

							// Apply cell formatting
							if (!cellData.Format.IsEmpty() || !cellData.BgColor.IsEmpty() || !cellData.FgColor.IsEmpty())
							{
								var formatting = new XlCellFormatting();

								if (!cellData.Format.IsEmpty())
								{
									formatting.IsDateTimeFormatString = true;
									formatting.NetFormatString = cellData.Format;
								}

								if (!cellData.BgColor.IsEmpty())
								{
									formatting.Fill = XlFill.SolidFill(ParseColor(cellData.BgColor));
								}

								if (!cellData.FgColor.IsEmpty())
								{
									formatting.Font = new XlFont { Color = ParseColor(cellData.FgColor) };
								}

								cell.Formatting = formatting;
							}

							// Set value (including hyperlink text if applicable)
							var displayValue = cellData.Value;
							if (!cellData.HyperlinkUrl.IsEmpty() && displayValue == null)
							{
								displayValue = cellData.HyperlinkText ?? cellData.HyperlinkUrl;
							}

							if (displayValue != null)
							{
								XlVariantValue xlVal;

								if (displayValue is bool b)
									xlVal = new XlVariantValue { BooleanValue = b };
								else if (displayValue is DateTime dt)
									xlVal = new XlVariantValue { DateTimeValue = dt };
								else if (displayValue is DateTimeOffset dto)
									xlVal = new XlVariantValue { DateTimeValue = dto.UtcDateTime };
								else if (displayValue.GetType().IsNumeric())
									xlVal = new XlVariantValue { NumericValue = displayValue.To<double>() };
								else
								{
									xlVal = new XlVariantValue { TextValue = displayValue.To<string>() };
								}

								cell.Value = xlVal;
							}
						}
					}

					// Apply merge ranges
					foreach (var merge in MergeRanges)
					{
						sheet.MergedCells.Add(new XlCellRange(
							new XlCellPosition(merge.StartCol, merge.StartRow),
							new XlCellPosition(merge.EndCol, merge.EndRow)));
					}
				}

				Columns.Clear();
				ColumnWidths.Clear();
				RowHeights.Clear();
				AutoFitColumns.Clear();
				Rows.Clear();
				MergeRanges.Clear();

				_cells.Clear();
			}

			private static Color ParseColor(string colorStr)
			{
				if (colorStr.IsEmpty())
					return Color.Empty;

				if (colorStr.StartsWithIgnoreCase("#"))
				{
					return ColorTranslator.FromHtml(colorStr);
				}

				return Color.FromName(colorStr);
			}
		}

		private readonly IXlExporter _exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);
		private readonly IXlDocument _document;
		private readonly List<SheetData> _sheets = [];
		private SheetData _currSheet;

		public DevExpExcelWorker(Stream stream)
		{
			_document = _exporter.CreateDocument(stream);
		}

		void IDisposable.Dispose()
		{
			_sheets.ForEach(s => s.Dispose());
			_sheets.Clear();

			_document.Dispose();

			GC.SuppressFinalize(this);
		}

		IExcelWorker IExcelWorker.SetCell<T>(int col, int row, T value)
		{
			_currSheet.SetCell(col, row, value);
			return this;
		}

		T IExcelWorker.GetCell<T>(int col, int row)
		{
			return _currSheet.GetCell<T>(col, row);
		}

		IExcelWorker IExcelWorker.SetStyle(int col, Type type)
		{
			_currSheet.Columns[col] = new(type, null);
			return this;
		}

		IExcelWorker IExcelWorker.SetStyle(int col, string format)
		{
			_currSheet.Columns[col] = new(null, format);
			return this;
		}

		IExcelWorker IExcelWorker.SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor)
		{
			// Note: DevExpress XlExport streaming API has limited conditional formatting support.
			// This is a no-op in this implementation.
			return this;
		}

		IExcelWorker IExcelWorker.RenameSheet(string name)
		{
			_currSheet.Name = name;
			return this;
		}

		IExcelWorker IExcelWorker.AddSheet()
		{
			_currSheet = new SheetData(this);
			_sheets.Add(_currSheet);
			return this;
		}

		bool IExcelWorker.ContainsSheet(string name) => _sheets.Any(s => s.Name.EqualsIgnoreCase(name));

		IExcelWorker IExcelWorker.SwitchSheet(string name)
		{
			_currSheet = _sheets.First(s => s.Name.EqualsIgnoreCase(name));
			return this;
		}

		int IExcelWorker.GetColumnsCount() => _currSheet.Columns.Count > 0 ? _currSheet.Columns.Keys.Max() + 1 : 0;
		int IExcelWorker.GetRowsCount() => _currSheet.Rows.Count > 0 ? _currSheet.Rows.Max() + 1 : 0;

		IExcelWorker IExcelWorker.SetColumnWidth(int col, double width)
		{
			_currSheet.ColumnWidths[col] = width;
			return this;
		}

		IExcelWorker IExcelWorker.SetRowHeight(int row, double height)
		{
			_currSheet.RowHeights[row] = height;
			return this;
		}

		IExcelWorker IExcelWorker.AutoFitColumn(int col)
		{
			// Note: DevExpress XlExport doesn't support auto-fit directly.
			// Track for future use or alternative implementation.
			_currSheet.AutoFitColumns.Add(col);
			return this;
		}

		IExcelWorker IExcelWorker.FreezeRows(int count)
		{
			_currSheet.FreezeRowCount = count;
			return this;
		}

		IExcelWorker IExcelWorker.FreezeCols(int count)
		{
			_currSheet.FreezeColCount = count;
			return this;
		}

		IExcelWorker IExcelWorker.MergeCells(int startCol, int startRow, int endCol, int endRow)
		{
			_currSheet.MergeRanges.Add(new MergeRange
			{
				StartCol = startCol,
				StartRow = startRow,
				EndCol = endCol,
				EndRow = endRow
			});
			return this;
		}

		IExcelWorker IExcelWorker.SetHyperlink(int col, int row, string url, string text)
		{
			_currSheet.SetHyperlink(col, row, url, text);
			return this;
		}

		IExcelWorker IExcelWorker.SetCellFormat(int col, int row, string format)
		{
			_currSheet.SetCellFormat(col, row, format);
			return this;
		}

		IExcelWorker IExcelWorker.SetCellColor(int col, int row, string bgColor, string fgColor)
		{
			_currSheet.SetCellColor(col, row, bgColor, fgColor);
			return this;
		}

		IEnumerable<string> IExcelWorker.GetSheetNames()
		{
			return _sheets.Select(s => s.Name).Where(n => !n.IsEmpty());
		}

		IExcelWorker IExcelWorker.DeleteSheet(string name)
		{
			var sheet = _sheets.FirstOrDefault(s => s.Name.EqualsIgnoreCase(name));
			if (sheet != null)
			{
				_sheets.Remove(sheet);
				if (_currSheet == sheet)
				{
					_currSheet = _sheets.FirstOrDefault();
				}
			}
			return this;
		}

		IExcelWorker IExcelWorker.AddLineChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddBarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddPieChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddAreaChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddDoughnutChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddScatterChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddRadarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddBubbleChart(string name, string dataRange, int xCol, int yCol, int sizeCol, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}

		IExcelWorker IExcelWorker.AddStockChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			// Note: DevExpress XlExport streaming API does not support chart creation.
			// Use OpenXmlExcelWorkerProvider for chart support.
			return this;
		}
	}

	IExcelWorker IExcelWorkerProvider.CreateNew(Stream stream, bool readOnly)
	{
		return new DevExpExcelWorker(stream);
	}

	IExcelWorker IExcelWorkerProvider.OpenExist(Stream stream)
	{
		// Note: DevExpress XlExport is write-only, it cannot read existing files.
		// This creates a new document on the stream.
		return new DevExpExcelWorker(stream);
	}
}
