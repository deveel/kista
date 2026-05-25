using Kista;

namespace Kista.Benchmarks.Abstractions;

public interface IRepositoryBenchmarkDriver<TEntity, TKey> : IDisposable
	where TEntity : class {
	IRepository<TEntity, TKey> Repository { get; }

	void Initialize();

	void Reset(IReadOnlyCollection<TEntity>? seedEntities = null);
}

