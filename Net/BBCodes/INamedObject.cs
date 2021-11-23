namespace Ecng.Net.BBCodes
{
	public interface INamedObject<TContext>
	{
		public long Id { get; }
		public string GetName(TContext domain);
		public string GetUrlPart(TContext context);
	}
}