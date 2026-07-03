using Kista;

namespace Kista.Benchmarks.Abstractions;

/// <summary>
/// Minimal filterable pipeline interface for benchmarks,
/// exposing only the methods needed by the benchmark base
/// class so driver stubs avoid duplicating the full
/// <see cref="ITestRepository{TEntity, TKey}"/> forwarding pattern.
/// </summary>
internal interface IBenchmarkFilterableRepository<in TEntity, in TKey>
    where TEntity : class {
    ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default);
    ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default);
}