using Deveel;

namespace Kista.SampleApp.OperationPipeline.Interceptors;

/// <summary>
/// An interceptor that short-circuits write operations outside
/// business hours (09:00–18:00 UTC), demonstrating the
/// <see cref="IEntityManagerInterceptor{TEntity, TKey}.PreWriteAsync"/>
/// short-circuit capability of the operation pipeline.
/// </summary>
/// <remarks>
/// When a write is attempted outside business hours, the interceptor
/// returns a failed <see cref="IOperationResult"/> from
/// <see cref="PreWriteAsync"/>, which skips the repository write and
/// all downstream interceptors. The caller receives the error result
/// — no exception is thrown.
/// </remarks>
public class BusinessHoursInterceptor<TEntity, TKey> : IEntityManagerInterceptor<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private static readonly OperationError OutsideBusinessHours = new(
        "OUTSIDE_BUSINESS_HOURS",
        "OperationPipeline",
        "Writes are only allowed between 09:00 and 18:00 UTC.");

    /// <inheritdoc/>
    public ValueTask<IOperationResult?> PreWriteAsync(IEntityOperationContext<TEntity, TKey> context)
    {
        var hour = context.Timestamp.Hour;

        if (hour < 9 || hour >= 18)
        {
            return new ValueTask<IOperationResult?>(
                OperationResult.Fail(OutsideBusinessHours));
        }

        return default;
    }

    /// <inheritdoc/>
    public ValueTask PostWriteAsync(IEntityOperationContext<TEntity, TKey> context, IOperationResult result)
        => ValueTask.CompletedTask;
}