using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace MessagePipe.Interprocess.Workers;

public sealed class NamedPipeWorker : IDisposable
{
    readonly struct ChannelMsg
    {
        public byte[] Data { get; init; }
        public CancellationToken Token { get; init; }
    }

    readonly IServiceProvider _provider;
    readonly ILogger<NamedPipeWorker> _log;
    readonly CancellationTokenSource _workerCts;
    readonly IAsyncPublisher<IInterprocessKey, IInterprocessValue> _publisher;
    readonly MessagePipeInterprocessNamedPipeOptions _options;
    private readonly bool _isServer;

    // Channel is used from publisher for thread safety of write packet
    readonly Channel<ChannelMsg> _channel;

    private byte[]? _inMessageBuffer;

    private bool _workerStarted;
    private bool _workerStopped;
    private bool _pipeWasStarted;

    private TaskCompletionSource<PipeStream> _pipeConnectTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // request-response
    int _requestMsgId;
    readonly ConcurrentDictionary<int, TaskCompletionSource<IInterprocessValue>> _responseCompletions = new();

    // create from DI
    public NamedPipeWorker(IServiceProvider provider, MessagePipeInterprocessNamedPipeOptions options, IAsyncPublisher<IInterprocessKey, IInterprocessValue> publisher, ILogger<NamedPipeWorker> logger)
    {
        _provider = provider;
        _log = logger;
        _workerCts = new CancellationTokenSource();
        _options = options;
        _publisher = publisher;
        _isServer = options.HostAsServer == true;

        _channel = Channel.CreateUnbounded<ChannelMsg>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        if (_isServer)
            EnsurePipe();
    }

    public void Publish<TKey, TMessage>(TKey key, TMessage message)
    {
        EnsureWorkerIsActive();

        var buffer = MessageBuilder.BuildPubSubMessage(key, message, _options.MessagePackSerializerOptions);

        if (!_channel.Writer.TryWrite(new ChannelMsg { Data = buffer, Token = CancellationToken.None }))
            throw new IOException("+cant write to channel");
    }

