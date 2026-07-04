// Copyright 2023-2026 Antonello Provenzano
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Concurrent;

namespace Kista;

/// <summary>
/// Concurrency stress tests for <see cref="InMemoryRepository{TEntity,TKey}"/>.
///
/// Every test in this class hammers the repository from 100+ simultaneous tasks to
/// demonstrate that the <see cref="ReaderWriterLockSlim"/> guard introduced in the
/// thread-safety fix prevents race conditions, null-reference exceptions, and silent
/// data loss that were observable with the previous unguarded implementation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "InMemoryRepository")]
[Trait("Concern", "Concurrency")]
public class InMemoryRepositoryConcurrencyTests {
    private const int ThreadCount = 100;

    private static readonly Faker<Person> PersonFaker = new Faker<Person>("en")
        .RuleFor(x => x.FirstName, f => f.Name.FirstName())
        .RuleFor(x => x.LastName, f => f.Name.LastName())
        .RuleFor(x => x.Email, f => f.Internet.Email())
        .RuleFor(x => x.PhoneNumber, f => f.Phone.PhoneNumber())
        .RuleFor(x => x.DateOfBirth, f => f.Date.Past(30));

    // -------------------------------------------------------------------------
    #region AddAsync — concurrent writers

    [Fact]
    public async Task Should_AddAllEntities_When_ManyThreadsWriteConcurrently() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        var people = PersonFaker.Generate(ThreadCount);

        // Act — fire all AddAsync calls simultaneously
        var tasks = people.Select(p => repository.AddAsync(p, cancellationToken).AsTask());
        await Task.WhenAll(tasks);

