namespace Ecng.Interop.NPOI
{
	public class NPOIExcelWorkerProvider : IExcelWorkerProvider
	{
		IExcelWorker IExcelWorkerProvider.Create()
		{
			return new NPOIExcelWorker();
		}

		IExcelWorker IExcelWorkerProvider.Create(string sheetName)
		{
			return new NPOIExcelWorker(sheetName);
		}
	}
}