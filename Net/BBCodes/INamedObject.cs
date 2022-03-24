namespace Ecng.Net.BBCodes
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface INamedObject<TContext>
	{
		public long Id { get; }

		public Task<string> GetName(TContext domain, CancellationToken cancellationToken);
		public Task<string> GetDescription(TContext domain, CancellationToken cancellationToken);
		public Task<string> GetUrlPart(TContext context, CancellationToken cancellationToken);
	}
}