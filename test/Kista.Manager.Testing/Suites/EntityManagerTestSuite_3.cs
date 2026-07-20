using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Kista;

// NOSONAR: S2436 — the 3rd generic parameter (TKey) is intentional and required
// to model both the keyed (TPerson<TKey>) and no-key (TPerson) manager variants
// from a single suite hierarchy; collapsing below 3 type parameters would
// force duplicating the entire suite.
public abstract class EntityManagerTestSuite<TManager, TPerson, TKey> : EntityManagerTestSuiteBase<TPerson, TKey>, IAsyncLifetime
	where TManager : EntityManager<TPerson, TKey>
	where TPerson : class, IPerson<TKey>, new()
	where TKey : notnull {
	protected EntityManagerTestSuite(ITestOutputHelper testOutput) : base(testOutput) {
	}

	protected TManager Manager => Services.GetRequiredService<TManager>();

	protected IRepository<TPerson, TKey> Repository => Services.GetRequiredService<IRepository<TPerson, TKey>>();

	protected abstract TKey GenerateKey();

	protected abstract void SetKey(TPerson person, TKey key);

	/// <inheritdoc/>
	public virtual async ValueTask InitializeAsync()
		=> await InitializeCoreAsync();

	/// <summary>
	/// A no-op hook that subclasses can override to run custom cleanup
	/// after the test class has finished. This is intentionally separate
	/// from <see cref="IAsyncDisposable.DisposeAsync"/> to preserve the
	/// original suite's two-method disposal surface.
	/// </summary>
	public virtual async Task DisposeAsync() {
		await Task.CompletedTask;
	}

	/// <inheritdoc/>
	async ValueTask IAsyncDisposable.DisposeAsync()
		=> await DisposeCoreAsync();

	protected override async ValueTask<OperationResult> AddAsync(TPerson person)
		=> await Manager.AddAsync(person);

	protected override async ValueTask<OperationResult> AddRangeAsync(IEnumerable<TPerson> people)
		=> await Manager.AddRangeAsync(people);

	protected override async ValueTask<OperationResult> UpdateAsync(TPerson person)
		=> await Manager.UpdateAsync(person);

	protected override async ValueTask<OperationResult> RemoveAsync(TPerson person)
		=> await Manager.RemoveAsync(person);

	protected override async ValueTask<OperationResult> RemoveRangeAsync(IEnumerable<TPerson> people)
		=> await Manager.RemoveRangeAsync(people);

	protected override async ValueTask<OperationResult<TPerson>> FindByKeyAsync(object key)
		=> await Manager.FindAsync((TKey)key);

	protected override async ValueTask<TPerson?> FindInRepositoryAsync(object key)
		=> await Repository.FindAsync((TKey)key);

	protected override async ValueTask AddRangeToRepositoryAsync(IEnumerable<TPerson> people)
		=> await Repository.AddRangeAsync(people);

	protected override async ValueTask<PageResult<TPerson>> GetPageAsync(PageRequest request)
		=> await Repository.GetPageAsync(request);

	protected override async ValueTask<PageResult<TPerson>> GetPageAsync(PageQuery<TPerson> query)
		=> await Repository.GetPageAsync(query);

	protected override object GenerateUnknownKey() => GenerateKey();

	protected override TPerson CreatePersonWithUnknownKey() {
		var person = PersonFaker.Generate();
		SetKey(person, GenerateKey());
		return person;
	}

	protected override void ConfigureServices(IServiceCollection services) {
		services.AddSingleton<IEqualityComparer<TPerson>, PersonComparer<TPerson, TKey>>();
		TestServiceRegistration.RegisterManager(services, typeof(TManager));
		TestServiceRegistration.RegisterValidator(services, typeof(PersonValidator<TPerson, TKey>));
	}
}