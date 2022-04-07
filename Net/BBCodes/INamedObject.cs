namespace Ecng.Net.BBCodes
{
	using System.Threading;
	using System.Threading.Tasks;

	public interface INamedObject<TContext>
	{
		public long Id { get; }

		public ValueTask<string> GetName(TContext domain, CancellationToken cancellationToken);
		public ValueTask<string> GetDescription(TContext domain, CancellationToken cancellationToken);
		public ValueTask<string> GetUrlPart(TContext context, CancellationToken cancellationToken);
	}
}