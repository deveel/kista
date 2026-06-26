using System.ComponentModel.DataAnnotations;
using Kista.Caching;
using Microsoft.Extensions.Logging;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Kista;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
[Trait("Feature", "EntityManager")]
public class EntityManagerKeyedBehaviorTests
{
    private readonly PersonFaker _faker = new();
    const string DbErrorMessage = "db error";

    public class TestEntity
    {
        [Key]
        public string? Id { get; set; }
        public string? Name { get; set; }
    }

    public class TimestampEntity : IHaveTimeStamp
    {
        [Key]
        public string? Id { get; set; }
        public DateTimeOffset? CreatedAtUtc { get; set; }
        public DateTimeOffset? UpdatedAtUtc { get; set; }
    }

    public class EquatableEntity : IEquatable<EquatableEntity>
    {
        [Key]
        public string? Id { get; set; }
        public string? Name { get; set; }

        public bool Equals(EquatableEntity? other) =>
            other is not null && Id == other.Id && Name == other.Name;
    }

    [Fact]
    public void Should_ReturnTrue_When_SupportsPaging()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.True(manager.SupportsPaging);
    }

    [Fact]
    public void Should_ReturnTrue_When_SupportsQueriesForQueryableRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>, IQueryableRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.True(manager.SupportsQueries);
    }

    [Fact]
    public void Should_ReturnFalse_When_SupportsQueriesForNonQueryableRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.False(manager.SupportsQueries);
    }

    [Fact]
    public void Should_ReturnTrue_When_SupportsFiltersForFilterableRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>, IFilterableRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.True(manager.SupportsFilters);
    }

    [Fact]
    public void Should_ReturnFalse_When_SupportsFiltersForNonFilterableRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.False(manager.SupportsFilters);
    }

    [Fact]
    public void Should_ReturnTrue_When_SupportsTrackingForTrackingRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>, ITrackingRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.True(manager.SupportsTracking);
    }

    [Fact]
    public void Should_ReturnFalse_When_SupportsTrackingForNonTrackingRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.False(manager.SupportsTracking);
    }

    [Fact]
    public void Should_ReturnTrue_When_IsTrackingChangesForTrackingRepo()
    {
        var trackingRepo = Substitute.For<IRepository<Person, string>, ITrackingRepository<Person, string>>();
        ((ITrackingRepository<Person, string>)trackingRepo).IsTrackingChanges.Returns(true);
        var manager = new EntityManager<Person, string>(trackingRepo);

        Assert.True(manager.IsTrackingChanges);
    }

    [Fact]
    public void Should_ReturnFalse_When_IsTrackingChangesForNonTrackingRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        Assert.False(manager.IsTrackingChanges);
    }

    [Fact]
    public void Should_ThrowNotSupported_When_EntitiesAccessedForNonQueryableRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        Assert.Throws<NotSupportedException>(() => manager.ExposedEntities);
    }

    [Fact]
    public void Should_ThrowNotSupported_When_FilterableRepositoryAccessedForNonFilterableRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        Assert.Throws<NotSupportedException>(() => manager.ExposedFilterableRepository);
    }

    [Fact]
    public void Should_ThrowNotSupported_When_TrackingRepositoryAccessedForNonTrackingRepo()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        Assert.Throws<NotSupportedException>(() => manager.ExposedTrackingRepository);
    }

    [Fact]
    public void Should_UseRepositoryComparer_When_RepositoryIsIEqualityComparer()
    {
        var repo = Substitute.For<IRepository<Person, string>, IEqualityComparer<Person>>();
        var manager = new ExposedEntityManager(repo);

        var comparer = manager.ExposedEntityComparer;

        Assert.Same(repo, comparer);
    }

    [Fact]
    public void Should_UseServiceComparer_When_RepositoryIsNotComparer()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var services = new ServiceCollection();
        var customComparer = Substitute.For<IEqualityComparer<Person>>();
        services.AddSingleton(customComparer);
        var provider = services.BuildServiceProvider();
        var manager = new ExposedEntityManager(repo, services: provider);

        var comparer = manager.ExposedEntityComparer;

        Assert.Same(customComparer, comparer);
    }

    [Fact]
    public void Should_UseDefaultComparer_When_NoComparerAvailable()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        var comparer = manager.ExposedEntityComparer;

        Assert.Same(EqualityComparer<Person>.Default, comparer);
    }

    [Fact]
    public void Should_GenerateCacheKey_WithoutGenerator()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        var key = manager.ExposedGenerateCacheKey("test123");

        Assert.Equal("person:test123", key);
    }

    [Fact]
    public void Should_GenerateCacheKey_WithGenerator()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var generator = Substitute.For<IEntityCacheKeyGenerator<Person>>();
        generator.GenerateKey("test123").Returns("custom:test123");
        var services = new ServiceCollection();
        services.AddSingleton(generator);
        var provider = services.BuildServiceProvider();
        var manager = new ExposedEntityManager(repo, services: provider);

        var key = manager.ExposedGenerateCacheKey("test123");

        Assert.Equal("custom:test123", key);
    }

    [Fact]
    public void Should_GenerateCacheKeys_WithoutGenerator()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);
        var entity = new Person();

        var keys = manager.ExposedGenerateCacheKeys(entity);

        Assert.Empty(keys);
    }

    [Fact]
    public void Should_GenerateCacheKeys_WithGenerator()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var generator = Substitute.For<IEntityCacheKeyGenerator<Person>>();
        generator.GenerateAllKeys(Arg.Any<Person>()).Returns(new[] { "key1", "key2" });
        var services = new ServiceCollection();
        services.AddSingleton(generator);
        var provider = services.BuildServiceProvider();
        var manager = new ExposedEntityManager(repo, services: provider);
        var entity = new Person();

        var keys = manager.ExposedGenerateCacheKeys(entity);

        Assert.Equal(2, keys.Length);
    }

    [Fact]
    public void Should_UseErrorFactory_When_Available()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var errorFactory = Substitute.For<IOperationErrorFactory<Person>>();
        errorFactory.CreateError("ERR", "Person", "msg").Returns(new OperationError("ERR", "Person", "msg"));
        var manager = new ExposedEntityManager(repo, errorFactory: errorFactory);

        var error = manager.ExposedOperationError("ERR", "msg");

        Assert.Equal("ERR", error.Code);
    }

    [Fact]
    public void Should_UseDefaultError_When_NoErrorFactory()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        var error = manager.ExposedOperationError("ERR", "msg");

        Assert.Equal("ERR", error.Code);
    }

    [Fact]
    public void Should_UseErrorFactory_ForValidationError()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var errorFactory = Substitute.For<IOperationErrorFactory<Person>>();
        var validationResults = new List<ValidationResult> { new("invalid") };
        errorFactory.CreateValidationError("ERR", "Person", validationResults)
            .Returns(new OperationValidationError("ERR", "Person", validationResults));
        var manager = new ExposedEntityManager(repo, errorFactory: errorFactory);

        var error = manager.ExposedValidationError("ERR", validationResults);

        Assert.Equal("ERR", error.Code);
    }

    [Fact]
    public void Should_UseDefaultValidationError_When_NoErrorFactory()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);
        var validationResults = new List<ValidationResult> { new("invalid") };

        var error = manager.ExposedValidationError("ERR", validationResults);

        Assert.Equal("ERR", error.Code);
    }

    [Fact]
    public void Should_Fail_WithOperationException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        var result = manager.ExposedFail(new OperationException("ERR", "Person", "msg"));

        Assert.False(result.IsSuccess());
        Assert.Equal("ERR", result.Error?.Code);
    }

    [Fact]
    public void Should_ReturnEmptyValidation_When_NoValidator()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        var results = manager.ExposedValidateAsync(new Person(), default).GetAwaiter().GetResult();

        Assert.Empty(results);
    }

    [Fact]
    public void Should_ReturnValidationErrors_When_ValidatorFails()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var validator = Substitute.For<IEntityValidator<Person, string>>();
        var validationResults = new List<ValidationResult> { new("invalid") };
        validator.ValidateAsync(Arg.Any<EntityManager<Person, string>>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(validationResults));
        var manager = new ExposedEntityManager(repo, validator: validator);

        var results = manager.ExposedValidateAsync(new Person(), default).GetAwaiter().GetResult();

        Assert.Single(results);
    }

    [Fact]
    public void Should_SetTimestamp_When_OnAddingEntityWithIHaveTimeStamp()
    {
        var repo = Substitute.For<IRepository<TimestampEntity, string>>();
        var time = Substitute.For<ISystemTime>();
        var now = DateTimeOffset.UtcNow;
        time.UtcNow.Returns(now);
        var manager = new ExposedEntityManager<TimestampEntity, string>(repo, systemTime: time);
        var entity = new TimestampEntity();

        var result = manager.ExposedOnAddingEntityAsync(entity).GetAwaiter().GetResult();

        Assert.Equal(now, result.CreatedAtUtc);
    }

    [Fact]
    public void Should_SetTimestamp_When_OnUpdatingEntityWithIHaveTimeStamp()
    {
        var repo = Substitute.For<IRepository<TimestampEntity, string>>();
        var time = Substitute.For<ISystemTime>();
        var now = DateTimeOffset.UtcNow;
        time.UtcNow.Returns(now);
        var manager = new ExposedEntityManager<TimestampEntity, string>(repo, systemTime: time);
        var entity = new TimestampEntity();

        var result = manager.ExposedOnUpdatingEntityAsync(entity).GetAwaiter().GetResult();

        Assert.Equal(now, result.UpdatedAtUtc);
    }

    [Fact]
    public void Should_ReturnTrue_When_AreEqualBothNull()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        Assert.True(manager.ExposedAreEqual(null!, null!));
    }

    [Fact]
    public void Should_ReturnFalse_When_AreEqualExistingNull()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);

        Assert.False(manager.ExposedAreEqual(null!, new Person()));
    }

    [Fact]
    public void Should_UseIEquatable_When_EntityIsIEquatable()
    {
        var repo = Substitute.For<IRepository<EquatableEntity, string>>();
        var manager = new ExposedEntityManager<EquatableEntity, string>(repo);
        var a = new EquatableEntity { Id = "1", Name = "A" };
        var b = new EquatableEntity { Id = "1", Name = "A" };

        Assert.True(manager.ExposedAreEqual(a, b));
    }

    [Fact]
    public void Should_UseComparer_When_EntityIsNotIEquatable()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var comparer = Substitute.For<IEqualityComparer<Person>>();
        comparer.Equals(Arg.Any<Person>(), Arg.Any<Person>()).Returns(true);
        var services = new ServiceCollection();
        services.AddSingleton(comparer);
        var provider = services.BuildServiceProvider();
        var manager = new ExposedEntityManager(repo, services: provider);
        var a = new Person { Id = "1" };
        var b = new Person { Id = "1" };

        Assert.True(manager.ExposedAreEqual(a, b));
    }

    [Fact]
    public async Task Should_AddAsync_WithValidationFailure()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var validator = Substitute.For<IEntityValidator<Person, string>>();
        validator.ValidateAsync(Arg.Any<EntityManager<Person, string>>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<ValidationResult> { new("invalid") }));
        var manager = new EntityManager<Person, string>(repo, validator: validator);

        var result = await manager.AddAsync(new Person(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(EntityErrorCodes.NotValid, result.Error?.Code);
    }

    [Fact]
    public async Task Should_AddAsync_WithException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.When(x => x.AddAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException(DbErrorMessage));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.AddAsync(new Person(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_AddRangeAsync_WithValidationFailure()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var validator = Substitute.For<IEntityValidator<Person, string>>();
        validator.ValidateAsync(Arg.Any<EntityManager<Person, string>>(), Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(ToAsyncEnumerable(new List<ValidationResult> { new("invalid") }));
        var manager = new EntityManager<Person, string>(repo, validator: validator);

        var result = await manager.AddRangeAsync(new[] { new Person() }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_AddRangeAsync_WithException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.When(x => x.AddRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException(DbErrorMessage));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.AddRangeAsync(new[] { new Person() }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_UpdateAsync_WithNullKey()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns((string?)null);
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.UpdateAsync(new Person(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_UpdateAsync_WithExistingNull()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>((Person?)null));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.UpdateAsync(new Person { Id = "1" }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(EntityErrorCodes.NotFound, result.Error?.Code);
    }

    [Fact]
    public async Task Should_UpdateAsync_WithEqualEntities()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        var existing = new Person { Id = "1", FirstName = "A" };
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>(existing));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.UpdateAsync(existing, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_UpdateAsync_WithUpdateFailure()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        var existing = new Person { Id = "1", FirstName = "Old" };
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>(existing));
        repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()).Returns(new ValueTask<bool>(false));
        var manager = new EntityManager<Person, string>(repo);

        existing.FirstName = "New";
        var result = await manager.UpdateAsync(existing, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_UpdateAsync_WithException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        var existing = new Person { Id = "1", FirstName = "Old" };
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>(existing));
        repo.UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<bool>(new InvalidOperationException(DbErrorMessage)));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.UpdateAsync(new Person { Id = "1", FirstName = "New" }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_RemoveAsync_WithNullKey()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns((string?)null);
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.RemoveAsync(new Person(), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_RemoveAsync_WithNotFound()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>((Person?)null));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.RemoveAsync(new Person { Id = "1" }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(EntityErrorCodes.NotFound, result.Error?.Code);
    }

    [Fact]
    public async Task Should_RemoveAsync_WithRemoveFailure()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        var existing = new Person { Id = "1" };
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>(existing));
        repo.RemoveAsync(existing, Arg.Any<CancellationToken>()).Returns(new ValueTask<bool>(false));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.RemoveAsync(new Person { Id = "1" }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_RemoveAsync_WithException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.GetEntityKey(Arg.Any<Person>()).Returns("1");
        var existing = new Person { Id = "1" };
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>(existing));
        repo.RemoveAsync(existing, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<bool>(new InvalidOperationException(DbErrorMessage)));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.RemoveAsync(new Person { Id = "1" }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_RemoveRangeAsync_WithException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.When(x => x.RemoveRangeAsync(Arg.Any<IEnumerable<Person>>(), Arg.Any<CancellationToken>()))
            .Throw(new InvalidOperationException(DbErrorMessage));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.RemoveRangeAsync(new[] { new Person() }, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_FindAsync_WithNullResult()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.FindAsync("1", Arg.Any<CancellationToken>()).Returns(new ValueTask<Person?>((Person?)null));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.FindAsync("1", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(EntityErrorCodes.NotFound, result.Error?.Code);
    }

    [Fact]
    public async Task Should_FindAsync_WithException()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        repo.FindAsync("1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<Person?>(new InvalidOperationException(DbErrorMessage)));
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.FindAsync("1", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_FindFirstAsync_WithUnsupportedFilter()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        var result = await manager.FindFirstAsync(Query.Empty, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(EntityErrorCodes.NotSupported, result.Error?.Code);
    }

    [Fact]
    public async Task Should_FindFirstAsync_WithNullResult()
    {
        var filterable = Substitute.For<IRepository<Person, string>, IFilterableRepository<Person, string>>();
#pragma warning disable S1944 // NSubstitute creates the type dynamically at runtime
        var filterableRepo = (IFilterableRepository<Person, string>)filterable;
#pragma warning restore S1944
        filterableRepo.FindFirstAsync(Arg.Any<IQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Person?>((Person?)null));
        var manager = new EntityManager<Person, string>(filterable);

        var result = await manager.FindFirstAsync(Query.Empty, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
        Assert.Equal(EntityErrorCodes.NotFound, result.Error?.Code);
    }

    [Fact]
    public async Task Should_FindFirstAsync_WithException()
    {
        var filterable = Substitute.For<IRepository<Person, string>, IFilterableRepository<Person, string>>();
#pragma warning disable S1944 // NSubstitute creates the type dynamically at runtime
        var filterableRepo = (IFilterableRepository<Person, string>)filterable;
#pragma warning restore S1944
        filterableRepo.FindFirstAsync(Arg.Any<IQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<Person?>(new InvalidOperationException(DbErrorMessage)));
        var manager = new EntityManager<Person, string>(filterable);

        var result = await manager.FindFirstAsync(Query.Empty, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess());
    }

    [Fact]
    public async Task Should_FindAllAsync_WithUnsupportedFilter()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            manager.FindAllAsync(Query.Empty, TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Should_FindAllAsync_WithException()
    {
        var filterable = Substitute.For<IRepository<Person, string>, IFilterableRepository<Person, string>>();
#pragma warning disable S1944 // NSubstitute creates the type dynamically at runtime
        var filterableRepo = (IFilterableRepository<Person, string>)filterable;
#pragma warning restore S1944
        filterableRepo.FindAllAsync(Arg.Any<IQuery>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException<IReadOnlyList<Person>>(new InvalidOperationException(DbErrorMessage)));
        var manager = new EntityManager<Person, string>(filterable);

        await Assert.ThrowsAsync<OperationException>(() =>
            manager.FindAllAsync(Query.Empty, TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Should_CountAsync_WithUnsupportedFilter()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            manager.CountAsync(QueryFilter.Empty, TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public async Task Should_CountAsync_WithException()
    {
        var repo = new ThrowingFilterableRepo();
        var manager = new EntityManager<Person, string>(repo);

        await Assert.ThrowsAsync<OperationException>(() =>
            manager.CountAsync(QueryFilter.Empty, TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    public void Should_Dispose_WithDisposedFlag()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.SupportsPaging);
    }

    [Fact]
    public async Task Should_DisposeAsync_WithDisposedFlag()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new EntityManager<Person, string>(repo);
        await manager.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => manager.SupportsPaging);
    }

    [Fact]
    public void Should_GetCancellationToken_FromSource()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        using var cts = new CancellationTokenSource();
        var cancellationSource = Substitute.For<IOperationCancellationSource>();
        cancellationSource.Token.Returns(cts.Token);
        var services = new ServiceCollection();
        services.AddSingleton(cancellationSource);
        var provider = services.BuildServiceProvider();
        var manager = new ExposedEntityManager(repo, services: provider);

        var token = manager.ExposedGetCancellationToken(null);

        Assert.Equal(cts.Token, token);
    }

    [Fact]
    public void Should_GetCancellationToken_FromProvided()
    {
        var repo = Substitute.For<IRepository<Person, string>>();
        var manager = new ExposedEntityManager(repo);
        using var cts = new CancellationTokenSource();

        var token = manager.ExposedGetCancellationToken(cts.Token);

        Assert.Equal(cts.Token, token);
    }

    private static async IAsyncEnumerable<ValidationResult> ToAsyncEnumerable(List<ValidationResult> results)
    {
        foreach (var r in results)
        {
            yield return r;
            await Task.CompletedTask;
        }
    }

    public sealed class ExposedEntityManager : EntityManager<Person, string>
    {
        public ExposedEntityManager(
            IRepository<Person, string> repository,
            IEntityValidator<Person, string>? validator = null,
            IEntityCache<Person>? cache = null,
            ISystemTime? systemTime = null,
            IOperationErrorFactory<Person>? errorFactory = null,
            IServiceProvider? services = null)
            : base(repository, validator, cache, systemTime, errorFactory, services) { }

        public IQueryable<Person> ExposedEntities => Entities;
        public IFilterableRepository<Person, string> ExposedFilterableRepository => FilterableRepository;
        public ITrackingRepository<Person, string> ExposedTrackingRepository => TrackingRepository;
        public IEqualityComparer<Person> ExposedEntityComparer => EntityComparer;
        public string ExposedGenerateCacheKey(string key) => GenerateCacheKey(key);
        public string[] ExposedGenerateCacheKeys(Person entity) => GenerateCacheKeys(entity);
        public IOperationError ExposedOperationError(string code, string? msg) => OperationError(code, msg);
        public IValidationError ExposedValidationError(string code, IReadOnlyList<ValidationResult> results) => ValidationError(code, results);
        public OperationResult ExposedFail(OperationException ex) => Fail(ex);
        public ValueTask<IReadOnlyList<ValidationResult>> ExposedValidateAsync(Person entity, CancellationToken ct) => ValidateAsync(entity, ct);
        public ValueTask<Person> ExposedOnAddingEntityAsync(Person entity) => OnAddingEntityAsync(entity);
        public ValueTask<Person> ExposedOnUpdatingEntityAsync(Person entity) => OnUpdatingEntityAsync(entity);
        public bool ExposedAreEqual(Person existing, Person other) => AreEqual(existing, other);
        public CancellationToken ExposedGetCancellationToken(CancellationToken? ct) => GetCancellationToken(ct);
    }

    public sealed class ExposedEntityManager<TEntity, TKey> : EntityManager<TEntity, TKey>
        where TEntity : class
        where TKey : notnull
    {
        public ExposedEntityManager(
            IRepository<TEntity, TKey> repository,
            IEntityValidator<TEntity, TKey>? validator = null,
            IEntityCache<TEntity>? cache = null,
            ISystemTime? systemTime = null,
            IOperationErrorFactory<TEntity>? errorFactory = null,
            IServiceProvider? services = null)
            : base(repository, validator, cache, systemTime, errorFactory, services) { }

        public ValueTask<TEntity> ExposedOnAddingEntityAsync(TEntity entity) => OnAddingEntityAsync(entity);
        public ValueTask<TEntity> ExposedOnUpdatingEntityAsync(TEntity entity) => OnUpdatingEntityAsync(entity);
        public bool ExposedAreEqual(TEntity existing, TEntity other) => AreEqual(existing, other);
    }

    public class ThrowingFilterableRepo : IFilterableRepository<Person, string>
    {
        public IServiceProvider? Services => null;
        public string? GetEntityKey(Person entity) => entity.Id;
        public ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
        public ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
        public ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
#pragma warning disable S2325 // Interface implementation, cannot be static
        public ValueTask<Person?> FindAsync(string key, CancellationToken cancellationToken = default) => new ValueTask<Person?>((Person?)null);
        public ValueTask<Person?> FindAsync(object key, CancellationToken cancellationToken = default) => new ValueTask<Person?>((Person?)null);
#pragma warning restore S2325
        public ValueTask<PageResult<Person>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default)
            => new ValueTask<PageResult<Person>>(new PageResult<Person>(request, 0, new List<Person>()));
        public ValueTask<Person?> FindFirstAsync(IQuery query, CancellationToken cancellationToken = default) => new ValueTask<Person?>((Person?)null);
        public ValueTask<IReadOnlyList<Person>> FindAllAsync(IQuery query, CancellationToken cancellationToken = default) => new ValueTask<IReadOnlyList<Person>>(new List<Person>());
        public ValueTask<bool> ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default) => ValueTask.FromResult(false);
        public ValueTask<long> CountAsync(IQueryFilter filter, CancellationToken cancellationToken = default) => throw new InvalidOperationException(DbErrorMessage);
    }
}
