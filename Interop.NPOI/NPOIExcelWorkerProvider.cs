namespace Ecng.Interop.NPOI
{
	public class NPOIExcelWorkerProvider : IExcelWorkerProvider
	{
		IExcelWorker IExcelWorkerProvider.Create()
		{
			return new NPOIExcelWorker();
		}

		IExcelWorker IExcelWorkerProvider.Create(string sheetName, bool readOnly)
		{
			return new NPOIExcelWorker(sheetName, readOnly);
		}
	}
}