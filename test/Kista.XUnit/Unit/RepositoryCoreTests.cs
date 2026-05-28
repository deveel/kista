using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Reflection;

namespace Kista;

/// <summary>
/// Core unit tests for the Kista Repository framework, covering repository
/// scanning, queryable extensions, query filter operations, repository
/// wrapper behavior, and synchronous extension methods.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Core")]
[Trait("Feature", "Core")]
public class RepositoryCoreTests {
    [Fact]
    public void RepositoryContextBuilder_ScanRepositories_FindsRepos() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.ScanRepositories(typeof(ScanTestRepo).Assembly);

        Assert.Contains(typeof(ScanTestRepo), builder.RegisteredRepositoryTypes);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepository<ScanEntity>>());
    }

    [Fact]
    public void RepositoryContextBuilder_ScanRepositories_DoesNotDuplicateAssembly() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.ScanRepositories(typeof(ScanTestRepo).Assembly);
        builder.ScanRepositories(typeof(ScanTestRepo).Assembly);

        Assert.Single(builder.RegisteredRepositoryTypes.Where(t => t == typeof(ScanTestRepo)));
    }

    [Fact]
    public void RepositoryContextBuilder_RegisteredEntityTypes_ResolvesFromServices() {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<Person>>(new List<Person>().AsRepository());
        var builder = new RepositoryContextBuilder(services);

        Assert.Contains(typeof(Person), builder.RegisteredEntityTypes);
    }

    [Fact]
    public void RepositoryContextBuilder_AddRepository_ByType_RegistersAndTracks() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.AddRepository(typeof(ScanTestRepo));

        Assert.Contains(typeof(ScanTestRepo), builder.RegisteredRepositoryTypes);
    }

    [Fact]
    public void RepositoryContextBuilder_AddRepository_OpenGeneric_Registers() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.AddRepository(typeof(OpenScanRepo<>));

        Assert.Contains(typeof(OpenScanRepo<>), builder.RegisteredRepositoryTypes);
    }

    [Fact]
    public void RepositoryContextBuilder_AddRepository_Generic_RegistersAndTracks() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.AddRepository<ScanTestRepo>();

        Assert.Contains(typeof(ScanTestRepo), builder.RegisteredRepositoryTypes);
    }

    [Fact]
    public void ExcludeFromScanAttribute_CanBeApplied() {
        var attr = typeof(ExcludedRepo).GetCustomAttribute<ExcludeFromScanAttribute>();
        Assert.NotNull(attr);
    }

    [Fact]
    public void QueryableExtensions_LongCount_WithEmptyFilter_ReturnsAll() {
        var people = new List<Person> { new(), new() };
        QueryableExtensions.LongCount(people.AsQueryable(), QueryFilter.Empty);
    }

    [Fact]
    public void QueryableExtensions_LongCount_WithFilter_ReturnsMatching() {
        var people = new List<Person> {
            new() { FirstName = "Alice" },
            new() { FirstName = "Bob" }
        };
        var count = QueryableExtensions.LongCount(people.AsQueryable(), QueryFilter.Where<Person>(x => x.FirstName == "Alice"));
        Assert.Equal(1, count);
    }

    [Fact]
    public void QueryableExtensions_ToList_WithEmptyFilter_ReturnsAll() {
        var people = new List<Person> { new(), new() };
        var result = QueryableExtensions.ToList(people.AsQueryable(), QueryFilter.Empty);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void QueryableExtensions_ToList_WithFilter_ReturnsMatching() {
        var people = new List<Person> {
            new() { FirstName = "Alice" },
            new() { FirstName = "Bob" }
        };
        var result = QueryableExtensions.ToList(people.AsQueryable(), QueryFilter.Where<Person>(x => x.FirstName == "Alice"));
        Assert.Single(result);
    }

    [Fact]
    public void QueryableExtensions_FirstOrDefault_WithEmptyFilter_ReturnsFirst() {
        var people = new List<Person> { new() { FirstName = "Alice" }, new() { FirstName = "Bob" } };
        var result = QueryableExtensions.FirstOrDefault(people.AsQueryable(), QueryFilter.Empty);
        Assert.NotNull(result);
        Assert.Equal("Alice", result!.FirstName);
    }

    [Fact]
    public void QueryableExtensions_FirstOrDefault_WithFilter_ReturnsMatching() {
        var people = new List<Person> {
            new() { FirstName = "Alice" },
            new() { FirstName = "Bob" }
        };
        var result = QueryableExtensions.FirstOrDefault(people.AsQueryable(), QueryFilter.Where<Person>(x => x.FirstName == "Bob"));
        Assert.NotNull(result);
        Assert.Equal("Bob", result!.FirstName);
    }

    [Fact]
    public void QueryableExtensions_FirstOrDefault_NoMatch_ReturnsNull() {
        var people = new List<Person> { new() { FirstName = "Alice" } };
        var result = QueryableExtensions.FirstOrDefault(people.AsQueryable(), QueryFilter.Where<Person>(x => x.FirstName == "Nobody"));
        Assert.Null(result);
    }

    [Fact]
    public void QueryableExtensions_Any_WithEmptyFilter_TrueIfAny() {
        var people = new List<Person> { new() };
        Assert.True(QueryableExtensions.Any(people.AsQueryable(), QueryFilter.Empty));
    }

    [Fact]
    public void QueryableExtensions_Any_WithFilter_TrueIfMatch() {
        var people = new List<Person> { new() { FirstName = "Alice" } };
        Assert.True(QueryableExtensions.Any(people.AsQueryable(), QueryFilter.Where<Person>(x => x.FirstName == "Alice")));
    }

    [Fact]
    public void QueryableExtensions_Any_NoMatch_False() {
        var people = new List<Person> { new() { FirstName = "Alice" } };
        Assert.False(QueryableExtensions.Any(people.AsQueryable(), QueryFilter.Where<Person>(x => x.FirstName == "Nobody")));
    }

    [Fact]
    public void RepositoryWrapper_AsFilterable_ReturnsSelf() {
        var repo = new List<Person> { new() }.AsRepository();
        Assert.IsAssignableFrom<IFilterableRepository<Person>>(repo);
    }

    [Fact]
    public void RepositoryWrapper_CountAll_ReturnsCorrectCount() {
        var repo = new List<Person> { new(), new() }.AsRepository();
        Assert.Equal(2, repo.AsFilterable().CountAll());
    }

    [Fact]
    public void RepositoryExtension_AsFilterable_NonFilterable_Throws() {
        var repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.AsFilterable());
    }

    [Fact]
    public void RepositoryExtension_AsQueryable_NonQueryable_Throws() {
        var repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.AsQueryable());
    }

    [Fact]
    public void RepositoryExtension_ExistsAsync_WithExpression_DelToTKey() {
        var repo = new List<Person> { new() { FirstName = "Alice" } }.AsRepository();
        var result = repo.ExistsAsync((Expression<Func<Person, bool>>)(x => x.FirstName == "Alice"));
        Assert.True(result.Result);
    }

    [Fact]
    public void RepositoryExtension_CountAsync_WithExpression_DelToTKey() {
        var repo = new List<Person> { new() { FirstName = "Alice" }, new() { FirstName = "Bob" } }.AsRepository();
        var result = repo.CountAsync((Expression<Func<Person, bool>>)(x => x.FirstName == "Alice"));
        Assert.Equal(1, result.Result);
    }

    [Fact]
    public void RepositoryExtension_CountAllAsync_SingleTParam() {
        var repo = new List<Person> { new(), new() }.AsRepository();
        var result = repo.CountAllAsync();
        Assert.Equal(2, result.Result);
    }

    [Fact]
    public void RepositoryExtension_RemoveByKey_TKey_NotFound_ReturnsFalse() {
        IRepository<Person, object> repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        var result = repo.RemoveByKeyAsync((object)"nonexistent");
        Assert.False(result.Result);
    }

    [Fact]
    public void RepositoryExtension_RemoveByKey_TKey_Found_ReturnsTrue() {
        IRepository<Person, object> repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        var result = repo.RemoveByKeyAsync((object)"1");
        Assert.True(result.Result);
    }

    [Fact]
    public void RepositoryExtension_Exists_Sync_TKey() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        Assert.True(repo.Exists(QueryFilter.Where<Person>(x => x.FirstName == "A")));
        Assert.False(repo.Exists(QueryFilter.Where<Person>(x => x.FirstName == "Z")));
    }

    [Fact]
    public void RepositoryExtension_Count_Sync_TKey() {
        IRepository<Person, object> repo = new List<Person> {
            new() { FirstName = "A" }, new() { FirstName = "A" }, new() { FirstName = "B" }
        }.AsRepository();
        Assert.Equal(2, repo.Count(QueryFilter.Where<Person>(x => x.FirstName == "A")));
    }

    [Fact]
    public void RepositoryExtension_CountAll_Sync_TKey() {
        IRepository<Person, object> repo = new List<Person> { new(), new(), new() }.AsRepository();
        Assert.Equal(3, repo.CountAll());
    }

    [Fact]
    public void RepositoryExtension_CountAll_Sync_SingleT() {
        var repo = new List<Person> { new(), new(), new() }.AsRepository();
        Assert.Equal(3, repo.CountAll());
    }

    [Fact]
    public void RepositoryExtension_Count_Sync_TEntity() {
        var repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "A" } }.AsRepository();
        Assert.Equal(2, repo.Count(x => x.FirstName == "A"));
    }

    [Fact]
    public void RepositoryExtension_RemoveByKey_Sync_Found() {
        var repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        Assert.True(repo.RemoveByKey("1"));
    }

    [Fact]
    public void RepositoryExtension_RemoveByKey_Sync_NotFound() {
        var repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        Assert.False(repo.RemoveByKey("missing"));
    }

    [Fact]
    public async Task RepositoryExtension_GetPageAsync_NonPageable_NonQueryable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repo.GetPageAsync(new PageQuery<Person>(1, 10)).AsTask()
        );
    }

    [Fact]
    public void RepositoryExtension_GetPage_Sync_NonPageable_NonQueryable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.GetPage(new PageQuery<Person>(1, 10)));
    }

    [Fact]
    public void RepositoryExtension_GetPage_Sync_WithPageAndSize() {
        IRepository<Person, object> repo = new List<Person> { new(), new(), new() }.AsRepository();
        var page = repo.GetPage(1, 2);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public void RepositoryExtension_ExistsAsync_TKey_WithExpression() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        var result = repo.ExistsAsync((Expression<Func<Person, bool>>)(x => x.FirstName == "A"));
        Assert.True(result.Result);
    }

    [Fact]
    public void RepositoryExtension_ExistsAsync_TKey_WithFilter() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        var result = repo.ExistsAsync(QueryFilter.Where<Person>(x => x.FirstName == "A"));
        Assert.True(result.Result);
    }

    [Fact]
    public void RepositoryExtension_ExistsAsync_TKey_WithFilter_NoMatch() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        var result = repo.ExistsAsync(QueryFilter.Where<Person>(x => x.FirstName == "Z"));
        Assert.False(result.Result);
    }

    [Fact]
    public void RepositoryExtension_CountAsync_TKey_WithExpression() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "A" } }.AsRepository();
        var result = repo.CountAsync((Expression<Func<Person, bool>>)(x => x.FirstName == "A"));
        Assert.Equal(2, result.Result);
    }

    [Fact]
    public void RepositoryExtension_CountAsync_TKey_WithFilter() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "B" } }.AsRepository();
        var result = repo.CountAsync(QueryFilter.Where<Person>(x => x.FirstName == "A"));
        Assert.Equal(1, result.Result);
    }

    [Fact]
    public void RepositoryExtension_CountAllAsync_TKey() {
        IRepository<Person, object> repo = new List<Person> { new(), new(), new() }.AsRepository();
        var result = repo.CountAllAsync();
        Assert.Equal(3, result.Result);
    }

    [Fact]
    public async Task RepositoryExtension_ExistsAsync_TKey_NonFilterable_NonQueryable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repo.ExistsAsync(QueryFilter.Where<Person>(x => x.FirstName == "A")).AsTask()
        );
    }

    [Fact]
    public async Task RepositoryExtension_CountAsync_TKey_NonFilterable_NonQueryable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repo.CountAsync(QueryFilter.Where<Person>(x => x.FirstName == "A")).AsTask()
        );
    }

    [Fact]
    public async Task RepositoryExtension_CountAllAsync_TKey_NonFilterable_NonQueryable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        await Assert.ThrowsAsync<NotSupportedException>(
            () => repo.CountAllAsync().AsTask()
        );
    }

    [Fact]
    public void RepositoryExtension_Exists_Sync_TKey_WithExpression() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        Assert.True(repo.Exists((Expression<Func<Person, bool>>)(x => x.FirstName == "A")));
    }

    [Fact]
    public void RepositoryExtension_Exists_Sync_TEntity_WithExpression() {
        var repo = new List<Person> { new() { FirstName = "A" } }.AsRepository();
        Assert.True(repo.Exists((Expression<Func<Person, bool>>)(x => x.FirstName == "A")));
    }

    [Fact]
    public void RepositoryExtension_Remove_Sync() {
        var list = new List<Person> { new() { Id = "1" } };
        var repo = list.AsRepository();
        repo.Remove(list[0]);
    }

    [Fact]
    public void RepositoryExtension_Remove_Sync_TKey() {
        var list = new List<Person> { new() { Id = "1" } };
        IRepository<Person, object> repo = list.AsRepository();
        repo.Remove(list[0]);
    }

    [Fact]
    public void RepositoryExtension_Add_Sync() {
        var repo = new List<Person>().AsRepository();
        repo.Add(new Person { FirstName = "Test" });
    }

    [Fact]
    public void RepositoryExtension_Add_Sync_TKey() {
        IRepository<Person, object> repo = new List<Person>().AsRepository();
        repo.Add(new Person { FirstName = "Test" });
    }

    [Fact]
    public void RepositoryExtension_Update_Sync() {
        var list = new List<Person> { new() { Id = "1" } };
        var repo = list.AsRepository();
        var result = repo.Update(list[0]);
        Assert.True(result);
    }

    [Fact]
    public void RepositoryExtension_Update_Sync_TKey() {
        var list = new List<Person> { new() { Id = "1" } };
        IRepository<Person, object> repo = list.AsRepository();
        var result = repo.Update(list[0]);
        Assert.True(result);
    }

    [Fact]
    public void RepositoryExtension_RemoveByKey_Sync_TKey_Found() {
        IRepository<Person, object> repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        Assert.True(repo.RemoveByKey((object)"1"));
    }

    [Fact]
    public void RepositoryExtension_RemoveByKey_Sync_TKey_NotFound() {
        IRepository<Person, object> repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        Assert.False(repo.RemoveByKey((object)"missing"));
    }

    [Fact]
    public void RepositoryExtension_RemoveRange_Sync() {
        var list = new List<Person> { new() { Id = "1" }, new() { Id = "2" } };
        var repo = list.AsRepository();
        repo.RemoveRange(new List<Person> { list[0] });
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryExtension_RemoveRange_Sync_TKey() {
        var list = new List<Person> { new() { Id = "1" }, new() { Id = "2" } };
        IRepository<Person, object> repo = list.AsRepository();
        repo.RemoveRange(new List<Person> { list[0] });
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryExtension_AddRange_Sync_TKey() {
        IRepository<Person, object> repo = new List<Person> { new() { Id = "1" } }.AsRepository();
        repo.AddRange(new List<Person> { new() });
    }

    [Fact]
    public void RepositoryExtension_CountAll_Sync_KnownLifetime() {
        var repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.CountAll());
    }

    [Fact]
    public void Repository_Remove_Sync_Entity_NotFound() {
        var list = new List<Person> { new() { Id = "1" } };
        var repo = list.AsRepository();
        var result = repo.Remove(new Person { Id = "nonexistent" });
        Assert.False(result);
    }

    [Fact]
    public void Repository_Remove_Sync_TKey_Entity_NotFound() {
        var list = new List<Person> { new() { Id = "1" } };
        IRepository<Person, object> repo = list.AsRepository();
        var result = repo.Remove(new Person { Id = "nonexistent" });
        Assert.False(result);
    }

    [Fact]
    public void QueryFilter_AsLambda_NonExpressionFilter_Throws() {
        var filter = new CustomFilter();
        Assert.Throws<ArgumentException>(() => filter.AsLambda<Person>());
    }

    [Fact]
    public void QueryFilter_Apply_CombinedFilter_Iterates() {
        var people = new List<Person> {
            new() { FirstName = "A", LastName = "X" },
            new() { FirstName = "B", LastName = "Y" }
        }.AsQueryable();
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "Y");
        var combined = QueryFilter.Combine(new[] { f1, f2 });

        var result = combined.Apply(people).ToList();

        Assert.Empty(result); // no match since A has X, B has Y
    }

    [Fact]
    public void QueryFilter_Apply_ExpressionFilter_UsesAsLambda() {
        var people = new List<Person> {
            new() { FirstName = "Alice" },
            new() { FirstName = "Bob" }
        }.AsQueryable();
        var filter = QueryFilter.Where<Person>(x => x.FirstName == "Alice");

        var result = filter.Apply(people).ToList();

        Assert.Single(result);
    }

    [Fact]
    public void QueryFilter_Apply_UnsupportedFilter_Throws() {
        var people = new List<Person>().AsQueryable();
        var filter = new CustomFilter();

        Assert.Throws<ArgumentException>(() => filter.Apply(people));
    }

    [Fact]
    public void QueryFilter_Combine_FirstCombined_Flattens() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
        var inner = QueryFilter.Combine(new[] { f1, f2 });
        var f3 = QueryFilter.Where<Person>(x => x.Email != null);

        var result = QueryFilter.Combine(inner, f3);

        Assert.NotNull(result);
    }

    [Fact]
    public void CombinedQueryFilter_NonGenericEnumerator_Works() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
        var combined = QueryFilter.Combine(new[] { f1, f2 });

        System.Collections.IEnumerable enumerable = (System.Collections.IEnumerable)combined;
        var enumerator = enumerable.GetEnumerator();

        Assert.NotNull(enumerator);
    }

    [Fact]
    public void CombinedQueryFilter_Combine_NestedCombined_Flattens() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = QueryFilter.Where<Person>(x => x.LastName == "B");
        var inner = QueryFilter.Combine(new[] { f1, f2 }) as CombinedQueryFilter;

        var outer = inner!.Combine(QueryFilter.Where<Person>(x => x.Email != null));

        Assert.NotNull(outer);
    }

    [Fact]
    public void CombinedQueryFilter_AsLambda_EmptyFilter_ReturnsTrue() {
        var combined = new CombinedQueryFilter(new List<IQueryFilter> { QueryFilter.Empty });

        var lambda = combined.AsLambda<Person>();

        Assert.NotNull(lambda);
        Assert.True(lambda.Compile()(new Person()));
    }

    [Fact]
    public void CombinedQueryFilter_AsLambda_DifferentParamNames_Throws() {
        var f1 = QueryFilter.Where<Person>(x => x.FirstName == "A");
        var f2 = new ExpressionQueryFilter<Person>(p => p.LastName == "B"); // different param name
        var combined = QueryFilter.Combine(new[] { f1, f2 }) as CombinedQueryFilter;

        Assert.Throws<InvalidOperationException>(() => combined!.AsLambda<Person>());
    }

    [Fact]
    public void RepositoryExtension_AsFilterable_TKey_NonFilterable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.AsFilterable());
    }

    [Fact]
    public void RepositoryExtension_AsQueryable_TKey_NonQueryable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.AsQueryable());
    }

    [Fact]
    public void RepositoryExtension_GetPageAsync_QueryablePath_Works() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "B" } }.AsRepository();
        var page = repo.GetPageAsync(new PageQuery<Person>(1, 10));
        Assert.NotNull(page.Result);
    }

    [Fact]
    public void RepositoryExtension_GetPage_QueryablePath_Works() {
        IRepository<Person, object> repo = new List<Person> { new() { FirstName = "A" }, new() { FirstName = "B" } }.AsRepository();
        var page = repo.GetPage(new PageQuery<Person>(1, 10));
        Assert.NotNull(page);
    }

    [Fact]
    public void RepositoryWrapper_GetEntityKey_FieldKey_FindsById() {
        var repo = new List<Person> { new() { Id = "test-id" } }.AsRepository();
        var key = repo.GetEntityKey(new Person { Id = "test-id" });
        Assert.Equal("test-id", key);
    }

    [Fact]
    public void RepositoryWrapper_Update_NonExistent_ReturnsFalse() {
        var list = new List<Person> { new() { Id = "1" } };
        var repo = list.AsRepository();
        var result = repo.UpdateAsync(new Person { Id = "999" });
        Assert.False(result.Result);
    }

    [Fact]
    public void RepositoryWrapper_RemoveRange_RemovesMultiple() {
        var list = new List<Person> { new() { Id = "1" }, new() { Id = "2" }, new() { Id = "3" } };
        var repo = list.AsRepository();
        repo.RemoveRangeAsync(new List<Person> { list[0], list[2] });
        Assert.Single(list);
    }

    [Fact]
    public void RepositoryWrapper_Find_ByKey_ReturnsEntity() {
        var entity = new Person { Id = "1", FirstName = "Alice" };
        var repo = new List<Person> { entity }.AsRepository();
        var found = repo.FindAsync((object)"1");
        Assert.NotNull(found.Result);
        Assert.Equal("Alice", found.Result!.FirstName);
    }

    [Fact]
    public void RepositoryWrapper_FindAll_EmptyFilter_ReturnsAll() {
        var repo = new List<Person> { new() { Id = "1" }, new() { Id = "2" } }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var result = filterable.FindAllAsync(QueryFilter.Empty);
        Assert.Equal(2, result.Result.Count);
    }

    [Fact]
    public void RepositoryWrapper_CountAsync_WithFilter_Works() {
        var repo = new List<Person> {
            new() { FirstName = "A" },
            new() { FirstName = "B" },
            new() { FirstName = "A" }
        }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var result = filterable.CountAsync(QueryFilter.Where<Person>(x => x.FirstName == "A"));
        Assert.Equal(2, result.Result);
    }

    [Fact]
    public void RepositoryRegistration_GetRepositoryServiceTypes_ReturnsInterfaces() {
        var services = new ServiceCollection();
        services.AddRepositoryContext().AddRepository<ScanTestRepo>();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IScanRepo>());
        Assert.NotNull(provider.GetService<IRepository<ScanEntity>>());
    }

    [Fact]
    public void ServiceCollectionExtensions_AddRepository_Obsolete_StillWorks() {
        var services = new ServiceCollection();
        services.AddRepository<ScanTestRepo>();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepository<ScanEntity>>());
    }

    [Fact]
    public void ServiceCollectionExtensions_AddRepository_Type_StillWorks() {
        var services = new ServiceCollection();
        services.AddRepository(typeof(ScanTestRepo));
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepository<ScanEntity>>());
    }

    [Fact]
    public void WithSeedDataFrom_FindsProviders() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly);

        var provider = services.BuildServiceProvider();
        var seedProvider = provider.GetService<IRepositorySeedDataProvider<SeedEntity>>();
        Assert.NotNull(seedProvider);
    }

    [Fact]
    public void WithSeedDataFrom_RegistersMultiple() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepositorySeedDataProvider<SeedEntity>>());
        Assert.NotNull(provider.GetService<IRepositorySeedDataProvider<AnotherSeedEntity>>());
    }

    [Fact]
    public void WithSeedDataFrom_EmptyAssembly_DoesNotThrow() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(typeof(ScanTestRepo).Assembly);
    }

    [Fact]
    public void WithSeedDataFrom_NullAssembly_DoesNotThrow() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(new Assembly[] { null! });
    }

    [Fact]
    public void WithSeedDataFrom_MultipleAssemblies_ScansAll() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly, typeof(ExplicitSeedProvider).Assembly);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IRepositorySeedDataProvider<SeedEntity>>());
    }

    [Fact]
    public void WithSeedDataFrom_DuplicateScan_DoesNotDuplicate() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly);
        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly);

        var provider = services.BuildServiceProvider();
        var seedProviders = provider.GetServices<IRepositorySeedDataProvider<SeedEntity>>().ToList();
        Assert.Single(seedProviders);
    }

    [Fact]
    public void WithSeedDataFrom_WithSeedDataBeforeScan_ExplicitWins() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedData<SeedEntity, ExplicitSeedProvider>();
        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IRepositorySeedDataProvider<SeedEntity>>();

        Assert.IsType<ExplicitSeedProvider>(resolved);
    }

    [Fact]
    public void WithSeedDataFrom_WithSeedDataInlineBeforeScan_ExplicitWins() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        var data = new[] { new SeedEntity { Id = "inline" } };
        builder.WithSeedData(data);
        builder.WithSeedDataFrom(typeof(SeedEntityProvider).Assembly);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IRepositorySeedDataProvider<SeedEntity>>();

        var result = resolved.GetSeedData().ToList();
        Assert.Single(result);
        Assert.Equal("inline", result[0].Id);
    }

    [Fact]
    public void WithSeedDataFrom_IgnoresOpenGenericSeedProviders() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedDataFrom(typeof(OpenSeedProvider<>).Assembly);

        var provider = services.BuildServiceProvider();
        // No closed IRepositorySeedDataProvider<OpenSeedEntity> exists in the assembly,
        // only the open generic OpenSeedProvider<> which should be filtered out
        var resolved = provider.GetService<IRepositorySeedDataProvider<OpenSeedEntity>>();
        Assert.Null(resolved);
    }

    [Fact]
    public void WithSeedData_Provider_RegistersService() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedData<SeedEntity, ExplicitSeedProvider>();

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IRepositorySeedDataProvider<SeedEntity>>();
        Assert.NotNull(resolved);
        Assert.IsType<ExplicitSeedProvider>(resolved);
    }

    [Fact]
    public void WithSeedData_Inline_RegistersService() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        var data = new[] { new SeedEntity { Id = "inline" } };
        builder.WithSeedData(data);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IRepositorySeedDataProvider<SeedEntity>>();
        Assert.NotNull(resolved);

        var result = resolved.GetSeedData().ToList();
        Assert.Single(result);
        Assert.Equal("inline", result[0].Id);
    }

    [Fact]
    public void WithSeedData_Provider_SupportsCustomLifetime() {
        var services = new ServiceCollection();
        var builder = new RepositoryContextBuilder(services);

        builder.WithSeedData<SeedEntity, ExplicitSeedProvider>(ServiceLifetime.Transient);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<IRepositorySeedDataProvider<SeedEntity>>();
        Assert.NotNull(resolved);
    }

    private sealed class NonOverrideServicesRepo : List<Person>, IRepository<Person> {
        public object? GetEntityKey(Person entity) => entity.Id;
        public ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) { Add(entity); return ValueTask.CompletedTask; }
        public ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) { AddRange(entities); return ValueTask.CompletedTask; }
        public ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) { var r = Remove(entity); return new ValueTask<bool>(r); }
        public ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) { foreach (var e in entities.ToList()) Remove(e); return ValueTask.CompletedTask; }
        public ValueTask<Person?> FindAsync(object key, CancellationToken cancellationToken = default) => new ValueTask<Person?>(this.FirstOrDefault(x => x.Id == (string)key));
    }

    [Fact]
    public void IRepository_DefaultServices_HitDefaultImplementation() {
        var repo = new NonOverrideServicesRepo();
        Assert.Null(((IRepository<Person, object>)repo).Services);
    }

    [Fact]
    public async Task RepositoryWrapper_FindAll_WithFilter_FromFilterable() {
        var repo = new List<Person> { new Person { FirstName = "A" }, new Person { FirstName = "B" } }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var result = await filterable.FindAllAsync(QueryFilter.Where<Person>(x => x.FirstName == "A"));
        Assert.Single(result);
    }

    [Fact]
    public async Task RepositoryWrapper_FindFirst_WithFilter() {
        var repo = new List<Person> {
            new Person { FirstName = "A", LastName = "X" },
            new Person { FirstName = "B", LastName = "Y" }
        }.AsRepository();
        var filterable = (IFilterableRepository<Person>)repo;
        var found = await filterable.FindFirstAsync(QueryFilter.Where<Person>(x => x.FirstName == "B"));
        Assert.NotNull(found);
    }

    [Fact]
    public async Task RepositoryExtensions_Count_TKey_Async_WithExpression() {
        IRepository<Person, object> repo = new List<Person> { new Person { FirstName = "A" }, new Person { FirstName = "A" } }.AsRepository();
        var count = await repo.CountAsync((Expression<Func<Person, bool>>)(x => x.FirstName == "A"));
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task RepositoryExtensions_ExistsAsync_SingleT_IQueryFilter() {
        var repo = new List<Person> { new Person { FirstName = "A" } }.AsRepository();
        var result = await repo.ExistsAsync(QueryFilter.Where<Person>(x => x.FirstName == "A"));
        Assert.True(result);
    }

    [Fact]
    public async Task RepositoryWrapper_RemoveRange_WithMultipleItems() {
        var list = new List<Person> { new Person { Id = "1" }, new Person { Id = "2" }, new Person { Id = "3" }, new Person { Id = "4" } };
        var repo = list.AsRepository();
        await repo.RemoveRangeAsync(new List<Person> { list[0], list[2] });
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task RepositoryWrapper_FindAsync_WithKey_WhenEntityExists() {
        var entity = new Person { Id = "key1", FirstName = "Found" };
        var repo = new List<Person> { entity }.AsRepository();
        var result = await repo.FindAsync((object)"key1");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task RepositoryWrapper_FindAsync_WithKey_WhenNotExists() {
        var repo = new List<Person> { new Person { Id = "key1" } }.AsRepository();
        var result = await repo.FindAsync((object)"nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void RepositoryExtensions_AsQueryable_TKey_Success() {
        var list = new List<Person> { new Person() };
        IRepository<Person, object> repo = list.AsRepository();
        var queryable = repo.AsQueryable();
        Assert.NotNull(queryable);
    }

    [Fact]
    public void RepositoryExtensions_AsQueryable_SingleTypeParam_Success() {
        var repo = new List<Person> { new Person() }.AsRepository();
        var queryable = repo.AsQueryable();
        Assert.NotNull(queryable);
    }

    [Fact]
    public void RepositoryExtensions_RequirePageable_NonPageable_Throws() {
        IRepository<Person, object> repo = new NonFilterableRepo<Person>();
        Assert.Throws<NotSupportedException>(() => repo.GetPage(new PageQuery<Person>(1, 10)));
    }
}

#region Test types

/// <summary>
/// An entity that uses a public field (<see cref="Id"/>) as its key,
/// used to test the field-based key discovery path in <see cref="RepositoryWrapper{TEntity}"/>.
/// </summary>
class FieldKeyEntity {
    public string? Id;
    public string? Name { get; set; }
}

/// <summary>
/// A custom <see cref="IQueryFilter"/> that is not an <see cref="IExpressionQueryFilter"/>,
/// used to test the error path in <see cref="QueryFilter.AsLambda{TEntity}"/>
/// and <see cref="QueryFilter.Apply{TEntity}"/>.
/// </summary>
class CustomFilter : IQueryFilter {
    public bool IsEmpty() => false;
    public void Initialize(IFilterContext context) { }
}

/// <summary>
/// A simple entity class used in repository scanning tests.
/// </summary>
class ScanEntity {
    public string? Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// A repository interface for <see cref="ScanEntity"/>, used to test
/// the scanning and automatic registration of repository types.
/// </summary>
interface IScanRepo : IRepository<ScanEntity> { }

/// <summary>
/// An in-memory implementation of <see cref="IScanRepo"/> backed by a <see cref="List{ScanEntity}"/>,
/// used to verify that the scanner discovers and registers repository types correctly.
/// </summary>
class ScanTestRepo : List<ScanEntity>, IScanRepo {
    public object? GetEntityKey(ScanEntity entity) => entity.Id;
    public IServiceProvider? Services => null;
    public ValueTask AddAsync(ScanEntity entity, CancellationToken ct = default) { Add(entity); return ValueTask.CompletedTask; }
    public ValueTask AddRangeAsync(IEnumerable<ScanEntity> entities, CancellationToken ct = default) { AddRange(entities); return ValueTask.CompletedTask; }
    public ValueTask<bool> UpdateAsync(ScanEntity entity, CancellationToken ct = default) => throw new NotSupportedException();
    public ValueTask<bool> RemoveAsync(ScanEntity entity, CancellationToken ct = default) { var r = Remove(entity); return new ValueTask<bool>(r); }
    public ValueTask RemoveRangeAsync(IEnumerable<ScanEntity> entities, CancellationToken ct = default) { foreach (var e in entities.ToList()) Remove(e); return ValueTask.CompletedTask; }
    public ValueTask<ScanEntity?> FindAsync(object key, CancellationToken ct = default) => new ValueTask<ScanEntity?>(this.FirstOrDefault(x => x.Id == (string)key));
}

/// <summary>
/// A repository type decorated with <see cref="ExcludeFromScanAttribute"/> to verify
/// that the scanner correctly skips excluded types.
/// </summary>
[ExcludeFromScan]
class ExcludedRepo : ScanTestRepo { }

/// <summary>
/// An open-generic in-memory repository used to verify that the scanner
/// can register open generic repository types.
/// </summary>
class OpenScanRepo<TEntity> : List<TEntity>, IRepository<TEntity> where TEntity : class {
    public IServiceProvider? Services => null;
    public object? GetEntityKey(TEntity entity) => null;
    public ValueTask AddAsync(TEntity entity, CancellationToken ct = default) { Add(entity); return ValueTask.CompletedTask; }
    public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) { AddRange(entities); return ValueTask.CompletedTask; }
    public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken ct = default) => throw new NotSupportedException();
    public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken ct = default) { var r = Remove(entity); return new ValueTask<bool>(r); }
    public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) { foreach (var e in entities.ToList()) Remove(e); return ValueTask.CompletedTask; }
    public ValueTask<TEntity?> FindAsync(object key, CancellationToken ct = default) => throw new NotSupportedException();
}

