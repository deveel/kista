using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;

using MongoFramework;

namespace Kista.Benchmarks.Drivers;

/// <summary>
/// Benchmark test stubs deriving from each driver's concrete repository,
/// implementing <see cref="ITestRepository{TEntity, TKey}"/> so the benchmark
/// base class can drive the protected filterable pipeline through public
/// passthroughs without <c>InternalsVisibleTo</c>.
/// </summary>
internal sealed class BenchmarkInMemoryRepository<TEntity, TKey> : InMemoryRepository<TEntity, TKey>, ITestRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull {

    public BenchmarkInMemoryRepository() : base() { }

    ValueTask<TEntity?> ITestRepository<TEntity, TKey>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
        => FindFirstAsync(query, cancellationToken);

    ValueTask<IReadOnlyList<TEntity>> ITestRepository<TEntity, TKey>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
        => FindAllAsync(query, cancellationToken);

    ValueTask<long> ITestRepository<TEntity, TKey>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => CountAsync(filter, cancellationToken);

    ValueTask<bool> ITestRepository<TEntity, TKey>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => ExistsAsync(filter, cancellationToken);

    IQueryable<TEntity> ITestRepository<TEntity, TKey>.Queryable() => Queryable();
}

internal sealed class BenchmarkEntityRepository<TEntity, TKey> : EntityRepository<TEntity, TKey>, ITestRepository<TEntity, TKey>
    where TEntity : class {

    public BenchmarkEntityRepository(DbContext context) : base(context) { }

    ValueTask<TEntity?> ITestRepository<TEntity, TKey>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
        => FindFirstAsync(query, cancellationToken);

    ValueTask<IReadOnlyList<TEntity>> ITestRepository<TEntity, TKey>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
        => FindAllAsync(query, cancellationToken);

    ValueTask<long> ITestRepository<TEntity, TKey>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => CountAsync(filter, cancellationToken);

    ValueTask<bool> ITestRepository<TEntity, TKey>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => ExistsAsync(filter, cancellationToken);

    IQueryable<TEntity> ITestRepository<TEntity, TKey>.Queryable() => Queryable();
}

internal sealed class BenchmarkMongoRepository<TEntity, TKey> : MongoRepository<TEntity, TKey>, ITestRepository<TEntity, TKey>
    where TEntity : class {

    public BenchmarkMongoRepository(IMongoDbContext context) : base(context, (ILogger<MongoRepository<TEntity, TKey>>?)null, null) { }

    ValueTask<TEntity?> ITestRepository<TEntity, TKey>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
        => FindFirstAsync(query, cancellationToken);

    ValueTask<IReadOnlyList<TEntity>> ITestRepository<TEntity, TKey>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
        => FindAllAsync(query, cancellationToken);

    ValueTask<long> ITestRepository<TEntity, TKey>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => CountAsync(filter, cancellationToken);

    ValueTask<bool> ITestRepository<TEntity, TKey>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => ExistsAsync(filter, cancellationToken);

    IQueryable<TEntity> ITestRepository<TEntity, TKey>.Queryable() => Queryable();
}