namespace Ecng.Excel;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

using Ecng.Common;

using A = DocumentFormat.OpenXml.Drawing;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using Xdr = DocumentFormat.OpenXml.Drawing.Spreadsheet;

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
	public IExcelWorker CreateNew(Stream stream, bool readOnly)
	{
		if (readOnly)
			throw new InvalidOperationException("Cannot create new file in read-only mode.");

		return new OpenXmlExcelWorker(stream, createNew: true);
	}

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

		// Style caches to avoid duplicates
		private readonly Dictionary<string, uint> _numberFormatIdCache = new();
		private readonly Dictionary<string, uint> _fillIndexCache = new();
		private readonly Dictionary<string, uint> _fontIndexCache = new();
		private readonly Dictionary<(uint? numFmtId, uint? fillId, uint? fontId), uint> _cellFormatCache = new();
		private readonly Dictionary<int, uint> _columnStyleIndex2 = new();
		private int _conditionalFormattingPriority = 0;
		private uint _chartId = 0;
		private uint _nextNumberFormatId = 164;
		private bool _numberFormatIdSeeded;
		private bool _disposed;

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
			if (name.IsEmptyOrWhiteSpace())
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
			if (name.IsEmptyOrWhiteSpace())
				throw new ArgumentNullException(nameof(name));

			_currentSheet.Name = name;
			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SwitchSheet(string name)
		{
			if (name.IsEmptyOrWhiteSpace())
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
			// Handle SharedString (cell value is index into SharedStringTable)
			else if (cell.DataType?.Value == CellValues.SharedString)
			{
				var index = cell.CellValue?.Text;
				if (index != null && int.TryParse(index, out var ssIndex))
				{
					var sst = _workbookPart.SharedStringTablePart?.SharedStringTable;
					if (sst != null)
					{
						var item = sst.ElementAt(ssIndex);
						raw = item?.InnerText;
					}
					else
					{
						raw = null;
					}
				}
				else
				{
					raw = null;
				}
			}
			else
			{
				raw = cell.CellValue?.Text;
			}

			if (raw == null)
				return default;

			var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

			// Handle Boolean conversion (Excel stores as "1" or "0")
			if (targetType == typeof(bool))
			{
				return (T)(object)(raw == "1");
			}

			// Handle DateTime conversion (Excel stores as OADate double)
			if (targetType == typeof(DateTime))
			{
				if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate))
					return (T)(object)DateTime.FromOADate(oaDate);
			}

			return (T)Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
		}

		/// <inheritdoc />
		public IExcelWorker SetStyle(int col, Type type)
		{
			// Determine format based on type
			string format = null;
			if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
				format = "yyyy-MM-dd HH:mm:ss";
			else if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
				format = "#,##0.00";

			return format != null ? SetStyle(col, format) : this;
		}

		/// <inheritdoc />
		public IExcelWorker SetStyle(int col, string format)
		{
			if (format.IsEmpty())
				return this;

			var numFmtId = GetOrCreateNumberFormatId(format);
			var styleIndex = GetOrCreateCellFormatIndex(numFmtId, null, null);
			_columnStyleIndex2[col] = styleIndex;

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor)
		{
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null || condition.IsEmpty())
				return this;

			// Get the range for the column (e.g., "A:A" for column 0)
			var colName = ToColumnName(col);
			var sqRef = $"{colName}:{colName}";

			// Create the conditional formatting
			var conditionalFormatting = new ConditionalFormatting
			{
				SequenceOfReferences = new ListValue<StringValue> { InnerText = sqRef }
			};

			var cfRule = new ConditionalFormattingRule
			{
				Type = ConditionalFormatValues.CellIs,
				Priority = ++_conditionalFormattingPriority,
				Operator = op switch
				{
					ComparisonOperator.Equal => ConditionalFormattingOperatorValues.Equal,
					ComparisonOperator.NotEqual => ConditionalFormattingOperatorValues.NotEqual,
					ComparisonOperator.Greater => ConditionalFormattingOperatorValues.GreaterThan,
					ComparisonOperator.GreaterOrEqual => ConditionalFormattingOperatorValues.GreaterThanOrEqual,
					ComparisonOperator.Less => ConditionalFormattingOperatorValues.LessThan,
					ComparisonOperator.LessOrEqual => ConditionalFormattingOperatorValues.LessThanOrEqual,
					ComparisonOperator.Any => ConditionalFormattingOperatorValues.Equal,
					_ => ConditionalFormattingOperatorValues.Equal
				}
			};

			cfRule.Append(new Formula(condition));
			cfRule.FormatId = AddDifferentialFormat(bgColor, fgColor);

			conditionalFormatting.Append(cfRule);

			InsertConditionalFormatting(worksheet, conditionalFormatting);

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetConditionalFormattingFormula(int startCol, int startRow, int endCol, int endRow, string formula, string bgColor, string fgColor = null)
			=> SetConditionalFormattingFormula(startCol, startRow, endCol, endRow, formula,
				new ExcelConditionalFormat { BackgroundColor = bgColor, FontColor = fgColor });

		/// <inheritdoc />
		public IExcelWorker SetConditionalFormattingFormula(int startCol, int startRow, int endCol, int endRow, string formula, ExcelConditionalFormat format)
		{
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null || formula.IsEmpty())
				return this;

			var sqRef = $"{ToColumnName(startCol)}{startRow + 1}:{ToColumnName(endCol)}{endRow + 1}";

			var conditionalFormatting = new ConditionalFormatting
			{
				SequenceOfReferences = new ListValue<StringValue> { InnerText = sqRef }
			};

			var cfRule = new ConditionalFormattingRule
			{
				// Expression rule (not CellIs): Excel re-evaluates the formula on edit,
				// so the dxf follows the value instead of being a static cell fill.
				Type = ConditionalFormatValues.Expression,
				Priority = ++_conditionalFormattingPriority,
				FormatId = AddDifferentialFormat(format ?? new ExcelConditionalFormat()),
			};

			cfRule.Append(new Formula(formula));
			conditionalFormatting.Append(cfRule);

			InsertConditionalFormatting(worksheet, conditionalFormatting);

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetColorScale(int col, int startRow, string minColor, string midColor, string maxColor)
		{
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null)
				return this;

			// Get the range for the column starting from startRow (e.g., "G2:G10001")
			var colName = ToColumnName(col);
			var sqRef = $"{colName}{startRow + 1}:{colName}10001";

			// Create the conditional formatting with colorScale
			var conditionalFormatting = new ConditionalFormatting
			{
				SequenceOfReferences = new ListValue<StringValue> { InnerText = sqRef }
			};

			var cfRule = new ConditionalFormattingRule
			{
				Type = ConditionalFormatValues.ColorScale,
				Priority = ++_conditionalFormattingPriority
			};

			// Create 3-color scale: min (red) -> percentile 50 (yellow) -> max (green)
			var colorScale = new ColorScale();

			// Min value
			colorScale.Append(new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Min });
			// Mid value (percentile 50)
			colorScale.Append(new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Percentile, Val = "50" });
			// Max value
			colorScale.Append(new ConditionalFormatValueObject { Type = ConditionalFormatValueObjectValues.Max });

			// Colors
			colorScale.Append(new Color { Rgb = ParseColor(minColor) });
			colorScale.Append(new Color { Rgb = ParseColor(midColor) });
			colorScale.Append(new Color { Rgb = ParseColor(maxColor) });

			cfRule.Append(colorScale);
			conditionalFormatting.Append(cfRule);

			InsertConditionalFormatting(worksheet, conditionalFormatting);

			return this;
		}

		/// <inheritdoc />
		public int GetColumnsCount()
		{
			var sheetData = _currentWorksheetPart?.Worksheet?.GetFirstChild<SheetData>();
			if (sheetData == null)
				return 0;

			var maxCol = -1;
			foreach (var row in sheetData.Elements<Row>())
			{
				foreach (var cell in row.Elements<Cell>())
				{
					var (col, _) = ParseCellReference(cell.CellReference?.Value);
					if (col > maxCol)
						maxCol = col;
				}
			}
			return maxCol + 1;
		}

		/// <inheritdoc />
		public int GetRowsCount()
		{
			var sheetData = _currentWorksheetPart?.Worksheet?.GetFirstChild<SheetData>();
			if (sheetData == null)
				return 0;

			var maxRow = -1;
			foreach (var row in sheetData.Elements<Row>())
			{
				foreach (var cell in row.Elements<Cell>())
				{
					var (_, rowIdx) = ParseCellReference(cell.CellReference?.Value);
					if (rowIdx > maxRow)
						maxRow = rowIdx;
				}
			}
			return maxRow + 1;
		}

		/// <inheritdoc />
		public IExcelWorker SetColumnWidth(int col, double width)
		{
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null)
				return this;

			var columns = worksheet.GetFirstChild<Columns>();
			if (columns == null)
			{
				columns = new Columns();
				InsertWorksheetChildInOrder(worksheet, columns);
			}

			var colIndex = (uint)(col + 1);
			var existingCol = columns.Elements<Column>()
				.FirstOrDefault(c => c.Min <= colIndex && c.Max >= colIndex);

			if (existingCol != null)
			{
				existingCol.Width = width;
				existingCol.CustomWidth = true;
			}
			else
			{
				columns.Append(new Column
				{
					Min = colIndex,
					Max = colIndex,
					Width = width,
					CustomWidth = true
				});
			}

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetRowHeight(int row, double height)
		{
			var rowRef = GetOrCreateRow(row);
			rowRef.Height = height;
			rowRef.CustomHeight = true;
			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AutoFitColumn(int col) => this;

		/// <inheritdoc />
		public IExcelWorker FreezeRows(int count)
		{
			if (count <= 0)
				return this;

			EnsureSheetView();
			var sheetView = _currentWorksheetPart.Worksheet.SheetViews.Elements<SheetView>().First();

			var pane = sheetView.GetFirstChild<Pane>() ?? sheetView.AppendChild(new Pane());
			pane.VerticalSplit = count;
			UpdateFrozenPane(pane);

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker FreezeCols(int count)
		{
			if (count <= 0)
				return this;

			EnsureSheetView();
			var sheetView = _currentWorksheetPart.Worksheet.SheetViews.Elements<SheetView>().First();

			var pane = sheetView.GetFirstChild<Pane>() ?? sheetView.AppendChild(new Pane());
			pane.HorizontalSplit = count;
			UpdateFrozenPane(pane);

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker MergeCells(int startCol, int startRow, int endCol, int endRow)
		{
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null)
				return this;

			var mergeCells = worksheet.GetFirstChild<MergeCells>();
			if (mergeCells == null)
			{
				mergeCells = new MergeCells();
				InsertWorksheetChildInOrder(worksheet, mergeCells);
			}

			var startRef = ToCellReference(startCol, startRow);
			var endRef = ToCellReference(endCol, endRow);
			mergeCells.Append(new MergeCell { Reference = $"{startRef}:{endRef}" });

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetHyperlink(int col, int row, string url, string text)
		{
			// Set the text in the cell
			if (!text.IsEmpty())
				SetCellValue(col, row, text);
			else if (!url.IsEmpty())
				SetCellValue(col, row, url);

			// Add hyperlink relationship and element
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null || url.IsEmpty())
				return this;

			var hyperlinks = worksheet.GetFirstChild<Hyperlinks>();
			if (hyperlinks == null)
			{
				hyperlinks = new Hyperlinks();
				InsertWorksheetChildInOrder(worksheet, hyperlinks);
			}

			var relId = _currentWorksheetPart.AddHyperlinkRelationship(new Uri(url, UriKind.Absolute), true).Id;
			hyperlinks.Append(new Hyperlink
			{
				Reference = ToCellReference(col, row),
				Id = relId
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetCellFormat(int col, int row, string format)
		{
			if (format.IsEmpty())
				return this;

			var cell = GetCell(col, row, createIfMissing: true);
			var numFmtId = GetOrCreateNumberFormatId(format);
			var (_, fillId, fontId) = GetCellFormatIds(cell);
			var styleIndex = GetOrCreateCellFormatIndex(numFmtId, fillId, fontId);
			cell.StyleIndex = styleIndex;

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker SetCellColor(int col, int row, string bgColor, string fgColor = null)
			=> SetCellColor(col, row, bgColor, ExcelFillPattern.Solid, null, fgColor);

		/// <inheritdoc />
		public IExcelWorker SetCellColor(int col, int row, string bgColor, ExcelFillPattern pattern, string patternColor = null, string fgColor = null)
		{
			if (bgColor.IsEmpty() && fgColor.IsEmpty())
				return this;

			var cell = GetCell(col, row, createIfMissing: true);
			uint? fillId = null;
			uint? fontId = null;

			if (!bgColor.IsEmpty())
				fillId = GetOrCreateFillIndex(bgColor, pattern, patternColor);

			if (!fgColor.IsEmpty())
				fontId = GetOrCreateFontIndex(fgColor);

			var (numFmtId, existingFillId, existingFontId) = GetCellFormatIds(cell);
			var styleIndex = GetOrCreateCellFormatIndex(numFmtId, fillId ?? existingFillId, fontId ?? existingFontId);
			cell.StyleIndex = styleIndex;

			return this;
		}

		/// <inheritdoc />
		public IEnumerable<string> GetSheetNames()
			=> _workbookPart.Workbook.Sheets!.Elements<Sheet>().Select(s => s.Name?.Value).Where(n => !n.IsEmptyOrWhiteSpace());

		/// <inheritdoc />
		public IExcelWorker DeleteSheet(string name)
		{
			if (name.IsEmptyOrWhiteSpace())
				return this;

			var sheet = _workbookPart.Workbook.Sheets!.Elements<Sheet>()
				.FirstOrDefault(s => string.Equals(s.Name?.Value, name, StringComparison.OrdinalIgnoreCase));

			if (sheet == null)
				return this;

			var relId = sheet.Id?.Value;
			sheet.Remove();

			if (!relId.IsEmpty())
			{
				var part = _workbookPart.GetPartById(relId);
				if (part != null)
					_workbookPart.DeletePart(part);
			}

			// If we deleted the current sheet, switch to first available
			if (_currentSheet == sheet)
			{
				var firstSheet = _workbookPart.Workbook.Sheets!.Elements<Sheet>().FirstOrDefault();
				if (firstSheet != null)
				{
					_currentSheet = firstSheet;
					_currentWorksheetPart = (WorksheetPart)_workbookPart.GetPartById(firstSheet.Id!);
				}
				else
				{
					_currentSheet = null;
					_currentWorksheetPart = null;
				}
			}

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddLineChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var lineChart = new C.LineChart();
				lineChart.Append(new C.Grouping { Val = C.GroupingValues.Standard });
				lineChart.Append(new C.VaryColors { Val = false });

				var series = CreateLineSeries(0, name, dataRange, xCol, yCol);
				lineChart.Append(series);

				lineChart.Append(new C.AxisId { Val = 1 });
				lineChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(lineChart);
				plotArea.Append(CreateCategoryAxis(1, 2));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddBarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var barChart = new C.BarChart();
				barChart.Append(new C.BarDirection { Val = C.BarDirectionValues.Column });
				barChart.Append(new C.BarGrouping { Val = C.BarGroupingValues.Clustered });
				barChart.Append(new C.VaryColors { Val = false });

				var series = CreateBarSeries(0, name, dataRange);
				barChart.Append(series);

				barChart.Append(new C.AxisId { Val = 1 });
				barChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(barChart);
				plotArea.Append(CreateCategoryAxis(1, 2));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddPieChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
			=> AddPieChart(name, dataRange, anchorCol, anchorRow, width, height, null);

		/// <inheritdoc />
		public IExcelWorker AddPieChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height, IEnumerable<string> colors)
		{
			var colorList = colors?.ToArray();

			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var pieChart = new C.PieChart();
				pieChart.Append(new C.VaryColors { Val = true });

				var series = CreatePieSeries(0, name, dataRange, colors: colorList);
				pieChart.Append(series);

				plotArea.Append(pieChart);
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddAreaChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var areaChart = new C.AreaChart();
				areaChart.Append(new C.Grouping { Val = C.GroupingValues.Standard });
				areaChart.Append(new C.VaryColors { Val = false });

				var series = CreateAreaSeries(0, name, dataRange);
				areaChart.Append(series);

				areaChart.Append(new C.AxisId { Val = 1 });
				areaChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(areaChart);
				plotArea.Append(CreateCategoryAxis(1, 2));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddDoughnutChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var doughnutChart = new C.DoughnutChart();
				doughnutChart.Append(new C.VaryColors { Val = true });

				var series = CreatePieSeries(0, name, dataRange);
				doughnutChart.Append(series);

				doughnutChart.Append(new C.HoleSize { Val = 50 });

				plotArea.Append(doughnutChart);
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddScatterChart(string name, string dataRange, int xCol, int yCol, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var scatterChart = new C.ScatterChart();
				scatterChart.Append(new C.ScatterStyle { Val = C.ScatterStyleValues.LineMarker });
				scatterChart.Append(new C.VaryColors { Val = false });

				var series = CreateScatterSeries(0, name, dataRange, xCol, yCol);
				scatterChart.Append(series);

				scatterChart.Append(new C.AxisId { Val = 1 });
				scatterChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(scatterChart);
				plotArea.Append(CreateValueAxis(1, 2, C.AxisPositionValues.Bottom));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddRadarChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var radarChart = new C.RadarChart();
				radarChart.Append(new C.RadarStyle { Val = C.RadarStyleValues.Marker });
				radarChart.Append(new C.VaryColors { Val = false });

				var series = CreateRadarSeries(0, name, dataRange);
				radarChart.Append(series);

				radarChart.Append(new C.AxisId { Val = 1 });
				radarChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(radarChart);
				plotArea.Append(CreateCategoryAxis(1, 2));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddBubbleChart(string name, string dataRange, int xCol, int yCol, int sizeCol, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var bubbleChart = new C.BubbleChart();
				bubbleChart.Append(new C.VaryColors { Val = false });

				var series = CreateBubbleSeries(0, name, dataRange, xCol, yCol, sizeCol);
				bubbleChart.Append(series);

				bubbleChart.Append(new C.AxisId { Val = 1 });
				bubbleChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(bubbleChart);
				plotArea.Append(CreateValueAxis(1, 2, C.AxisPositionValues.Bottom));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		/// <inheritdoc />
		public IExcelWorker AddStockChart(string name, string dataRange, int anchorCol, int anchorRow, int width, int height)
		{
			AddChartCore(name, dataRange, anchorCol, anchorRow, width, height, plotArea =>
			{
				var stockChart = new C.StockChart();

				// Stock chart requires 4 series: Open, High, Low, Close
				stockChart.Append(CreateLineSeries(0, "Open", dataRange, 0, 1));
				stockChart.Append(CreateLineSeries(1, "High", dataRange, 0, 2));
				stockChart.Append(CreateLineSeries(2, "Low", dataRange, 0, 3));
				stockChart.Append(CreateLineSeries(3, "Close", dataRange, 0, 4));

				// Add high-low lines and up-down bars for proper stock chart appearance
				// Per OpenXML schema (CT_StockChart): ser, dLbls, dropLines, hiLowLines, upDownBars, axId
				stockChart.Append(new C.HighLowLines());
				stockChart.Append(new C.UpDownBars
				{
					GapWidth = new C.GapWidth { Val = 150 },
					UpBars = new C.UpBars(),
					DownBars = new C.DownBars()
				});

				// AxisId must be last per schema
				stockChart.Append(new C.AxisId { Val = 1 });
				stockChart.Append(new C.AxisId { Val = 2 });

				plotArea.Append(stockChart);
				plotArea.Append(CreateDateAxis(1, 2));
				plotArea.Append(CreateValueAxis(2, 1));
			});

			return this;
		}

		private void AddChartCore(string name, string dataRange, int anchorCol, int anchorRow, int width, int height, Action<C.PlotArea> configureChart)
		{
			var drawingsPart = _currentWorksheetPart.DrawingsPart;
			if (drawingsPart == null)
			{
				drawingsPart = _currentWorksheetPart.AddNewPart<DrawingsPart>();
				drawingsPart.WorksheetDrawing = new Xdr.WorksheetDrawing();

				// Link drawing to worksheet
				var worksheet = _currentWorksheetPart.Worksheet;
				var drawing = worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.Drawing>();
				if (drawing == null)
				{
					drawing = new DocumentFormat.OpenXml.Spreadsheet.Drawing { Id = _currentWorksheetPart.GetIdOfPart(drawingsPart) };
					InsertWorksheetChildInOrder(worksheet, drawing);
				}
			}

			var chartPart = drawingsPart.AddNewPart<ChartPart>();
			var chart = CreateChart(name, configureChart);
			chartPart.ChartSpace = chart;

			// Create anchor (coordinates are 0-based, same as SetCell API)
			var twoCellAnchor = new Xdr.TwoCellAnchor
			{
				FromMarker = new Xdr.FromMarker
				{
					ColumnId = new Xdr.ColumnId(anchorCol.ToString()),
					ColumnOffset = new Xdr.ColumnOffset("0"),
					RowId = new Xdr.RowId(anchorRow.ToString()),
					RowOffset = new Xdr.RowOffset("0")
				},
				ToMarker = new Xdr.ToMarker
				{
					ColumnId = new Xdr.ColumnId((anchorCol + (width / 64)).ToString()),
					ColumnOffset = new Xdr.ColumnOffset("0"),
					RowId = new Xdr.RowId((anchorRow + (height / 20)).ToString()),
					RowOffset = new Xdr.RowOffset("0")
				}
			};

			var graphicFrame = new Xdr.GraphicFrame
			{
				Macro = string.Empty,
				NonVisualGraphicFrameProperties = new Xdr.NonVisualGraphicFrameProperties
				{
					NonVisualDrawingProperties = new Xdr.NonVisualDrawingProperties { Id = NextDrawingId(drawingsPart.WorksheetDrawing), Name = name ?? "Chart" },
					NonVisualGraphicFrameDrawingProperties = new Xdr.NonVisualGraphicFrameDrawingProperties()
				},
				Transform = new Xdr.Transform
				{
					Offset = new A.Offset { X = 0, Y = 0 },
					Extents = new A.Extents { Cx = width * 9525, Cy = height * 9525 }
				},
				Graphic = new A.Graphic
				{
					GraphicData = new A.GraphicData
					{
						Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart",
						InnerXml = $"<c:chart xmlns:c=\"http://schemas.openxmlformats.org/drawingml/2006/chart\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"{drawingsPart.GetIdOfPart(chartPart)}\"/>"
					}
				}
			};

			twoCellAnchor.Append(graphicFrame);
			twoCellAnchor.Append(new Xdr.ClientData());

			drawingsPart.WorksheetDrawing.Append(twoCellAnchor);
		}

		private static C.ChartSpace CreateChart(string name, Action<C.PlotArea> configureChart)
		{
			var chartSpace = new C.ChartSpace();
			chartSpace.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
			chartSpace.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");

			var chart = new C.Chart();

			// Add title if provided
			if (!name.IsEmpty())
			{
				var richText = new C.RichText();
				richText.Append(new A.BodyProperties());
				richText.Append(new A.ListStyle());

				var paragraph = new A.Paragraph();
				var run = new A.Run();
				run.Append(new A.Text(name));
				paragraph.Append(run);
				richText.Append(paragraph);

				chart.Title = new C.Title
				{
					ChartText = new C.ChartText { RichText = richText },
					Overlay = new C.Overlay { Val = false }
				};
			}

			var plotArea = new C.PlotArea();
			plotArea.Append(new C.Layout());

			configureChart(plotArea);

			chart.Append(plotArea);
			var legend = new C.Legend();
			legend.Append(new C.LegendPosition { Val = C.LegendPositionValues.Bottom });
			legend.Append(new C.Overlay { Val = false });
			chart.Append(legend);
			chart.Append(new C.PlotVisibleOnly { Val = true });

			chartSpace.Append(chart);
			return chartSpace;
		}

		private static C.LineChartSeries CreateLineSeries(uint index, string name, string dataRange, int xCol, int yCol)
		{
			var series = new C.LineChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			series.Append(new C.Marker { Symbol = new C.Symbol { Val = C.MarkerStyleValues.None } });

			// Parse dataRange to extract sheet name and row range, then build column-specific formulas
			var (xFormula, yFormula) = BuildColumnFormulas(dataRange, xCol, yCol);

			// Add category (X) axis reference
			series.Append(new C.CategoryAxisData
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(xFormula)
				}
			});

			// Add values (Y) axis reference
			series.Append(new C.Values
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(yFormula)
				}
			});

			series.Append(new C.Smooth { Val = false });

			return series;
		}

		private static (string xFormula, string yFormula) BuildColumnFormulas(string dataRange, int xCol, int yCol)
		{
			var zeroBased = xCol == 0 || yCol == 0;
			return (BuildSingleColumnFormula(dataRange, xCol, zeroBased), BuildSingleColumnFormula(dataRange, yCol, zeroBased));
		}

		private static string BuildSingleColumnFormula(string dataRange, int col, bool zeroBased = false)
		{
			// dataRange format: "SheetName!$A$2:$B$10" or "Sheet!A2:B10"
			// We need to extract sheet name and row range, then build a single column reference

			var exclamationIdx = dataRange.IndexOf('!');
			var sheetName = exclamationIdx < 0 ? string.Empty : dataRange[..exclamationIdx];
			var cellRange = exclamationIdx < 0 ? dataRange : dataRange[(exclamationIdx + 1)..];

			// Parse cell range like "$A$2:$B$10" or "A2:B10"
			var colonIdx = cellRange.IndexOf(':');
			if (colonIdx < 0)
				return dataRange; // fallback

			var startCell = cellRange[..colonIdx];
			var endCell = cellRange[(colonIdx + 1)..];

			// Extract row numbers from cells
			var startRow = ExtractRowNumber(startCell);
			var endRow = ExtractRowNumber(endCell);
			var startCol = ExtractColumnIndex(startCell);

			var colIndex = zeroBased ? startCol + col : col - 1;
			if (colIndex < 0)
				colIndex = startCol;

			var colLetter = ToColumnName(colIndex);
			var prefix = sheetName.IsEmpty() ? string.Empty : $"{sheetName}!";

			return $"{prefix}${colLetter}${startRow}:${colLetter}${endRow}";
		}

		private static int ExtractRowNumber(string cell)
		{
			// Remove $ signs and extract numeric part
			var cleaned = cell.Replace("$", "");
			var digits = new string(cleaned.Where(char.IsDigit).ToArray());
			return int.TryParse(digits, out var row) ? row : 1;
		}

		private static int ExtractColumnIndex(string cell)
		{
			var cleaned = cell.Replace("$", "");
			var letters = new string(cleaned.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
			if (letters.IsEmpty())
				return 0;

			var col = 0;
			foreach (var ch in letters)
			{
				col *= 26;
				col += ch - 'A' + 1;
			}

			return col - 1;
		}

		private static C.BarChartSeries CreateBarSeries(uint index, string name, string dataRange, int catCol = 0, int valCol = 1)
		{
			var series = new C.BarChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			// Use separate columns for categories and values
			var (catFormula, valFormula) = BuildColumnFormulas(dataRange, catCol, valCol);

			series.Append(new C.CategoryAxisData
			{
				StringReference = new C.StringReference
				{
					Formula = new C.Formula(catFormula)
				}
			});

			series.Append(new C.Values
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(valFormula)
				}
			});

			return series;
		}

		private static C.PieChartSeries CreatePieSeries(uint index, string name, string dataRange, int catCol = 0, int valCol = 1, IReadOnlyList<string> colors = null)
		{
			var series = new C.PieChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			// Per-slice fills as data points. CT_PieSer orders dPt after the series text
			// and before the category/value references, so emit them here. A solid fill on
			// each dPt overrides Office's automatic per-slice colour; slices without a colour
			// keep the automatic one.
			if (colors is not null)
			{
				for (var i = 0; i < colors.Count; i++)
				{
					if (colors[i].IsEmpty())
						continue;

					series.Append(new C.DataPoint(
						new C.Index { Val = (uint)i },
						new C.Bubble3D { Val = false },
						new C.ChartShapeProperties(
							new A.SolidFill(new A.RgbColorModelHex { Val = ToChartHex(colors[i]) }))));
				}
			}

			// Use separate columns for categories and values
			var (catFormula, valFormula) = BuildColumnFormulas(dataRange, catCol, valCol);

			series.Append(new C.CategoryAxisData
			{
				StringReference = new C.StringReference
				{
					Formula = new C.Formula(catFormula)
				}
			});

			series.Append(new C.Values
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(valFormula)
				}
			});

			return series;
		}

		private static C.AreaChartSeries CreateAreaSeries(uint index, string name, string dataRange, int catCol = 0, int valCol = 1)
		{
			var series = new C.AreaChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			// Use separate columns for categories and values
			var (catFormula, valFormula) = BuildColumnFormulas(dataRange, catCol, valCol);

			series.Append(new C.CategoryAxisData
			{
				StringReference = new C.StringReference
				{
					Formula = new C.Formula(catFormula)
				}
			});

			series.Append(new C.Values
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(valFormula)
				}
			});

			return series;
		}

		private static C.ScatterChartSeries CreateScatterSeries(uint index, string name, string dataRange, int xCol, int yCol)
		{
			var series = new C.ScatterChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			series.Append(new C.Marker { Symbol = new C.Symbol { Val = C.MarkerStyleValues.Circle } });

			// Parse dataRange to extract sheet name and row range, then build column-specific formulas
			var (xFormula, yFormula) = BuildColumnFormulas(dataRange, xCol, yCol);

			series.Append(new C.XValues
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(xFormula)
				}
			});

			series.Append(new C.YValues
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(yFormula)
				}
			});

			series.Append(new C.Smooth { Val = false });

			return series;
		}

		private static C.RadarChartSeries CreateRadarSeries(uint index, string name, string dataRange, int catCol = 0, int valCol = 1)
		{
			var series = new C.RadarChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			series.Append(new C.Marker { Symbol = new C.Symbol { Val = C.MarkerStyleValues.Circle } });

			// Use separate columns for categories and values
			var (catFormula, valFormula) = BuildColumnFormulas(dataRange, catCol, valCol);

			series.Append(new C.CategoryAxisData
			{
				StringReference = new C.StringReference
				{
					Formula = new C.Formula(catFormula)
				}
			});

			series.Append(new C.Values
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(valFormula)
				}
			});

			return series;
		}

		private static C.BubbleChartSeries CreateBubbleSeries(uint index, string name, string dataRange, int xCol, int yCol, int sizeCol)
		{
			var series = new C.BubbleChartSeries();
			series.Append(new C.Index { Val = index });
			series.Append(new C.Order { Val = index });

			if (!name.IsEmpty())
			{
				series.Append(new C.SeriesText
				{
					NumericValue = new C.NumericValue(name)
				});
			}

			// Build column formulas from the column indices
			var zeroBased = xCol == 0 || yCol == 0 || sizeCol == 0;
			var xFormula = BuildSingleColumnFormula(dataRange, xCol, zeroBased);
			var yFormula = BuildSingleColumnFormula(dataRange, yCol, zeroBased);
			var sizeFormula = BuildSingleColumnFormula(dataRange, sizeCol, zeroBased);

			series.Append(new C.XValues
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(xFormula)
				}
			});

			series.Append(new C.YValues
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(yFormula)
				}
			});

			series.Append(new C.BubbleSize
			{
				NumberReference = new C.NumberReference
				{
					Formula = new C.Formula(sizeFormula)
				}
			});

			series.Append(new C.Bubble3D { Val = false });

			return series;
		}

		private static C.CategoryAxis CreateCategoryAxis(uint id, uint crossingAxisId)
		{
			var axis = new C.CategoryAxis();
			axis.Append(new C.AxisId { Val = id });
			axis.Append(new C.Scaling { Orientation = new C.Orientation { Val = C.OrientationValues.MinMax } });
			axis.Append(new C.Delete { Val = false });
			axis.Append(new C.AxisPosition { Val = C.AxisPositionValues.Bottom });
			axis.Append(new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo });
			axis.Append(new C.CrossingAxis { Val = crossingAxisId });
			axis.Append(new C.Crosses { Val = C.CrossesValues.AutoZero });
			axis.Append(new C.AutoLabeled { Val = true });
			axis.Append(new C.LabelAlignment { Val = C.LabelAlignmentValues.Center });
			axis.Append(new C.LabelOffset { Val = 100 });

			return axis;
		}

		private static C.ValueAxis CreateValueAxis(uint id, uint crossingAxisId)
			=> CreateValueAxis(id, crossingAxisId, C.AxisPositionValues.Left);

		private static C.ValueAxis CreateValueAxis(uint id, uint crossingAxisId, C.AxisPositionValues position)
		{
			var axis = new C.ValueAxis();
			axis.Append(new C.AxisId { Val = id });
			axis.Append(new C.Scaling { Orientation = new C.Orientation { Val = C.OrientationValues.MinMax } });
			axis.Append(new C.Delete { Val = false });
			axis.Append(new C.AxisPosition { Val = position });
			axis.Append(new C.MajorGridlines());
			axis.Append(new C.NumberingFormat { FormatCode = "General", SourceLinked = true });
			axis.Append(new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo });
			axis.Append(new C.CrossingAxis { Val = crossingAxisId });
			axis.Append(new C.Crosses { Val = C.CrossesValues.AutoZero });
			axis.Append(new C.CrossBetween { Val = C.CrossBetweenValues.Between });

			return axis;
		}

		private static C.DateAxis CreateDateAxis(uint id, uint crossingAxisId)
		{
			var axis = new C.DateAxis();
			axis.Append(new C.AxisId { Val = id });
			axis.Append(new C.Scaling { Orientation = new C.Orientation { Val = C.OrientationValues.MinMax } });
			axis.Append(new C.Delete { Val = false });
			axis.Append(new C.AxisPosition { Val = C.AxisPositionValues.Bottom });
			axis.Append(new C.NumberingFormat { FormatCode = "d-mmm", SourceLinked = false });
			axis.Append(new C.TickLabelPosition { Val = C.TickLabelPositionValues.NextTo });
			axis.Append(new C.CrossingAxis { Val = crossingAxisId });
			axis.Append(new C.Crosses { Val = C.CrossesValues.AutoZero });
			axis.Append(new C.AutoLabeled { Val = true });
			axis.Append(new C.LabelOffset { Val = 100 });

			return axis;
		}

		private void EnsureSheetView()
		{
			var worksheet = _currentWorksheetPart?.Worksheet;
			if (worksheet == null)
				return;

			var sheetViews = worksheet.GetFirstChild<SheetViews>();
			if (sheetViews == null)
			{
				sheetViews = new SheetViews(new SheetView { WorkbookViewId = 0 });
				InsertWorksheetChildInOrder(worksheet, sheetViews);
			}
			else if (!sheetViews.Elements<SheetView>().Any())
			{
				sheetViews.Append(new SheetView { WorkbookViewId = 0 });
			}
		}

		private uint NextCustomNumberFormatId(Stylesheet stylesheet)
		{
			if (!_numberFormatIdSeeded)
			{
				_numberFormatIdSeeded = true;

				var max = 163u; // built-in number-format ids end at 163

				if (stylesheet.NumberingFormats is { } numFmts)
					foreach (var nf in numFmts.Elements<NumberingFormat>())
						if (nf.NumberFormatId?.Value is { } id && id > max)
							max = id;

				if (stylesheet.DifferentialFormats is { } dxfs)
					foreach (var d in dxfs.Elements<DifferentialFormat>())
						if (d.NumberingFormat?.NumberFormatId?.Value is { } id && id > max)
							max = id;

				_nextNumberFormatId = max + 1;
			}

			return _nextNumberFormatId++;
		}

		private uint AddDifferentialFormat(string bgColor, string fgColor)
			=> AddDifferentialFormat(new ExcelConditionalFormat { BackgroundColor = bgColor, FontColor = fgColor });

		/// <summary>
		/// Builds a differential format (dxf) for the given <see cref="ExcelConditionalFormat"/>,
		/// registers it in the stylesheet's <see cref="DifferentialFormats"/>, and returns its
		/// zero-based index for a conditional formatting rule to reference via <c>FormatId</c>.
		/// </summary>
		private uint AddDifferentialFormat(ExcelConditionalFormat fmt)
		{
			var stylesheet = EnsureStylesheet();

			var dxf = new DifferentialFormat();

			// Font: build it only when at least one font aspect is requested. The SDK
			// serialises CT_Font children in schema order regardless of assignment order.
			if (fmt.Bold.HasValue || fmt.Italic.HasValue || fmt.Underline.HasValue ||
				fmt.Strikethrough.HasValue || fmt.FontSize.HasValue ||
				!fmt.FontName.IsEmpty() || !fmt.FontColor.IsEmpty())
			{
				var font = new Font();

				if (fmt.Bold.HasValue)
					font.Bold = new Bold { Val = fmt.Bold.Value };
				if (fmt.Italic.HasValue)
					font.Italic = new Italic { Val = fmt.Italic.Value };
				if (fmt.Strikethrough.HasValue)
					font.Strike = new Strike { Val = fmt.Strikethrough.Value };
				if (fmt.Underline.HasValue)
					font.Underline = new Underline { Val = fmt.Underline.Value ? UnderlineValues.Single : UnderlineValues.None };
				if (fmt.FontSize.HasValue)
					font.FontSize = new FontSize { Val = fmt.FontSize.Value };
				if (!fmt.FontColor.IsEmpty())
					font.Color = new Color { Rgb = ParseColor(fmt.FontColor) };
				if (!fmt.FontName.IsEmpty())
					font.FontName = new FontName { Val = fmt.FontName };

				dxf.Font = font;
			}

			// Number format: a dxf numFmt carries both a (custom) id and the literal code.
			if (!fmt.NumberFormat.IsEmpty())
			{
				dxf.NumberingFormat = new NumberingFormat
				{
					NumberFormatId = NextCustomNumberFormatId(stylesheet),
					FormatCode = fmt.NumberFormat,
				};
			}

			// Fill: a dxf solid fill stores the colour in BackgroundColor (the slot Excel
			// actually paints), unlike a normal cell fill which uses ForegroundColor.
			if (!fmt.BackgroundColor.IsEmpty() || fmt.FillPattern != ExcelFillPattern.Solid)
			{
				PatternFill patternFill;

				if (fmt.FillPattern == ExcelFillPattern.Solid)
				{
					patternFill = new PatternFill
					{
						PatternType = PatternValues.Solid,
						BackgroundColor = new BackgroundColor { Rgb = ParseColor(fmt.BackgroundColor) }
					};
				}
				else
				{
					// Two-colour pattern: ForegroundColor draws the lines/dots, BackgroundColor
					// is the cell background. Default the pattern colour to black when unspecified.
					patternFill = new PatternFill
					{
						PatternType = MapFillPattern(fmt.FillPattern),
						ForegroundColor = new ForegroundColor { Rgb = ParseColor(fmt.PatternColor.IsEmpty() ? "#000000" : fmt.PatternColor) },
					};

					if (!fmt.BackgroundColor.IsEmpty())
						patternFill.BackgroundColor = new BackgroundColor { Rgb = ParseColor(fmt.BackgroundColor) };
				}

				dxf.Fill = new Fill(patternFill);
			}

			// Border: one style/colour applied to all four sides.
			if (fmt.Border != ExcelBorderStyle.None)
			{
				var style = MapBorderStyle(fmt.Border);
				var rgb = ParseColor(fmt.BorderColor.IsEmpty() ? "#000000" : fmt.BorderColor);

				dxf.Border = new Border
				{
					LeftBorder = new LeftBorder(new Color { Rgb = rgb }) { Style = style },
					RightBorder = new RightBorder(new Color { Rgb = rgb }) { Style = style },
					TopBorder = new TopBorder(new Color { Rgb = rgb }) { Style = style },
					BottomBorder = new BottomBorder(new Color { Rgb = rgb }) { Style = style },
				};
			}

			stylesheet.DifferentialFormats ??= new DifferentialFormats { Count = 0 };
			stylesheet.DifferentialFormats.Append(dxf);
			stylesheet.DifferentialFormats.Count = (uint)stylesheet.DifferentialFormats.ChildElements.Count;

			return stylesheet.DifferentialFormats.Count.Value - 1;
		}

		private static BorderStyleValues MapBorderStyle(ExcelBorderStyle style) => style switch
		{
			ExcelBorderStyle.Hair => BorderStyleValues.Hair,
			ExcelBorderStyle.Thin => BorderStyleValues.Thin,
			ExcelBorderStyle.Medium => BorderStyleValues.Medium,
			ExcelBorderStyle.Thick => BorderStyleValues.Thick,
			ExcelBorderStyle.Dashed => BorderStyleValues.Dashed,
			ExcelBorderStyle.Dotted => BorderStyleValues.Dotted,
			ExcelBorderStyle.Double => BorderStyleValues.Double,
			_ => BorderStyleValues.None,
		};

		private static PatternValues MapFillPattern(ExcelFillPattern pattern) => pattern switch
		{
			ExcelFillPattern.None => PatternValues.None,
			ExcelFillPattern.Solid => PatternValues.Solid,
			ExcelFillPattern.MediumGray => PatternValues.MediumGray,
			ExcelFillPattern.DarkGray => PatternValues.DarkGray,
			ExcelFillPattern.LightGray => PatternValues.LightGray,
			ExcelFillPattern.Gray125 => PatternValues.Gray125,
			ExcelFillPattern.Gray0625 => PatternValues.Gray0625,
			ExcelFillPattern.DarkHorizontal => PatternValues.DarkHorizontal,
			ExcelFillPattern.DarkVertical => PatternValues.DarkVertical,
			ExcelFillPattern.DarkDown => PatternValues.DarkDown,
			ExcelFillPattern.DarkUp => PatternValues.DarkUp,
			ExcelFillPattern.DarkGrid => PatternValues.DarkGrid,
			ExcelFillPattern.DarkTrellis => PatternValues.DarkTrellis,
			ExcelFillPattern.LightHorizontal => PatternValues.LightHorizontal,
			ExcelFillPattern.LightVertical => PatternValues.LightVertical,
			ExcelFillPattern.LightDown => PatternValues.LightDown,
			ExcelFillPattern.LightUp => PatternValues.LightUp,
			ExcelFillPattern.LightGrid => PatternValues.LightGrid,
			ExcelFillPattern.LightTrellis => PatternValues.LightTrellis,
			_ => PatternValues.Solid,
		};

		/// <summary>
		/// Inserts a conditional formatting block right after the sheet data (the schema
		/// requires it to follow <see cref="SheetData"/>), appending it if no sheet data
		/// exists yet.
		/// </summary>
		private static void InsertConditionalFormatting(Worksheet worksheet, ConditionalFormatting conditionalFormatting)
		{
			InsertWorksheetChildInOrder(worksheet, conditionalFormatting);
		}

		private Stylesheet EnsureStylesheet()
		{
			var stylesPart = _workbookPart.WorkbookStylesPart;
			if (stylesPart == null)
			{
				stylesPart = _workbookPart.AddNewPart<WorkbookStylesPart>();
				stylesPart.Stylesheet = new Stylesheet();
			}

			var stylesheet = stylesPart.Stylesheet;

			// Ensure required elements exist
			if (stylesheet.Fonts == null)
			{
				stylesheet.Fonts = new Fonts(new Font());
				stylesheet.Fonts.Count = 1;
			}

			if (stylesheet.Fills == null)
			{
				// Excel requires at least 2 fills: none and gray125
				stylesheet.Fills = new Fills(
					new Fill(new PatternFill { PatternType = PatternValues.None }),
					new Fill(new PatternFill { PatternType = PatternValues.Gray125 }));
				stylesheet.Fills.Count = 2;
			}

			if (stylesheet.Borders == null)
			{
				stylesheet.Borders = new Borders(new Border());
				stylesheet.Borders.Count = 1;
			}

			if (stylesheet.CellFormats == null)
			{
				stylesheet.CellFormats = new CellFormats(new CellFormat { FontId = 0, FillId = 0, BorderId = 0 });
				stylesheet.CellFormats.Count = 1;
			}

			if (stylesheet.NumberingFormats == null)
			{
				stylesheet.NumberingFormats = new NumberingFormats { Count = 0 };
			}

			return stylesheet;
		}

		private uint GetOrCreateNumberFormatId(string format)
		{
			if (_numberFormatIdCache.TryGetValue(format, out var cached))
				return cached;

			var stylesheet = EnsureStylesheet();
			var numFmts = stylesheet.NumberingFormats;

			var existing = numFmts.Elements<NumberingFormat>()
				.FirstOrDefault(nf => string.Equals(nf.FormatCode?.Value, format, StringComparison.Ordinal));
			if (existing?.NumberFormatId?.Value is { } existingId)
			{
				_numberFormatIdCache[format] = existingId;
				return existingId;
			}

			var nextId = NextCustomNumberFormatId(stylesheet);
			numFmts.Append(new NumberingFormat { NumberFormatId = nextId, FormatCode = format });
			numFmts.Count = (uint)numFmts.ChildElements.Count;

			_numberFormatIdCache[format] = nextId;
			return nextId;
		}

		private uint GetOrCreateFillIndex(string bgColor, ExcelFillPattern pattern = ExcelFillPattern.Solid, string patternColor = null)
		{
			if (bgColor.IsEmpty())
				return 0;

			var cacheKey = $"{bgColor}|{pattern}|{patternColor}";
			if (_fillIndexCache.TryGetValue(cacheKey, out var cached))
				return cached;

			var stylesheet = EnsureStylesheet();
			var fills = stylesheet.Fills;

			var color = ParseColor(bgColor);

			PatternFill patternFill;

			if (pattern == ExcelFillPattern.Solid)
			{
				// A normal cell solid fill paints from ForegroundColor; BackgroundColor=Indexed 64
				// is the conventional placeholder.
				patternFill = new PatternFill
				{
					PatternType = PatternValues.Solid,
					ForegroundColor = new ForegroundColor { Rgb = color },
					BackgroundColor = new BackgroundColor { Indexed = 64 }
				};
			}
			else
			{
				// Two-colour pattern: ForegroundColor draws the lines/dots, BackgroundColor is the
				// cell background. Default the pattern colour to black when unspecified.
				patternFill = new PatternFill
				{
					PatternType = MapFillPattern(pattern),
					ForegroundColor = new ForegroundColor { Rgb = ParseColor(patternColor.IsEmpty() ? "#000000" : patternColor) },
					BackgroundColor = new BackgroundColor { Rgb = color }
				};
			}

			var fill = new Fill(patternFill);

			fills.Append(fill);
			fills.Count = (uint)fills.ChildElements.Count;

			var index = fills.Count.Value - 1;
			_fillIndexCache[cacheKey] = index;
			return index;
		}

		private uint GetOrCreateFontIndex(string fgColor)
		{
			if (fgColor.IsEmpty())
				return 0;

			if (_fontIndexCache.TryGetValue(fgColor, out var cached))
				return cached;

			var stylesheet = EnsureStylesheet();
			var fonts = stylesheet.Fonts;

			var color = ParseColor(fgColor);
			var font = new Font(new Color { Rgb = color });

			fonts.Append(font);
			fonts.Count = (uint)fonts.ChildElements.Count;

			var index = fonts.Count.Value - 1;
			_fontIndexCache[fgColor] = index;
			return index;
		}

		private uint GetOrCreateCellFormatIndex(uint? numFmtId, uint? fillId, uint? fontId)
		{
			var key = (numFmtId, fillId, fontId);
			if (_cellFormatCache.TryGetValue(key, out var cached))
				return cached;

			var stylesheet = EnsureStylesheet();
			var cellFormats = stylesheet.CellFormats;

			var cellFormat = new CellFormat
			{
				FontId = fontId ?? 0,
				BorderId = 0,
				FillId = fillId ?? 0,
				ApplyFill = fillId.HasValue,
				ApplyFont = fontId.HasValue
			};

			if (numFmtId.HasValue)
			{
				cellFormat.NumberFormatId = numFmtId.Value;
				cellFormat.ApplyNumberFormat = true;
			}

			cellFormats.Append(cellFormat);
			cellFormats.Count = (uint)cellFormats.ChildElements.Count;

			var index = cellFormats.Count.Value - 1;
			_cellFormatCache[key] = index;
			return index;
		}

		private (uint? numFmtId, uint? fillId, uint? fontId) GetCellFormatIds(Cell cell)
		{
			if (cell?.StyleIndex?.Value is not { } styleIndex)
				return (null, null, null);

			var cellFormat = _workbookPart.WorkbookStylesPart?.Stylesheet?.CellFormats?
				.Elements<CellFormat>()
				.ElementAtOrDefault((int)styleIndex);

			if (cellFormat == null)
				return (null, null, null);

			return (cellFormat.NumberFormatId?.Value, cellFormat.FillId?.Value, cellFormat.FontId?.Value);
		}

		// Chart srgbClr values are 6-hex RRGGBB (no alpha), whereas ParseColor yields the
		// 8-hex AARRGGBB used for cell/fill colours; drop the leading alpha byte.
		private static string ToChartHex(string color)
		{
			var argb = ParseColor(color);
			return argb.Length == 8 ? argb.Substring(2) : argb;
		}

		private static string ParseColor(string color)
		{
			if (color.IsEmpty())
				return "FF000000";

			// Handle named colors
			color = color.ToUpperInvariant() switch
			{
				"LIGHTGRAY" or "LIGHTGREY" => "FFD3D3D3",
				"RED" => "FFFF0000",
				"GREEN" => "FF00FF00",
				"BLUE" => "FF0000FF",
				"YELLOW" => "FFFFFF00",
				"WHITE" => "FFFFFFFF",
				"BLACK" => "FF000000",
				_ => color
			};

			// Handle #RRGGBB or RRGGBB format
			if (color.StartsWith("#"))
				color = color.Substring(1);

			// Ensure ARGB format (add FF alpha if only RGB)
			if (color.Length == 6)
				color = "FF" + color;

			return color.ToUpperInvariant();
		}

		private Row GetOrCreateRow(int row)
		{
			var sheetData = _currentWorksheetPart.Worksheet.GetFirstChild<SheetData>()
				?? _currentWorksheetPart.Worksheet.AppendChild(new SheetData());

			var rowIndex = (uint)(row + 1);
			var rowRef = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == rowIndex);

			if (rowRef == null)
			{
				rowRef = new Row { RowIndex = rowIndex };
				var nextRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value > rowIndex);
				if (nextRow != null)
					sheetData.InsertBefore(rowRef, nextRow);
				else
					sheetData.Append(rowRef);
			}

			return rowRef;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			_doc.Save();
			_doc.Dispose();

			if (_targetStream.CanWrite)
			{
				if (_targetStream.CanSeek)
				{
					_targetStream.Position = 0;
					_targetStream.SetLength(0);
				}

				_workStream.Position = 0;
				_workStream.CopyTo(_targetStream);

				if (_targetStream.CanSeek)
					_targetStream.Position = 0;
			}

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

			// If this cell is newly created (no StyleIndex), apply column style
			if (cell.StyleIndex == null)
			{
				// First check if column style was set via SetStyle()
				if (_columnStyleIndex2.TryGetValue(col, out var styleIdx2))
					cell.StyleIndex = styleIdx2;
				// Otherwise use style from sample row (template)
				else if (_columnStyleIndex.TryGetValue(col, out var styleIdx))
					cell.StyleIndex = styleIdx;
			}

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
					cell.DataType = null;
					cell.CellValue = new CellValue(dto.DateTime.ToOADate().ToString(CultureInfo.InvariantCulture));
					return;
			}

			var type = value.GetType();

			// Keep IDs as text if you want to avoid any double precision issues in Excel itself.
			// (Excel also shows only ~15 significant digits for numbers.)
			const long maxExactInteger = 9_007_199_254_740_991L;
			if (value is long l && (l < -maxExactInteger || l > maxExactInteger))
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
				var nextRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value > rowIndex);
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
					.FirstOrDefault(c => ParseCellReference(c.CellReference?.Value).col > col);

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
				dividend = (dividend - 1) / 26;
			}

			return columnName;
		}

		private static (int col, int row) ParseCellReference(string cellRef)
		{
			if (cellRef.IsEmptyOrWhiteSpace())
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

		private static void UpdateFrozenPane(Pane pane)
		{
			var frozenRows = (int)(pane.VerticalSplit?.Value ?? 0D);
			var frozenCols = (int)(pane.HorizontalSplit?.Value ?? 0D);

			pane.TopLeftCell = $"{ToColumnName(frozenCols)}{frozenRows + 1}";
			pane.ActivePane = frozenRows > 0 && frozenCols > 0
				? PaneValues.BottomRight
				: frozenCols > 0 ? PaneValues.TopRight : PaneValues.BottomLeft;
			pane.State = PaneStateValues.Frozen;
		}

		private uint NextDrawingId(Xdr.WorksheetDrawing drawing)
		{
			var max = drawing.Descendants<Xdr.NonVisualDrawingProperties>()
				.Select(p => p.Id?.Value ?? 0u)
				.DefaultIfEmpty(0u)
				.Max();

			if (_chartId < max)
				_chartId = max;

			return ++_chartId;
		}

		private static void InsertWorksheetChildInOrder(Worksheet worksheet, OpenXmlElement element)
		{
			var priority = GetWorksheetChildPriority(element);
			var next = worksheet.ChildElements.FirstOrDefault(child => GetWorksheetChildPriority(child) > priority);

			if (next != null)
				worksheet.InsertBefore(element, next);
			else
				worksheet.Append(element);
		}

		private static int GetWorksheetChildPriority(OpenXmlElement element)
			=> element switch
			{
				SheetProperties => 0,
				SheetDimension => 1,
				SheetViews => 2,
				SheetFormatProperties => 3,
				Columns => 4,
				SheetData => 5,
				DocumentFormat.OpenXml.Spreadsheet.MergeCells => 14,
				ConditionalFormatting => 16,
				DataValidations => 17,
				Hyperlinks => 18,
				PrintOptions => 19,
				PageMargins => 20,
				PageSetup => 21,
				HeaderFooter => 22,
				RowBreaks => 23,
				ColumnBreaks => 24,
				DocumentFormat.OpenXml.Spreadsheet.Drawing => 29,
				LegacyDrawing => 30,
				LegacyDrawingHeaderFooter => 31,
				Picture => 32,
				TableParts => 36,
				WorksheetExtensionList => 37,
				_ => int.MaxValue,
			};
	}
}
