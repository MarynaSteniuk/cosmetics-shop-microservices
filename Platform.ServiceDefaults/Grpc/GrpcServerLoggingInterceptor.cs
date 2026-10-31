using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Platform.ServiceDefaults.Correlation;

namespace Platform.ServiceDefaults.Grpc;

public sealed class GrpcServerLoggingInterceptor : Interceptor
{
    private readonly ILogger<GrpcServerLoggingInterceptor> _logger;

    public GrpcServerLoggingInterceptor(ILogger<GrpcServerLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = context.RequestHeaders.GetValue(CorrelationIdMiddleware.HeaderName.ToLowerInvariant());

        Activity.Current?.SetTag("rpc.grpc.correlation_id", correlationId);

        try
        {
            var response = await continuation(request, context);

            _logger.LogInformation(
                "gRPC {GrpcMethod} from {GrpcPeer}: {GrpcStatus} in {ElapsedMs} ms, CorrelationId metadata {GrpcCorrelationId}",
                context.Method, context.Peer, StatusCode.OK, stopwatch.ElapsedMilliseconds, correlationId);
            return response;
        }
        catch (RpcException ex)
        {
            _logger.LogWarning(ex, "gRPC {GrpcMethod} from {GrpcPeer}: {GrpcStatus} in {ElapsedMs} ms, {GrpcDetail}",
                context.Method, context.Peer, ex.StatusCode, stopwatch.ElapsedMilliseconds, ex.Status.Detail);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "gRPC {GrpcMethod} from {GrpcPeer}: {GrpcStatus} in {ElapsedMs} ms",
                context.Method, context.Peer, StatusCode.Internal, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
