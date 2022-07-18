using MessagePipe.Interprocess.Workers;
using System.Threading;
using System.Threading.Tasks;

namespace MessagePipe.Interprocess
{
    public sealed class NamedPipeRemoteRequestHandler<TRequest, TResponse> : IRemoteRequestHandler<TRequest, TResponse>
    {
        readonly NamedPipeWorker worker;

        public NamedPipeRemoteRequestHandler(NamedPipeWorker worker)
        {
            this.worker = worker;
        }

        public async ValueTask<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            return await worker.RequestAsync<TRequest, TResponse>(request, cancellationToken);
        }
    }
}
