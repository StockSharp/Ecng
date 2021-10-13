namespace Ecng.Net.BBCodes
{
	public interface INamedObject<TDomain>
	{
		public long Id { get; }
		public string GetName(TDomain domain);
	}

	public interface IProductObject<TDomain> : INamedObject<TDomain>
	{
		public string GetUrlPart(TDomain domain);
	}

	public interface IPageObject<TDomain> : IProductObject<TDomain>
	{
		public string GetHeader(TDomain domain);
	}
}