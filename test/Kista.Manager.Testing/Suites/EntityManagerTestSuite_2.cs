using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Kista;

public abstract class EntityManagerTestSuite<TManager, TPerson> : EntityManagerTestSuiteBase<TPerson, string>, IAsyncLifetime
	where TManager : EntityManager<TPerson>
	where TPerson : class, IPerson, new() {
	protected EntityManagerTestSuite(ITestOutputHelper testOutput) : base(testOutput) {
	}

	protected TManager Manager => Services.GetRequiredService<TManager>();

	protected IRepository<TPerson> Repository => Services.GetRequiredService<IRepository<TPerson>>();

	/// <inheritdoc/>
	public virtual ValueTask InitializeAsync() => InitializeCoreAsync();

	/// <inheritdoc/>
	public virtual ValueTask DisposeAsync() => DisposeCoreAsync();

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
		=> await Manager.FindAsync(key);

	protected override async ValueTask<TPerson?> FindInRepositoryAsync(object key)
		=> await Repository.FindAsync(key);

	protected override async ValueTask AddRangeToRepositoryAsync(IEnumerable<TPerson> people)
		=> await Repository.AddRangeAsync(people);

	protected override async ValueTask<PageResult<TPerson>> GetPageAsync(PageRequest request)
		=> await Repository.GetPageAsync(request);

	protected override async ValueTask<PageResult<TPerson>> GetPageAsync(PageQuery<TPerson> query)
		=> await Repository.GetPageAsync(query);

	/// <summary>
	/// Generates a key that is guaranteed not to match any entity in the
	/// repository. The default no-key <see cref="EntityManager{TEntity}"/>
	/// suite does not depend on the key shape, so a string GUID is used.
	/// </summary>
	protected abstract string GenerateKey();

	protected override object GenerateUnknownKey() => GenerateKey();

	protected override TPerson CreatePersonWithUnknownKey()
		=> PersonFaker
			.RuleFor(x => x.Id, f => GenerateKey())
			.Generate();

	protected override void ConfigureServices(IServiceCollection services) {
		TestServiceRegistration.RegisterManager(services, typeof(TManager));
		TestServiceRegistration.RegisterValidator(services, typeof(PersonValidator<TPerson>));
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public override async Task Should_UpdateEntity_When_EntityIsValid() {
		// Arrange
		var person = People.Random();
		Assert.NotNull(person);
		Assert.NotNull(person.Id);

		var copy = (TPerson)(typeof(TPerson)
			.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(person, new object[0])!);

		copy.Email = new Bogus.Faker().Internet.Email();

		// Act
		var result = await Manager.UpdateAsync(copy);

		// Assert
		Assert.False(result.HasValidationErrors());
		Assert.True(result.IsSuccess());
		Assert.NotNull(copy.UpdatedAtUtc);
		Assert.Equal(TestTime.UtcNow, copy.UpdatedAtUtc.Value);
	}

	[Fact]
	[Trait("Category", "Integration")]
	[Trait("Layer", "Application")]
	[Trait("Feature", "Manager")]
	public override async Task Should_ReturnUnchanged_When_UpdateWithNoChanges() {
		// Arrange
		var person = People.Random();
		Assert.NotNull(person);

		var toUpdate = await Repository.FindAsync(person.Id!);
		Assert.NotNull(toUpdate);

		// Act
		var result = await Manager.UpdateAsync(toUpdate);

		// Assert
		Assert.True(result.IsUnchanged());
		Assert.False(result.IsSuccess());
		Assert.Null(result.Error);
	}
}