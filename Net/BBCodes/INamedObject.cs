namespace Ecng.Net.BBCodes
{
	public interface INamedObject
	{
		public long Id { get; }
		public string Name { get; }
	}

	public interface IProductObject : INamedObject
	{
		public string PackageId { get; }
	}
}