using Microsoft.Extensions.Logging;

using MongoDB.Bson;

using MongoFramework;

namespace Kista;

/// <summary>
/// A test stub deriving from <see cref="MongoRepository{TEntity, TKey}"/>
/// that implements <see cref="ITestRepository{TEntity, TKey}"/> by forwarding
/// the protected filterable pipeline and <c>Queryable()</c> hatch through
/// public passthroughs, so the shared
/// <see cref="RepositoryTestSuite{TPerson, TKey, TRelationship}"/> can exercise
/// the filterable methods without <c>InternalsVisibleTo</c>.
/// </summary>
public class TestMongoPersonRepository : MongoRepository<MongoPerson, ObjectId>, ITestRepository<MongoPerson, ObjectId> {
	public TestMongoPersonRepository(IMongoDbContext context, ILogger<MongoRepository<MongoPerson, ObjectId>>? logger = null, IServiceProvider? services = null)
		: base(context, logger, services) {
	}

	ValueTask<MongoPerson?> ITestRepository<MongoPerson, ObjectId>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
		=> FindFirstAsync(query, cancellationToken);

	ValueTask<IReadOnlyList<MongoPerson>> ITestRepository<MongoPerson, ObjectId>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
		=> FindAllAsync(query, cancellationToken);

	ValueTask<long> ITestRepository<MongoPerson, ObjectId>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
		=> CountAsync(filter, cancellationToken);

	ValueTask<bool> ITestRepository<MongoPerson, ObjectId>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
		=> ExistsAsync(filter, cancellationToken);

	IQueryable<MongoPerson> ITestRepository<MongoPerson, ObjectId>.Queryable() => Queryable();
}

/// <summary>
/// A test stub deriving from the no-key <see cref="MongoRepository{TEntity}"/>
/// that implements <see cref="ITestRepository{TEntity}"/> by forwarding the
/// protected filterable pipeline and <c>Queryable()</c> hatch through public
/// passthroughs, so the shared
/// <see cref="RepositoryTestSuite{TPerson, TRelationship}"/> can exercise the
/// filterable methods without <c>InternalsVisibleTo</c>.
/// </summary>
public class TestMongoPersonNoKeyRepository : MongoRepository<MongoPerson>, ITestRepository<MongoPerson> {
	public TestMongoPersonNoKeyRepository(IMongoDbContext context, ILogger<MongoRepository<MongoPerson>>? logger = null, IServiceProvider? services = null)
		: base(context, logger, services) {
	}

	ValueTask<MongoPerson?> ITestRepository<MongoPerson, object>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
		=> FindFirstAsync(query, cancellationToken);

	ValueTask<IReadOnlyList<MongoPerson>> ITestRepository<MongoPerson, object>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
		=> FindAllAsync(query, cancellationToken);

	ValueTask<long> ITestRepository<MongoPerson, object>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
		=> CountAsync(filter, cancellationToken);

	ValueTask<bool> ITestRepository<MongoPerson, object>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
		=> ExistsAsync(filter, cancellationToken);

	IQueryable<MongoPerson> ITestRepository<MongoPerson, object>.Queryable() => Queryable();
}