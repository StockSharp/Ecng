namespace Ecng.Serialization;

public interface IStorageTransaction : IDisposable
{
	void Commit();
}