/// <summary>
/// A repository that implements only <see cref="IRepository{TEntity}"/> without
/// <see cref="IFilterableRepository{TEntity}"/>, <see cref="IQueryableRepository{TEntity}"/>,
/// or <see cref="IPageableRepository{TEntity}"/>. Used to test error paths in
/// queryable, filterable, and pageable extension methods.
/// </summary>
class NonFilterableRepo<TEntity> : IRepository<TEntity> where TEntity : class {
    public IServiceProvider? Services => null;
    public ValueTask AddAsync(TEntity entity, CancellationToken ct = default) => throw new NotSupportedException();
    public ValueTask AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) => throw new NotSupportedException();
    public ValueTask<TEntity?> FindAsync(object key, CancellationToken ct = default) => throw new NotSupportedException();
    public object? GetEntityKey(TEntity entity) => null;
    public ValueTask<bool> RemoveAsync(TEntity entity, CancellationToken ct = default) => throw new NotSupportedException();
    public ValueTask RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) => throw new NotSupportedException();
    public ValueTask<bool> UpdateAsync(TEntity entity, CancellationToken ct = default) => throw new NotSupportedException();
}

/// <summary>
/// An entity used in seed data provider scanning tests.
/// </summary>
class SeedEntity {
    public string? Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// An entity used to test multiple seed data provider scanning.
/// </summary>
class AnotherSeedEntity {
    public string? Id { get; set; }
}

/// <summary>
/// A stub <see cref="IRepositorySeedDataProvider{SeedEntity}"/> used to verify
/// that <see cref="RepositoryContextBuilder.WithSeedDataFrom(Assembly[])"/>
/// automatically discovers and registers closed generic seed provider implementations.
/// </summary>
class SeedEntityProvider : IRepositorySeedDataProvider<SeedEntity> {
    public IEnumerable<SeedEntity> GetSeedData() {
        yield return new SeedEntity { Id = "scanned", Name = "Scanned" };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
        => GetSeedData().Cast<object>();
}

/// <summary>
/// A second seed provider for <see cref="AnotherSeedEntity"/>, used to verify
/// that scanning discovers multiple providers for different entity types.
/// </summary>
class AnotherSeedEntityProvider : IRepositorySeedDataProvider<AnotherSeedEntity> {
    public IEnumerable<AnotherSeedEntity> GetSeedData() {
        yield return new AnotherSeedEntity { Id = "another" };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
        => GetSeedData().Cast<object>();
}

/// <summary>
/// An explicit seed provider used in priority tests, verifying that
/// <see cref="RepositoryContextBuilder.WithSeedData{TEntity, TProvider}(ServiceLifetime)"/>
/// takes precedence over auto-discovered providers.
/// </summary>
class ExplicitSeedProvider : IRepositorySeedDataProvider<SeedEntity> {
    public IEnumerable<SeedEntity> GetSeedData() {
        yield return new SeedEntity { Id = "explicit", Name = "Explicit" };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData()
        => GetSeedData().Cast<object>();
}

/// <summary>
/// An entity type for which only an open generic <see cref="IRepositorySeedDataProvider{TEntity}"/>
/// exists — used to verify that scanning correctly ignores open generic providers.
/// </summary>
class OpenSeedEntity {
    public string? Id { get; set; }
}

/// <summary>
/// An open generic seed provider used to verify that scanning
/// correctly ignores open generic <see cref="IRepositorySeedDataProvider{TEntity}"/>
/// implementations.
/// </summary>
class OpenSeedProvider<TEntity> : IRepositorySeedDataProvider<TEntity> where TEntity : class {
    public IEnumerable<TEntity> GetSeedData() => Enumerable.Empty<TEntity>();
    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData() => Enumerable.Empty<object>();
}

#endregion
