namespace System.ServiceModel
{
	public interface IClientChannel
	{
		void Open();
		void Close();
		void Abort();
	}
}