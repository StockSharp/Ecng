namespace Ecng.Common
{
	public interface IReferenceCounter
	{
		void IncRef();

		void DecRef();
	}
}