        // Assert — every entity must be retrievable; no writes may have been lost
        var count = await repository.PublicCountAsync(QueryFilter.Empty);
        Assert.Equal(ThreadCount, count);
    }

    [Fact]
    public async Task Should_NotCorruptState_When_WritersAndReadersRunSimultaneously() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();

        // Seed an initial batch so readers have something to read from the start
        var seedPeople = PersonFaker.Generate(50);
        foreach (var p in seedPeople)
            await repository.AddAsync(p, cancellationToken);

        var newPeople = PersonFaker.Generate(ThreadCount);
        var exceptions = new ConcurrentBag<Exception>();

        // Act — mix 100 concurrent writes with 100 concurrent reads
        var writeTasks = newPeople.Select(async p => {
            try {
                await repository.AddAsync(p, cancellationToken);
            } catch (Exception ex) {
                exceptions.Add(ex);
            }
        });

		var readTasks = Enumerable.Range(0, ThreadCount).Select(async _ => {
			try {
				var list = await repository.PublicFindAllAsync(Kista.Query.Empty);
				Assert.NotNull(list);
			} catch (Exception ex) {
				exceptions.Add(ex);
			}
		});

        await Task.WhenAll(writeTasks.Concat(readTasks));

        // Assert — no exceptions and the store contains at least the seed set
        Assert.Empty(exceptions);
		var total = await repository.PublicCountAsync(QueryFilter.Empty);
        Assert.True(total >= 50, $"Expected at least 50 entities; found {total}");
    }

    #endregion

    // -------------------------------------------------------------------------
    #region FindAsync — concurrent readers

    [Fact]
    public async Task Should_ReturnCorrectEntity_When_ManyThreadsReadConcurrently() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        var people = PersonFaker.Generate(ThreadCount);
        foreach (var p in people)
            await repository.AddAsync(p, cancellationToken);

        // Act — every task fetches the entity that was just added
        var tasks = people.Select(async p => {
            var found = await repository.FindAsync(p.Id!, cancellationToken);
            return (Expected: p, Actual: found);
        });

        var results = await Task.WhenAll(tasks);

        // Assert — every read must have returned the correct entity
        foreach (var (expected, actual) in results) {
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual.Id);
        }
    }

    [Fact]
    public async Task Should_NeverThrow_When_ManyThreadsCallFindAllConcurrently() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        var people = PersonFaker.Generate(50);
        foreach (var p in people)
            await repository.AddAsync(p, cancellationToken);

        var exceptions = new ConcurrentBag<Exception>();

        // Act — 100 concurrent FindAll calls
		var tasks = Enumerable.Range(0, ThreadCount).Select(async _ => {
            try {
                var list = await repository.PublicFindAllAsync(Kista.Query.Empty);
                Assert.NotNull(list);
            } catch (Exception ex) {
                exceptions.Add(ex);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region UpdateAsync — concurrent writers

    [Fact]
    public async Task Should_PreserveAllUpdates_When_ManyThreadsUpdateConcurrently() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        var people = PersonFaker.Generate(ThreadCount);
        foreach (var p in people)
            await repository.AddAsync(p, cancellationToken);

        // Act — each task updates its own distinct entity (no key conflict)
        var tasks = people.Select(async p => {
            p.Email = new Faker().Internet.Email();
            var updated = await repository.UpdateAsync(p, cancellationToken);
            return updated;
        });

        var results = await Task.WhenAll(tasks);

        // Assert — every update must have succeeded
        Assert.All(results, Assert.True);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region RemoveAsync — concurrent writers

    [Fact]
    public async Task Should_RemoveAllTargetedEntities_When_ManyThreadsRemoveConcurrently() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        var people = PersonFaker.Generate(ThreadCount);
        foreach (var p in people)
            await repository.AddAsync(p, cancellationToken);

        // Act — every task removes its own distinct entity
        var tasks = people.Select(p => repository.RemoveAsync(p, cancellationToken).AsTask());
        var results = await Task.WhenAll(tasks);

        // Assert — every removal must have returned true and the store must be empty
        Assert.All(results, Assert.True);
        var count = await repository.PublicCountAsync(QueryFilter.Empty);
        Assert.Equal(0, count);
    }

    #endregion

    // -------------------------------------------------------------------------
    #region Mixed-workload — readers, writers, updaters simultaneously

    [Fact]
    public async Task Should_MaintainConsistency_When_ReadersWritersAndUpdatersCombine() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();

        // Seed 50 entities that will be read and updated throughout the test
        var seed = PersonFaker.Generate(50);
        foreach (var p in seed)
            await repository.AddAsync(p, cancellationToken);

        var exceptions = new ConcurrentBag<Exception>();

        // 100 writers: each adds a brand-new entity
        var writers = PersonFaker.Generate(ThreadCount).Select(async p => {
            try {
                await repository.AddAsync(p, cancellationToken);
            } catch (Exception ex) {
                exceptions.Add(ex);
            }
        });

        // 100 readers: each counts all current entities
		var readers = Enumerable.Range(0, ThreadCount).Select(async _ => {
            try {
                var count = await repository.PublicCountAsync(QueryFilter.Empty);
                Assert.True(count >= 0);
            } catch (Exception ex) {
                exceptions.Add(ex);
            }
        });

        // 50 updaters: each updates one of the seeded entities
        var updaters = seed.Select(async p => {
            try {
                p.PhoneNumber = new Faker().Phone.PhoneNumber();
                await repository.UpdateAsync(p, cancellationToken);
            } catch (Exception ex) {
                exceptions.Add(ex);
            }
        });

        // Act — all workloads run truly in parallel
        await Task.WhenAll(writers.Concat(readers).Concat(updaters));

        // Assert — no race-condition exceptions; store has at least the seeded set
        Assert.Empty(exceptions);
		var finalCount = await repository.PublicCountAsync(QueryFilter.Empty);
        Assert.True(finalCount >= 50, $"Expected at least 50 entities; found {finalCount}");
    }

    #endregion

    // -------------------------------------------------------------------------
    #region GetEntityKey — concurrent key discovery (idMember Lazy cache)

    [Fact]
    public async Task Should_ResolveEntityKey_When_ManyThreadsCallGetEntityKeyConcurrently() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        var people = PersonFaker.Generate(ThreadCount);
        foreach (var p in people)
            await repository.AddAsync(p, cancellationToken);

        // Act — 100 concurrent GetEntityKey calls stress the Lazy<MemberInfo?> cache
        var tasks = people.Select(p => Task.Run(() => ((IRepository<Person, string>)repository).GetEntityKey(p), cancellationToken));
        var keys = await Task.WhenAll(tasks);

        // Assert — every call returns the correct, non-null key
        for (int i = 0; i < people.Count; i++) {
            Assert.NotNull(keys[i]);
            Assert.Equal(people[i].Id, keys[i]);
        }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region Entities snapshot — linearisability check

    [Fact]
    public async Task Should_ReturnAtLeastSeedCount_When_SnapshotTakenDuringConcurrentWrites() {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var repository = new TestInMemoryRepository<Person, string>();
        const int seedCount = 20;
        const int extraCount = ThreadCount;

        var seed = PersonFaker.Generate(seedCount);
        foreach (var p in seed)
            await repository.AddAsync(p, cancellationToken);

        var extra = PersonFaker.Generate(extraCount);

        // Act — take snapshot while writers are running
        var writeTask = Task.WhenAll(extra.Select(p => repository.AddAsync(p, cancellationToken).AsTask()));
        var snapshot = repository.Entities; // taken while writes may still be in-flight

        await writeTask;

        // Assert — snapshot was a consistent point-in-time view (never fewer than seed)
        Assert.True(snapshot.Count >= seedCount,
            $"Snapshot contained {snapshot.Count} entities but should have at least {seedCount}");

        // Final count must be exactly seed + extra
		var finalCount = await repository.PublicCountAsync(QueryFilter.Empty);
        Assert.Equal(seedCount + extraCount, finalCount);
    }

    #endregion
}

