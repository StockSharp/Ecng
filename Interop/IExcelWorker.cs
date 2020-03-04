namespace Ecng.Interop
{
	using System;
	using System.IO;

	using Ecng.ComponentModel;

	public interface IExcelWorker : IDisposable
	{
		IExcelWorker SetCell<T>(int col, int row, T value);
		T GetCell<T>(int col, int row);

		IExcelWorker SetStyle(int col, Type type);
		IExcelWorker SetStyle(int col, string format);

		IExcelWorker SetConditionalFormatting(int col, ComparisonOperator op, string condition, string bgColor, string fgColor);

		IExcelWorker RenameSheet(string name);
		IExcelWorker AddSheet();
		bool ContainsSheet(string name);
		IExcelWorker SwitchSheet(string name);

		int GetColumnsCount();
		int GetRowsCount();
	}

	public interface IExcelWorkerProvider
	{
		IExcelWorker CreateNew(Stream stream, bool readOnly = false);
		IExcelWorker OpenExist(Stream stream);
	}
}