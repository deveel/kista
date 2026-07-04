using BenchmarkDotNet.Attributes;

using Kista;

using System.Linq.Expressions;

using Kista.Benchmarks.Abstractions;

namespace Kista.Benchmarks;

public abstract class RepositoryCoreBenchmarksBase<TEntity, TKey>
	where TEntity : class {
	[Params(10, 100, 1_000)]
	public int EntityCount { get; set; }

	[Params(25, 100)]
	public int BatchSize { get; set; }

	private IRepositoryBenchmarkDriver<TEntity, TKey> _driver = default!;
	private List<TEntity> _seedTemplate = default!;
	private List<TEntity> _addRangeTemplate = default!;
	private TEntity _singleToAdd = default!;
	private List<TEntity> _batchToAdd = default!;
	private TKey _targetId = default!;
	private TEntity _targetForUpdate = default!;
	private TEntity _targetForRemove = default!;

	protected IRepository<TEntity, TKey> Repository => _driver.Repository;

	protected abstract IRepositoryBenchmarkDriver<TEntity, TKey> CreateDriver();

	protected abstract TEntity CreateSeedEntity(int index);

	protected abstract TEntity CreateSingleEntity();

	protected abstract TEntity CloneForInsert(TEntity entity);

	protected abstract TEntity CreateUpdatedEntity(TEntity entity);

	protected abstract Expression<Func<TEntity, bool>> CountPredicate { get; }

	protected abstract Expression<Func<TEntity, bool>> ExistsPredicate { get; }

	[GlobalSetup]
	public void GlobalSetup() {
		_driver = CreateDriver();
		_driver.Initialize();

		_seedTemplate = Enumerable.Range(0, EntityCount)
			.Select(CreateSeedEntity)
			.ToList();

		_addRangeTemplate = Enumerable.Range(EntityCount, Math.Max(EntityCount, BatchSize))
			.Select(CreateSeedEntity)
			.ToList();
	}

	[IterationSetup(Target = nameof(AddAsync_One))]
	public void SetupForAddOne() {
		_driver.Reset();
		_singleToAdd = CreateSingleEntity();
	}

	[IterationSetup(Target = nameof(AddRangeAsync_Batch))]
	public void SetupForAddRange() {
		_driver.Reset();
		_batchToAdd = _addRangeTemplate
			.Take(BatchSize)
			.Select(CloneForInsert)
			.ToList();
	}

	[IterationSetup(
		Targets =
		[
			nameof(FindAsync_ByKey),
			nameof(UpdateAsync_Entity),
			nameof(RemoveAsync_Entity),
			nameof(CountAsync_Filtered),
			nameof(ExistsAsync_Filtered)
		])]
	public void SetupForSeededOperations() {
		var seeded = _seedTemplate
			.Select(CloneForInsert)
			.ToList();

		_driver.Reset(seeded);

		var middle = seeded[seeded.Count / 2];
		_targetId = Repository.GetEntityKey(middle)!;
		_targetForUpdate = CreateUpdatedEntity(middle);
		_targetForRemove = seeded[^1];
	}

	[GlobalCleanup]
	public void GlobalCleanup() {
		_driver.Dispose();
	}

	[Benchmark]
	public async Task AddAsync_One() => await Repository.AddAsync(_singleToAdd);

	[Benchmark]
	public async Task AddRangeAsync_Batch() => await Repository.AddRangeAsync(_batchToAdd);

	[Benchmark]
	public async Task<TEntity?> FindAsync_ByKey() => await Repository.FindAsync(_targetId);

	[Benchmark]
	public async Task<bool> UpdateAsync_Entity() => await Repository.UpdateAsync(_targetForUpdate);

	[Benchmark]
	public async Task<bool> RemoveAsync_Entity() => await Repository.RemoveAsync(_targetForRemove);

	[Benchmark]
	public Task<long> CountAsync_Filtered() => ((IBenchmarkFilterableRepository<TEntity, TKey>)Repository).CountAsync(QueryFilter.Where(CountPredicate)).AsTask();

	[Benchmark]
	public Task<bool> ExistsAsync_Filtered() => ((IBenchmarkFilterableRepository<TEntity, TKey>)Repository).ExistsAsync(QueryFilter.Where(ExistsPredicate)).AsTask();
}

