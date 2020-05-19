namespace System.ServiceModel
{
	public class ExceptionDetail
	{
		public string Type { get; set; }
	}

	public class FaultException<T> : Exception
	{
		public T Detail { get; }
	}
}