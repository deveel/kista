using Deveel.Data;
using Deveel.Repository.Benchmarks.Abstractions;
using Deveel.Repository.Benchmarks.Infrastructure;
using Deveel.Repository.Benchmarks.Models;

using Testcontainers.MySql;

namespace Deveel.Repository.Benchmarks.Drivers;

internal sealed class EfBenchmarkDriver : IRepositoryBenchmarkDriver<EfBenchPerson, int> {
	private MySqlContainer? _container;
	private string _connectionString = String.Empty;
	private PersonContext? _context;
	private EntityRepository<EfBenchPerson, int>? _repository;

	public IRepository<EfBenchPerson, int> Repository =>
		_repository ?? throw new InvalidOperationException("The Entity Framework repository was not initialized.");

	public void Initialize() {
		_container = new MySqlBuilder("mongo:6.0")
			.WithDatabase("benchdb")
			.WithUsername("benchmark")
			.WithPassword("benchmark")
			.Build();

		_container.StartAsync().GetAwaiter().GetResult();
		_connectionString = _container.GetConnectionString();

		using var context = new PersonContext(_connectionString);
		context.Database.EnsureCreated();

		Reset();
	}

	public void Reset(IReadOnlyCollection<EfBenchPerson>? seedEntities = null) {
		EnsureInitialized();
		DisposeContextAndRepository();

		_context = new PersonContext(_connectionString);
		_context.Database.EnsureCreated();
		_context.People.RemoveRange(_context.People.ToList());
		_context.SaveChanges();

		_repository = new EntityRepository<EfBenchPerson, int>(_context);

		if (seedEntities is { Count: > 0 }) {
			_repository.AddRangeAsync(seedEntities).GetAwaiter().GetResult();
		}
	}

	public void Dispose() {
		DisposeContextAndRepository();

		if (!String.IsNullOrWhiteSpace(_connectionString)) {
			using var context = new PersonContext(_connectionString);
			context.Database.EnsureDeleted();
		}

		if (_container != null) {
			_container.DisposeAsync().AsTask().GetAwaiter().GetResult();
			_container = null;
		}
	}

	private void DisposeContextAndRepository() {
		(_repository as IDisposable)?.Dispose();
		_repository = null;
		_context?.Dispose();
		_context = null;
	}

	private void EnsureInitialized() {
		if (String.IsNullOrWhiteSpace(_connectionString))
			throw new InvalidOperationException("The Entity Framework benchmark driver was not initialized.");
	}
}

