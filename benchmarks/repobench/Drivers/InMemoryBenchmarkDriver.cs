using Kista;
using Kista.Benchmarks.Abstractions;
using Kista.Benchmarks.Models;

namespace Kista.Benchmarks.Drivers;

internal sealed class InMemoryBenchmarkDriver : IRepositoryBenchmarkDriver<InMemoryBenchPerson, string> {
    private InMemoryStub? _repository;

    public IRepository<InMemoryBenchPerson, string> Repository =>
        _repository ?? throw new InvalidOperationException("The in-memory repository was not initialized.");

    public void Initialize() {
        Reset();
    }

    public void Reset(IReadOnlyCollection<InMemoryBenchPerson>? seedEntities = null) {
        _repository?.Dispose();
        _repository = new InMemoryStub();

        if (seedEntities is { Count: > 0 }) {
            _repository.AddRangeAsync(seedEntities).GetAwaiter().GetResult();
        }
    }

    public void Dispose() {
        _repository?.Dispose();
        _repository = null;
    }

    private sealed class InMemoryStub : InMemoryRepository<InMemoryBenchPerson, string>, IBenchmarkFilterableRepository<InMemoryBenchPerson, string> {
        ValueTask<long> IBenchmarkFilterableRepository<InMemoryBenchPerson, string>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
            => CountAsync(filter, cancellationToken);

        ValueTask<bool> IBenchmarkFilterableRepository<InMemoryBenchPerson, string>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
            => ExistsAsync(filter, cancellationToken);
    }
}