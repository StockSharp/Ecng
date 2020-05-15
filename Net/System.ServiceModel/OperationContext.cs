namespace System.ServiceModel
{
	public class OperationContext
	{
		public static OperationContext Current => throw new NotImplementedException();

		public event EventHandler OperationCompleted;
	}
}