    public async ValueTask<TResponse> RequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
    {
        EnsureWorkerIsActive();

        var mid = Interlocked.Increment(ref _requestMsgId);
        var tcs = new TaskCompletionSource<IInterprocessValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        _responseCompletions[mid] = tcs;
        var buffer = MessageBuilder.BuildRemoteRequestMessage(typeof(TRequest), typeof(TResponse), mid, request, _options.MessagePackSerializerOptions);

        await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            if(!_channel.Writer.TryWrite(new ChannelMsg { Data = buffer, Token = cancellationToken }))
                throw new IOException("+cant write to channel2");

            var memoryValue = await tcs.Task.ConfigureAwait(false);

            return MessagePackSerializer.Deserialize<TResponse>(memoryValue.ValueMemory, _options.MessagePackSerializerOptions);
        }
    }

    private async Task RunWorkerTask(string name, Func<Task> runner)
    {
        try
        {
            await runner();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException ce || ce.CancellationToken != _workerCts.Token)
                LogMsg(LogLevel.Error, e, "{worker}: stopping with error", name);

            TryStopWorker(e);
        }
    }

    private void EnsureWorkerIsActive()
    {
        bool checkActive()
        {
            if (_workerStopped)
                throw new NamedPipeWorkerStoppedException();

            return _workerStarted;
        }

        if (checkActive())
            return;

        lock (this)
        {
            if (checkActive())
                return;

            _workerStarted = true;
        }

        LogMsg(LogLevel.Debug, "starting send/recv tasks");

        _ = RunWorkerTask("send", RunSendLoop);
        _ = RunWorkerTask("recv", RunReceiveLoop);
    }

    private Task<PipeStream> WhenPipeIsConnected()
    {
        EnsurePipe();
        return _pipeConnectTcs.Task;
    }

    private async Task<NamedPipeServerStream> CreateServerPipe(TimeSpan maxTime, TimeSpan retryInterval)
    {
        var till = DateTime.UtcNow + maxTime;

        do
        {
            try
            {
                return new NamedPipeServerStream(_options.PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            }
            catch (IOException e)
            {
                LogMsg(LogLevel.Warning, e, "server pipe ctor error");

                if (DateTime.UtcNow > till)
                    throw new InvalidOperationException("+unable to create server pipe", e);

                LogMsg(LogLevel.Trace, e, "waiting for {delay}", retryInterval);
                await Task.Delay(retryInterval, _workerCts.Token);
            }
        } while (true);
    }

    private async void EnsurePipe(bool forceRestart = false)
    {
        async Task<PipeStream> createPipe()
        {
            LogMsg(LogLevel.Debug, "creating pipe object");
            return _isServer ?
                await CreateServerPipe(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(300)) :
                new NamedPipeClientStream(_options.ServerName, _options.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        try
        {
            if (_pipeWasStarted && !forceRestart)
                return;

            TaskCompletionSource<PipeStream>? oldTcs = null;

            lock (this)
            {
                if (_pipeWasStarted)
                {
                    if (!forceRestart)
                        return;

                    oldTcs = _pipeConnectTcs;
                    oldTcs.TrySetException(new IOException("+force recreate pipe"));

                    _pipeConnectTcs = new TaskCompletionSource<PipeStream>(TaskCreationOptions.RunContinuationsAsynchronously);
                }
                else
                {
                    var state = _pipeConnectTcs.Task.Status;
                    if (state != TaskStatus.WaitingForActivation)
                        throw new InvalidOperationException($"+unexpected state of connect tcs: {state}");

                    _pipeWasStarted = true;
                }
            }

            if(oldTcs != null)
                await DisposePipe(() => oldTcs.Task, "old on recreate");

            EnsureWorkerIsActive();

            var newPipe = await createPipe();

            try
            {
                LogMsg(LogLevel.Debug, "connecting pipe");

                if (_isServer)
                    await ((NamedPipeServerStream)newPipe).WaitForConnectionAsync(_workerCts.Token);
                else
                    await ((NamedPipeClientStream)newPipe).ConnectAsync(Timeout.Infinite, _workerCts.Token);

                LogMsg(LogLevel.Debug, "pipe is connected!");

                _pipeConnectTcs.TrySetResult(newPipe);
            }
            catch (Exception e)
            {
                var msg = e is OperationCanceledException ? "connection canceled" : $"connection failed {e.GetType().Name} {e.Message}";
                await DisposePipe(() => Task.FromResult(newPipe), msg);

                throw;
            }
        }
        catch(Exception e)
        {
            if (e is OperationCanceledException ce && ce.CancellationToken == _workerCts.Token)
                _pipeConnectTcs.TrySetCanceled();
            else
                _pipeConnectTcs.TrySetException(e);
        }
    }

    async ValueTask RunPipeOperation(Func<PipeStream, Task> doit)
    {
        while (true)
        {
            try
            {
                var pipe = await WhenPipeIsConnected().ConfigureAwait(false);
                EnsureWorkerIsActive();
                await doit(pipe).ConfigureAwait(false);
                return;
            }
            catch (IOException)
            {
                if (_workerCts.IsCancellationRequested)
                    throw;

                EnsurePipe(true);
            }
        }
    }

    private async Task RunSendLoop()
    {
        var reader = _channel.Reader;

        while (await reader.WaitToReadAsync(_workerCts.Token).ConfigureAwait(false))
            while (reader.TryRead(out var item))
                await RunPipeOperation(async pipe =>
                {
                    await pipe.WriteAsync(item.Data, 0, item.Data.Length, item.Token);
                    //LogMsg(LogLevel.Trace, $"sent {item.Data.Length} bytes");
                });
    }

    private async Task RunReceiveLoop()
    {
        while (true)
            await RunPipeOperation(async pipe => await ProcessPipeMessage(pipe, await ReadPipeMessage(pipe)));
        // ReSharper disable once FunctionNeverReturns
    }

    private async Task<ReadOnlyMemory<byte>> ReadPipeMessage(PipeStream pipe)
    {
        ReadOnlyMemory<byte> value;

        _inMessageBuffer ??= new byte[0xffff];

        var readLen = await ReadCheckPipe(pipe, _inMessageBuffer, 0, _inMessageBuffer.Length, _workerCts.Token).ConfigureAwait(false);

        var messageLen = MessageBuilder.FetchMessageLength(_inMessageBuffer);
        if (readLen == (messageLen + 4))
        {
            value = _inMessageBuffer.AsMemory(4, messageLen); // skip length header
        }
        else
        {
            // read more
            if (_inMessageBuffer.Length < messageLen + 4)
                Array.Resize(ref _inMessageBuffer, messageLen + 4);

            var remain = messageLen - (readLen - 4);

            await ReadFullyAsync(_inMessageBuffer, pipe, readLen, remain, _workerCts.Token).ConfigureAwait(false);

            value = _inMessageBuffer.AsMemory(4, messageLen);
        }

        return value;
    }

    async ValueTask ProcessPipeMessage(PipeStream pipe, ReadOnlyMemory<byte> value)
    {
        var message = MessageBuilder.ReadPubSubMessage(value.ToArray()); // can avoid copy?
        switch (message.MessageType)
        {
            case MessageType.PubSub:
            {
                // ReSharper disable once MethodHasAsyncOverload
                _publisher.Publish(message, message);
                break;
            }

            case MessageType.RemoteRequest:
            {
                // NOTE: should use without reflection(Expression.Compile)
                var header = Deserialize<RequestHeader>(message.KeyMemory, _options.MessagePackSerializerOptions);
                var (mid, reqTypeName, resTypeName) = (header.MessageId, header.RequestType, header.ResponseType);
                byte[] resultBytes;
                try
                {
                    var t = AsyncRequestHandlerRegistory.Get(reqTypeName, resTypeName);
                    var interfaceType = t.GetInterfaces().Where(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandler"))
                        .First(x => x.GetGenericArguments().Any(y => y.FullName == header.RequestType));
                    var coreInterfaceType = t.GetInterfaces().Where(x => x.IsGenericType && x.Name.StartsWith("IAsyncRequestHandlerCore"))
                        .First(x => x.GetGenericArguments().Any(y => y.FullName == header.RequestType));
                    var service = _provider.GetRequiredService(interfaceType); // IAsyncRequestHandler<TRequest,TResponse>
                    var genericArgs = interfaceType.GetGenericArguments(); // [TRequest, TResponse]
                    var request = MessagePackSerializer.Deserialize(genericArgs[0], message.ValueMemory, _options.MessagePackSerializerOptions);
                    var responseTask = coreInterfaceType.GetMethod("InvokeAsync")!.Invoke(service, new[] { request, CancellationToken.None });
                    var task = typeof(ValueTask<>).MakeGenericType(genericArgs[1]).GetMethod("AsTask")!.Invoke(responseTask, null);

                    // TODO: cache reflection requests
                    // TODO: this await is not async, we are blocking message processing by this await, may do this asynchronously
                    await ((Task)task!); // Task<T> -> Task

                    var result = task.GetType().GetProperty("Result")!.GetValue(task);
                    resultBytes = MessageBuilder.BuildRemoteResponseMessage(mid, genericArgs[1], result!, _options.MessagePackSerializerOptions);
                }
                catch (Exception ex)
                {
                    // NOTE: ok to send stacktrace?
                    resultBytes = MessageBuilder.BuildRemoteResponseError(mid, ex.ToString(), _options.MessagePackSerializerOptions);
                }

                // no need to RunPipeOperation. if it fails we'll reconnect anyway
                await pipe.WriteAsync(resultBytes, 0, resultBytes.Length).ConfigureAwait(false);

                break;
            }

            case MessageType.RemoteResponse:
            case MessageType.RemoteError:
            {
                var mid = Deserialize<int>(message.KeyMemory, _options.MessagePackSerializerOptions);
                if (_responseCompletions.TryRemove(mid, out var tcs))
                {
                    if (message.MessageType == MessageType.RemoteResponse)
                    {
                        tcs.TrySetResult(message); // synchronous completion, use memory buffer immediately.
                    }
                    else
                    {
                        var errorMsg = MessagePackSerializer.Deserialize<string>(message.ValueMemory, _options.MessagePackSerializerOptions);
                        tcs.TrySetException(new RemoteRequestException(errorMsg));
                    }
                }
                break;
            }
        }
    }

    // omajinai.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static T Deserialize<T>(ReadOnlyMemory<byte> buffer, MessagePackSerializerOptions options)
    {
        if (buffer.IsEmpty && MemoryMarshal.TryGetArray(buffer, out var segment))
        {
            buffer = segment;
        }
        return MessagePackSerializer.Deserialize<T>(buffer, options);
    }

    async ValueTask ReadFullyAsync(byte[] buffer, PipeStream stream, int index, int remain, CancellationToken token)
    {
        while (remain > 0)
        {
            var len = await ReadCheckPipe(stream, buffer, index, remain, token).ConfigureAwait(false);

            index += len;
            remain -= len;
        }
    }

    private async ValueTask<int> ReadCheckPipe(PipeStream pipe, byte[] buf, int offset, int count, CancellationToken token)
    {
        if (!pipe.IsConnected)
            throw new IOException("+pipe is not connected");

        var len = await pipe.ReadAsync(buf, offset, count, token).ConfigureAwait(false);

        if (len == 0)
            throw new IOException("+pipe read returned zero");

        //LogMsg(LogLevel.Trace, $"received {len} bytes");

        return len;
    }

    class NamedPipeWorkerStoppedException : ObjectDisposedException
    {
        public NamedPipeWorkerStoppedException() : base(nameof(NamedPipeWorker)) { }
    }

    private async ValueTask DisposePipe(Func<Task<PipeStream>> getPipe, string name)
    {
        try
        {
            var pipe = await getPipe();
            LogMsg(LogLevel.Debug, "disposing pipe: {name}", name);
            await pipe.DisposeAsync();
        }
        catch
        {
            // ignored
        }
    }

    private void TryStopWorker(Exception? e)
    {
        lock (this)
        {
            if (_workerStopped)
                return;

            _workerStopped = true;
        }

        try
        {
            if (e is OperationCanceledException ce && ce.CancellationToken == _workerCts.Token)
            {
                LogMsg(LogLevel.Debug, "stopping worker: canceled");
                e = null;
            }
            else if(e != null)
            {
                LogMsg(LogLevel.Error, e, "stopping worker with error");
            }
            else
            {
                LogMsg(LogLevel.Debug, "stopping worker");
            }

            _channel.Writer.TryComplete();
            _workerCts.Cancel();

            DisposePipe(() => _pipeConnectTcs.Task, "main(in dispose)").GetAwaiter().GetResult();

            //_cancellationTokenSource.Dispose();

            foreach (var item in _responseCompletions)
            {
                try
                {
                    if (e is null)
                        item.Value.TrySetCanceled();
                    else
                        item.Value.TrySetException(e);
                }
                catch
                {
                    // ignored
                }
            }

            if (e != null)
                _options.UnhandledErrorHandler("unhandled exception", e);
        }
        catch
        {
            // do nothing
        }
    }

    public void Dispose() => TryStopWorker(null);

    void LogMsg(LogLevel level, string m, params object[] args) => LogMsg(level, null, m, args);

    void LogMsg(LogLevel level, Exception? e, string m, params object[] args)
    {
        const string format = "({pipeType} {pipeName}): ";
        var pipeType = _isServer ? "server" : "client";

        _log.Log(level, e, format + m, new[] { pipeType, _options.PipeName }.Concat(args).ToArray());
    }
}
