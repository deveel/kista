using Kista;
using Kista.Benchmarks.Abstractions;
using Kista.Benchmarks.Models;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoFramework;

using Testcontainers.MongoDb;

namespace Kista.Benchmarks.Drivers;

internal sealed class MongoBenchmarkDriver : IRepositoryBenchmarkDriver<MongoBenchPerson, ObjectId> {
    private readonly string _databaseName = $"benchdb_{Guid.NewGuid():N}";
    private MongoDbContainer? _container;
    private string _connectionString = String.Empty;
    private ServiceProvider? _serviceProvider;
    private MongoStub? _repository;

    public IRepository<MongoBenchPerson, ObjectId> Repository =>
        _repository ?? throw new InvalidOperationException("The Mongo repository was not initialized.");

    public void Initialize() {
        _container = new MongoDbBuilder("mongo:6.0")
            .WithUsername(String.Empty)
            .WithPassword(String.Empty)
            .Build();

        _container.StartAsync().GetAwaiter().GetResult();
        _connectionString = SetDatabase(_container.GetConnectionString(), _databaseName);

        Reset();
    }

    public void Reset(IReadOnlyCollection<MongoBenchPerson>? seedEntities = null) {
        EnsureInitialized();
        DisposeCurrentRepository();
        DropDatabase();

        var services = new ServiceCollection();
        services.AddRepositoryContext().UseMongoDB<MongoDbContext>(mongoBuilder => mongoBuilder.WithConnection(builder => builder.UseConnection(_connectionString)));
        _serviceProvider = services.BuildServiceProvider();

        var context = _serviceProvider.GetRequiredService<IMongoDbContext>();
        _repository = new MongoStub(context);

        if (seedEntities is { Count: > 0 }) {
            _repository.AddRangeAsync(seedEntities).GetAwaiter().GetResult();
        }
    }

    public void Dispose() {
        DisposeCurrentRepository();
        DropDatabase();

        if (_container != null) {
            _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _container = null;
        }
    }

    private void DisposeCurrentRepository() {
        if (_repository is IAsyncDisposable asyncDisposable) {
            asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        _repository = null;
        _serviceProvider?.Dispose();
        _serviceProvider = null;
    }

    private void DropDatabase() {
        if (String.IsNullOrWhiteSpace(_connectionString))
            return;

        var mongoUrl = new MongoUrl(_connectionString);
        if (String.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            return;

        var client = new MongoClient(_connectionString);
        client.DropDatabase(mongoUrl.DatabaseName);
    }

    private static string SetDatabase(string connectionString, string databaseName) {
        var builder = new MongoUrlBuilder(connectionString) {
            DatabaseName = databaseName
        };

        return builder.ToString();
    }

    private void EnsureInitialized() {
        if (String.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("The Mongo benchmark driver was not initialized.");
    }

    private sealed class MongoStub : MongoRepository<MongoBenchPerson, ObjectId>, IBenchmarkFilterableRepository<MongoBenchPerson, ObjectId> {
        public MongoStub(IMongoDbContext context) : base(context, (ILogger<MongoRepository<MongoBenchPerson, ObjectId>>?)null, null) { }

        ValueTask<long> IBenchmarkFilterableRepository<MongoBenchPerson, ObjectId>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
            => CountAsync(filter, cancellationToken);

        ValueTask<bool> IBenchmarkFilterableRepository<MongoBenchPerson, ObjectId>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
            => ExistsAsync(filter, cancellationToken);
    }
}