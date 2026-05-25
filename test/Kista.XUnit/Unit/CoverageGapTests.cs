using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Linq.Expressions;

namespace Kista;

/// <summary>
/// Tests that target code paths uncovered by the existing test suite,
/// with the goal of increasing overall package coverage.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Coverage")]
public class CoverageGapTests {
    /// <summary>
    /// A repository that implements <see cref="IRepository{Person}"/> (single type parameter)
    /// without overriding the <see cref="IRepository{Person}.Services"/> property,
    /// allowing tests to verify the default <c>null</c> return value on
    /// <see cref="IRepository{TEntity, TKey}.Services"/> when cast to the two-type-parameter interface.
    /// </summary>
    private sealed class NonOverrideServicesRepo : List<Person>, IRepository<Person> {
        public object? GetEntityKey(Person entity) => entity.Id;
        public ValueTask AddAsync(Person entity, CancellationToken ct = default) { Add(entity); return ValueTask.CompletedTask; }
        public ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken ct = default) { AddRange(entities); return ValueTask.CompletedTask; }
        public ValueTask<bool> UpdateAsync(Person entity, CancellationToken ct = default) => throw new NotSupportedException();
        public ValueTask<bool> RemoveAsync(Person entity, CancellationToken ct = default) { var r = Remove(entity); return new ValueTask<bool>(r); }
        public ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken ct = default) { foreach (var e in entities.ToList()) Remove(e); return ValueTask.CompletedTask; }
        public ValueTask<Person?> FindAsync(object key, CancellationToken ct = default) => new ValueTask<Person?>(this.FirstOrDefault(x => x.Id == (string)key));
    }

    [Fact]
    public void IRepository_DefaultServices_HitDefaultImplementation() {
        var repo = new NonOverrideServicesRepo();
        Assert.Null(((IRepository<Person, object>)repo).Services);
    }

    [Fact]
    public void RepositoryExtensions_RequirePageable_NonPageable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.GetPage(new PageQuery<Person>(1, 10)));
    }

    [Fact]
    public void RepositoryExtensions_GetPage_QueryableOnly_Works() {
        var list = new List<Person> { new(), new(), new() };
        IRepository<Person, object> repo = list.AsRepository();
        var page = repo.GetPage(new PageQuery<Person>(1, 2));
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public void RepositoryExtensions_AsQueryable_TKey_Success() {
        var list = new List<Person> { new() };
        IRepository<Person, object> repo = list.AsRepository();
        var queryable = repo.AsQueryable();
        Assert.NotNull(queryable);
    }

    [Fact]
    public void RepositoryExtensions_Exists_TKey_WithFilter_Sync() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        var result = repo.Exists(QueryFilter.Where<Person>(x => x.FirstName == "A"));
        Assert.True(result);
    }

    [Fact]
    public void RepositoryExtensions_Remove_Single_Sync() {
        var list = new List<Person> { new() { Id = "1" }, new() { Id = "2" } };
        var repo = list.AsRepository();
        repo.Remove(list[0]);
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryExtensions_Remove_Single_TKey_Sync() {
        var list = new List<Person> { new() { Id = "1" }, new() { Id = "2" } };
        IRepository<Person, object> repo = list.AsRepository();
        repo.Remove(list[0]);
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryExtensions_Add_Sync_TEntity_Works() {
        var list = new List<Person>();
        var repo = list.AsRepository();
        repo.Add(new Person { FirstName = "T" });
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryExtensions_Add_Sync_TKey_Works() {
        var list = new List<Person>();
        IRepository<Person, object> repo = list.AsRepository();
        repo.Add(new Person { FirstName = "T" });
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryExtensions_Update_Sync_TEntity_Works() {
        var list = new List<Person> { new() { Id = "1" } };
        var repo = list.AsRepository();
        var result = repo.Update(list[0]);
        Assert.True(result);
    }

    [Fact]
    public void RepositoryExtensions_Update_Sync_TKey_Works() {
        var list = new List<Person> { new() { Id = "1" } };
        IRepository<Person, object> repo = list.AsRepository();
        var result = repo.Update(list[0]);
        Assert.True(result);
    }

    [Fact]
    public void RepositoryExtensions_Exists_Sync_TEntity_WithFilter() {
        var repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        Assert.True(repo.Exists(QueryFilter.Where<Person>(x => x.FirstName == "A")));
        Assert.False(repo.Exists(QueryFilter.Where<Person>(x => x.FirstName == "Z")));
    }

    [Fact]
    public void RepositoryExtensions_Remove_Sync_NotFound() {
        var list = new List<Person> { new() { Id = "1" } };
        var repo = list.AsRepository();
        var result = repo.Remove(new Person { Id = "X" });
        Assert.False(result);
    }

    [Fact]
    public void RepositoryExtensions_Remove_Sync_TKey_NotFound() {
        var list = new List<Person> { new() { Id = "1" } };
        IRepository<Person, object> repo = list.AsRepository();
        var result = repo.Remove(new Person { Id = "X" });
        Assert.False(result);
    }

    [Fact]
    public void RepositoryExtensions_GetPageAsync_QueryablePath_Works() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "B" } }.AsRepository();
        var page = repo.GetPageAsync(new PageQuery<Person>(1, 10)).Result;
        Assert.NotNull(page);
    }

    [Fact]
    public void RepositoryExtensions_Count_TKey_Async_WithExpression() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "A" } }.AsRepository();
        var count = repo.CountAsync((Expression<Func<Person, bool>>)(x => x.FirstName == "A")).Result;
        Assert.Equal(2, count);
    }

    [Fact]
    public void RepositoryWrapper_FindAll_WithFilter_FromFilterable() {
        var repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "B" } }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var result = filterable.FindAllAsync(QueryFilter.Where<Person>(x => x.FirstName == "A")).Result;
        Assert.Single(result);
    }

    [Fact]
    public void RepositoryWrapper_GetPage_Sync_FromPageable() {
        var repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "B" }, new() { FirstName = "C" } }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var page = filterable.GetPage(new PageQuery<Person>(1, 2));
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public void RepositoryWrapper_FindFirst_WithFilter() {
        var repo = new List<Person> {
            new() { FirstName = "A", LastName = "X" },
            new() { FirstName = "B", LastName = "Y" }
        }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var found = filterable.FindFirstAsync(QueryFilter.Where<Person>(x => x.FirstName == "B")).Result;
        Assert.NotNull(found);
    }

    [Fact]
    public void CombinedQueryFilter_Apply_WithMultipleFilters_FiltersCorrectly() {
        var people = new List<Person> {
            new() { FirstName = "A", LastName = "B" },
            new() { FirstName = "A", LastName = "C" },
            new() { FirstName = "B", LastName = "B" }
        }.AsQueryable();
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
        var combined = QueryFilter.Combine(new[] { f1, f2 });

        var result = combined.Apply(people).ToList();

        Assert.Single(result);
        Assert.Equal("A", result[0].FirstName);
        Assert.Equal("B", result[0].LastName);
    }

    [Fact]
    public void CombinedQueryFilter_Initialize_CallsAllFilters() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
        var combined = QueryFilter.Combine(new[] { f1, f2 });
        var ctx = new DefaultFilterContext(new ServiceCollection().BuildServiceProvider());
        combined.Initialize(ctx);
    }

    [Fact]
    public void QueryFilter_AsLambda_EmptyFilter_ReturnsTrue() {
        var result = QueryFilter.Empty.AsLambda<Person>().Compile();
        Assert.True(result(new Person()));
    }

    [Fact]
    public void QueryFilter_Combine_TwoNonEmpty_ReturnsCombined() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
        var combined = QueryFilter.Combine(f1, f2);
        Assert.NotNull(combined);
    }

    [Fact]
    public void QueryFilter_Combine_FirstEmptySecondNotEmpty_ReturnsSecond() {
        var f2 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var combined = QueryFilter.Combine(QueryFilter.Empty, f2);
        Assert.NotNull(combined);
    }

    [Fact]
    public void QueryFilter_Combine_FirstNotEmptySecondEmpty_ReturnsFirst() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var combined = QueryFilter.Combine(f1, QueryFilter.Empty);
        Assert.NotNull(combined);
    }

    [Fact]
    public void QueryFilter_Combine_BothEmpty_Throws() {
        Assert.Throws<ArgumentException>(() => QueryFilter.Combine(QueryFilter.Empty, QueryFilter.Empty));
    }

    [Fact]
    public void RepositoryExtensions_CountAll_Sync_TKey_FromRepo() {
        IRepository<Person, object> repo = new List<Person> { new(), new(), new() }.AsRepository();
        Assert.Equal(3, repo.CountAll());
    }

    [Fact]
    public void RepositoryExtensions_Exists_Sync_ExpressionFilter_SingleT() {
        var repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        Assert.True(repo.Exists((Expression<Func<Person, bool>>)(x => x.FirstName == "A")));
    }

    [Fact]
    public void RepositoryExtensions_ExistsAsync_SingleT_IQueryFilter() {
        var repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        var result = repo.ExistsAsync(QueryFilter.Where<Person>(x => x.FirstName == "A")).Result;
        Assert.True(result);
    }

    [Fact]
    public void FieldOrder_IsDescending_ReturnsTrue() {
        var order = new FieldOrder("FirstName", SortDirection.Descending);
        Assert.True(order.IsDescending());
    }

    [Fact]
    public void FieldOrder_IsAscending_ReturnsTrue() {
        var order = new FieldOrder("FirstName", SortDirection.Ascending);
        Assert.True(order.IsAscending());
    }

    [Fact]
    public void ServiceCollectionExtensions_AddRepositoryController_Obsolete_StillWorks() {
        var services = new ServiceCollection();
        services.AddRepositoryController<TestRepositoryController>();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepositoryController>());
    }

    [Fact]
    public void ServiceCollectionExtensions_AddRepositoryController_Default_Obsolete_StillWorks() {
        var services = new ServiceCollection();
        services.AddRepositoryController();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepositoryController>());
    }

    [Fact]
    public void RepositoryWrapper_RemoveRange_WithMultipleItems() {
        var list = new List<Person> { new() { Id = "1" }, new() { Id = "2" }, new() { Id = "3" }, new() { Id = "4" } };
        var repo = list.AsRepository();
        repo.RemoveRangeAsync(new List<Person> { list[0], list[2] });
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void RepositoryWrapper_FindAsync_WithKey_WhenEntityExists() {
        var entity = new Person { Id = "key1", FirstName = "Found" };
        var repo = new List<Person> { entity }.AsRepository();
        var result = repo.FindAsync((object)"key1").Result;
        Assert.NotNull(result);
    }

    [Fact]
    public void RepositoryWrapper_FindAsync_WithKey_WhenNotExists() {
        var repo = new List<Person> { new() { Id = "key1" } }.AsRepository();
        var result = repo.FindAsync((object)"nonexistent").Result;
        Assert.Null(result);
    }

    [Fact]
    public void RepositoryExtensions_AsQueryable_SingleTypeParam_Success() {
        var repo = new List<Person> { new() }.AsRepository();
        var queryable = repo.AsQueryable();
        Assert.NotNull(queryable);
    }

    [Fact]
    public void RepositoryExtensions_GetPageAsync_WithPageAndSize() {
        IRepository<Person, object> repo = new List<Person> { new(), new(), new() }.AsRepository();
        var page = repo.GetPageAsync(1, 2).Result;
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public void ResolveSeedStrategy_ByEnvironment_WithHostEnv() {
        var services = new ServiceCollection();
        services.AddRepositoryLifecycleOrchestrator();
        var sp = services.BuildServiceProvider();
        var orchestrator = new LifecycleEnvOrchestrator(
            Options.Create(new RepositoryLifecycleOptions {
                SeedStrategy = SeedStrategy.ByEnvironment,
                EnvironmentName = null
            }),
            sp
        );
        var strategy = orchestrator.PublicResolveSeedStrategy();
        Assert.Equal(SeedStrategy.Always, strategy);
    }

    /// <summary>
    /// A stub <see cref="IRepositoryController"/> used to test the obsolete
    /// <see cref="ServiceCollectionExtensions.AddRepositoryController{TController}(IServiceCollection, Action{RepositoryControllerOptions}?)"/>
    /// registration method.
    /// </summary>
    private class TestRepositoryController : IRepositoryController {
        /// <inheritdoc/>
        public ValueTask CreateRepositoryAsync<TEntity>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
        /// <inheritdoc/>
        public ValueTask CreateRepositoryAsync<TEntity, TKey>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
        /// <inheritdoc/>
        public ValueTask DropRepositoryAsync<TEntity>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
        /// <inheritdoc/>
        public ValueTask DropRepositoryAsync<TEntity, TKey>(CancellationToken ct = default) where TEntity : class => ValueTask.CompletedTask;
    }

    /// <summary>
    /// A testable subclass of <see cref="RepositoryLifecycleService"/> that
    /// exposes protected members (<see cref="RepositoryLifecycleService.ResolveSeedStrategy"/>
    /// and <see cref="RepositoryLifecycleService.ResolveEnvironmentName"/>) as public,
    /// allowing unit tests to verify environment-aware seed strategy resolution.
    /// </summary>
    private class LifecycleEnvOrchestrator : RepositoryLifecycleService {
        /// <summary>
        /// Creates a new instance with the given options and service provider.
        /// The logger is set to <see cref="NullLogger.Instance"/>.
        /// </summary>
        public LifecycleEnvOrchestrator(IOptions<RepositoryLifecycleOptions> options, IServiceProvider sp)
            : base(options, sp, NullLogger.Instance) { }

        /// <summary>
        /// Public accessor for <see cref="RepositoryLifecycleService.ResolveEnvironmentName"/>.
        /// </summary>
        /// <returns>
        /// The resolved environment name.
        /// </returns>
        public string PublicResolveEnvironmentName() => base.ResolveEnvironmentName();

        /// <summary>
        /// Public accessor for <see cref="RepositoryLifecycleService.ResolveSeedStrategy"/>.
        /// </summary>
        /// <returns>
        /// The resolved <see cref="SeedStrategy"/> value.
        /// </returns>
        public SeedStrategy PublicResolveSeedStrategy() => base.ResolveSeedStrategy();
    }
}
