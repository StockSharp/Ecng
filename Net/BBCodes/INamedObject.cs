namespace Ecng.Net.BBCodes
{
	public interface INamedObject
	{
		public long Id { get; }
		public string GetName(string langCode);
	}

	public interface IProductObject : INamedObject
	{
		public string GetUrlPart(string langCode);
	}

	public interface IPageObject : IProductObject
	{
		public string GetHeader(string langCode);
	}
}