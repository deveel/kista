using Deveel;

using Microsoft.Extensions.Logging;

namespace Kista.SampleApp.OperationPipeline.Interceptors;

/// <summary>
/// An interceptor that records every successful write operation
/// in <see cref="IEntityManagerInterceptor{TEntity, TKey}.PostWriteAsync"/>,
/// demonstrating the after-write slot of the operation pipeline.
/// </summary>
public class AuditInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private readonly ILogger<AuditInterceptor<TEntity, TKey>> _logger;

    public AuditInterceptor(ILogger<AuditInterceptor<TEntity, TKey>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context)
        => default;

    /// <inheritdoc/>
    public ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result)
    {
        if (result.IsSuccess())
        {
            _logger.LogInformation(
                "AUDIT: {Kind} on {EntityType} (Id={Key}) by {Actor} at {Timestamp:O}",
                context.Kind,
                typeof(TEntity).Name,
                context.Key,
                context.Actor ?? "anonymous",
                context.Timestamp);
        }

        return ValueTask.CompletedTask;
    }
}