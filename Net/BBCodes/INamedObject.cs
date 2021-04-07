namespace Ecng.Net.BBCodes
{
	public interface INamedObject
	{
		public long Id { get; }
		public string GetName(bool isEngish);
	}

	public interface IProductObject : INamedObject
	{
		public string UrlPart { get; }
	}

	public interface IPage : INamedObject
	{
		public string GetHeader(bool isEngish);
		public string GetUrl(bool isEngish);
	}
}