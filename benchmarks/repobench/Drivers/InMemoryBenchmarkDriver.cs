using Kista;
using Kista.Benchmarks.Abstractions;
using Kista.Benchmarks.Models;

namespace Kista.Benchmarks.Drivers;

internal sealed class InMemoryBenchmarkDriver : IRepositoryBenchmarkDriver<InMemoryBenchPerson, string> {
	private InMemoryRepository<InMemoryBenchPerson, string>? _repository;

	public IRepository<InMemoryBenchPerson, string> Repository =>
		_repository ?? throw new InvalidOperationException("The in-memory repository was not initialized.");

	public void Initialize() {
		Reset();
	}

	public void Reset(IReadOnlyCollection<InMemoryBenchPerson>? seedEntities = null) {
		_repository?.Dispose();
		_repository = new InMemoryRepository<InMemoryBenchPerson, string>();

		if (seedEntities is { Count: > 0 }) {
			_repository.AddRangeAsync(seedEntities).GetAwaiter().GetResult();
		}
	}

	public void Dispose() {
		_repository?.Dispose();
		_repository = null;
	}
}

