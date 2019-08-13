namespace Ecng.Interop
{
	using System;
	using System.Windows.Media;

	using Ecng.ComponentModel;

	public interface IExcelWorker : IDisposable
	{
		IExcelWorker SetCell<T>(int col, int row, T value);
		T GetCell<T>(int col, int row);

		IExcelWorker SetStyle(int col, Type type);
		IExcelWorker SetStyle(int col, string format);

		IExcelWorker SetConditionalFormatting(int col, ComparisonOperator op, string condition, Color? bgColor, Color? fgColor);

		IExcelWorker Save(string fileName, bool autoSizeColumns);
	}

	public interface IExcelWorkerProvider
	{
		IExcelWorker Create();
		IExcelWorker Create(string sheetName);
	}
}