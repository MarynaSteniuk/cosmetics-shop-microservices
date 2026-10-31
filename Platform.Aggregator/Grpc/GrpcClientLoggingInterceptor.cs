using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Platform.Aggregator.Grpc;

public sealed class GrpcClientLoggingInterceptor : Interceptor
{
    private readonly ILogger<GrpcClientLoggingInterceptor> _logger;

    public GrpcClientLoggingInterceptor(ILogger<GrpcClientLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            LogResponseAsync(call.ResponseAsync, context.Method.FullName, context.Host, stopwatch),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> LogResponseAsync<TResponse>(Task<TResponse> responseTask, string method, string? host,
        Stopwatch stopwatch)
    {
        try
        {
            var response = await responseTask;
            _logger.LogInformation("gRPC client {GrpcMethod} to {GrpcHost}: {GrpcStatus} in {ElapsedMs} ms",
                method, host, StatusCode.OK, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogWarning("gRPC client {GrpcMethod} to {GrpcHost}: {GrpcStatus} in {ElapsedMs} ms, {GrpcDetail}",
                method, host, ex.StatusCode, stopwatch.ElapsedMilliseconds, ex.Status.Detail);
            throw;
        }
    }
}
