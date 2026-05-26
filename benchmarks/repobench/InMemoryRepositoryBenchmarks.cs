using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using System.Linq.Expressions;

using Kista.Benchmarks.Abstractions;
using Kista.Benchmarks.Drivers;
using Kista.Benchmarks.Models;

namespace Kista.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[HideColumns("Baseline", "RatioSD", "RatioSDMean", "RatioSDMedian")]
public class InMemoryRepositoryBenchmarks : RepositoryCoreBenchmarksBase<InMemoryBenchPerson, string> {
	private static readonly Expression<Func<InMemoryBenchPerson, bool>> CountFilter =
		person => person.Email != null && person.Email.StartsWith("seed-even-");

	private static readonly Expression<Func<InMemoryBenchPerson, bool>> ExistsFilter =
		person => person.Email != null && person.Email.StartsWith("seed-odd-");

	protected override IRepositoryBenchmarkDriver<InMemoryBenchPerson, string> CreateDriver()
		=> new InMemoryBenchmarkDriver();

	protected override Expression<Func<InMemoryBenchPerson, bool>> CountPredicate => CountFilter;

	protected override Expression<Func<InMemoryBenchPerson, bool>> ExistsPredicate => ExistsFilter;

	protected override InMemoryBenchPerson CreateSeedEntity(int index) {
		var group = index % 2 == 0 ? "even" : "odd";
		return new InMemoryBenchPerson {
			FirstName = $"Name{index}",
			LastName = $"Surname{index}",
			Email = $"seed-{group}-{index}@example.test"
		};
	}

	protected override InMemoryBenchPerson CreateSingleEntity() {
		return new InMemoryBenchPerson {
			FirstName = "Insert",
			LastName = "One",
			Email = "insert.one@example.test"
		};
	}

	protected override InMemoryBenchPerson CloneForInsert(InMemoryBenchPerson entity) {
		return new InMemoryBenchPerson {
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Email = entity.Email
		};
	}

	protected override InMemoryBenchPerson CreateUpdatedEntity(InMemoryBenchPerson entity) {
		return new InMemoryBenchPerson {
			Id = entity.Id,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Email = "updated@example.test"
		};
	}
}

