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
public class EfRepositoryBenchmarks : RepositoryCoreBenchmarksBase<EfBenchPerson, int> {
	private static readonly Expression<Func<EfBenchPerson, bool>> CountFilter =
		person => person.Email != null && person.Email.StartsWith("seed-even-");

	private static readonly Expression<Func<EfBenchPerson, bool>> ExistsFilter =
		person => person.Email != null && person.Email.StartsWith("seed-odd-");

	protected override IRepositoryBenchmarkDriver<EfBenchPerson, int> CreateDriver()
		=> new EfBenchmarkDriver();

	protected override Expression<Func<EfBenchPerson, bool>> CountPredicate => CountFilter;

	protected override Expression<Func<EfBenchPerson, bool>> ExistsPredicate => ExistsFilter;

	protected override EfBenchPerson CreateSeedEntity(int index) {
		var group = index % 2 == 0 ? "even" : "odd";
		return new EfBenchPerson {
			FirstName = $"Name{index}",
			LastName = $"Surname{index}",
			Email = $"seed-{group}-{index}@example.test"
		};
	}

	protected override EfBenchPerson CreateSingleEntity() {
		return new EfBenchPerson {
			FirstName = "Insert",
			LastName = "One",
			Email = "insert.one@example.test"
		};
	}

	protected override EfBenchPerson CloneForInsert(EfBenchPerson entity) {
		return new EfBenchPerson {
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Email = entity.Email
		};
	}

	protected override EfBenchPerson CreateUpdatedEntity(EfBenchPerson entity) {
		return new EfBenchPerson {
			Id = entity.Id,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Email = "updated@example.test"
		};
	}
}