/// <summary>
/// A test stub deriving from <see cref="InMemoryRepository{TEntity, TKey}"/>
/// that implements <see cref="ITestRepository{TEntity, TKey}"/> by forwarding
/// the protected filterable pipeline through public passthroughs, so the
/// concurrency tests can drive the filterable methods without
/// <c>InternalsVisibleTo</c>.
/// </summary>
internal sealed class TestInMemoryRepository<TEntity, TKey> : InMemoryRepository<TEntity, TKey>, ITestRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull {

    public TestInMemoryRepository() : base() { }

    public TestInMemoryRepository(IList<TEntity>? entities) : base(entities) { }

    public ValueTask<TEntity?> PublicFindFirstAsync(IQuery query, CancellationToken cancellationToken = default)
        => FindFirstAsync(query, cancellationToken);

    public ValueTask<IReadOnlyList<TEntity>> PublicFindAllAsync(IQuery query, CancellationToken cancellationToken = default)
        => FindAllAsync(query, cancellationToken);

    public ValueTask<long> PublicCountAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
        => CountAsync(filter, cancellationToken);

    public ValueTask<bool> PublicExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
        => ExistsAsync(filter, cancellationToken);

    public new IQueryable<TEntity> Queryable() => base.Queryable();

    ValueTask<TEntity?> ITestRepository<TEntity, TKey>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
        => FindFirstAsync(query, cancellationToken);

    ValueTask<IReadOnlyList<TEntity>> ITestRepository<TEntity, TKey>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
        => FindAllAsync(query, cancellationToken);

    ValueTask<long> ITestRepository<TEntity, TKey>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => CountAsync(filter, cancellationToken);

    ValueTask<bool> ITestRepository<TEntity, TKey>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => ExistsAsync(filter, cancellationToken);

    IQueryable<TEntity> ITestRepository<TEntity, TKey>.Queryable() => Queryable();
}

/// <summary>
/// A no-key test stub deriving from <see cref="InMemoryRepository{TEntity}"/>
/// that implements <see cref="ITestRepository{TEntity}"/> by forwarding the
/// protected filterable pipeline through public passthroughs.
/// </summary>
internal sealed class TestInMemoryRepository<TEntity> : InMemoryRepository<TEntity>, ITestRepository<TEntity>
    where TEntity : class {

    public TestInMemoryRepository() : base() { }

    public TestInMemoryRepository(IList<TEntity>? entities) : base(entities) { }

    public ValueTask<TEntity?> PublicFindFirstAsync(IQuery query, CancellationToken cancellationToken = default)
        => FindFirstAsync(query, cancellationToken);

    public ValueTask<IReadOnlyList<TEntity>> PublicFindAllAsync(IQuery query, CancellationToken cancellationToken = default)
        => FindAllAsync(query, cancellationToken);

    public ValueTask<long> PublicCountAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
        => CountAsync(filter, cancellationToken);

    public ValueTask<bool> PublicExistsAsync(IQueryFilter filter, CancellationToken cancellationToken = default)
        => ExistsAsync(filter, cancellationToken);

    public new IQueryable<TEntity> Queryable() => base.Queryable();

    ValueTask<TEntity?> ITestRepository<TEntity, object>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
        => FindFirstAsync(query, cancellationToken);

    ValueTask<IReadOnlyList<TEntity>> ITestRepository<TEntity, object>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
        => FindAllAsync(query, cancellationToken);

    ValueTask<long> ITestRepository<TEntity, object>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => CountAsync(filter, cancellationToken);

    ValueTask<bool> ITestRepository<TEntity, object>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
        => ExistsAsync(filter, cancellationToken);

    IQueryable<TEntity> ITestRepository<TEntity, object>.Queryable() => Queryable();
}

