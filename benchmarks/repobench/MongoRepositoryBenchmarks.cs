using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using MongoDB.Bson;

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
public class MongoRepositoryBenchmarks : RepositoryCoreBenchmarksBase<MongoBenchPerson, ObjectId> {
	private static readonly Expression<Func<MongoBenchPerson, bool>> CountFilter =
		person => person.Email != null && person.Email.StartsWith("seed-even-");

	private static readonly Expression<Func<MongoBenchPerson, bool>> ExistsFilter =
		person => person.Email != null && person.Email.StartsWith("seed-odd-");

	protected override IRepositoryBenchmarkDriver<MongoBenchPerson, ObjectId> CreateDriver()
		=> new MongoBenchmarkDriver();

	protected override Expression<Func<MongoBenchPerson, bool>> CountPredicate => CountFilter;

	protected override Expression<Func<MongoBenchPerson, bool>> ExistsPredicate => ExistsFilter;

	protected override MongoBenchPerson CreateSeedEntity(int index) {
		var group = index % 2 == 0 ? "even" : "odd";
		return new MongoBenchPerson {
			Id = ObjectId.GenerateNewId(),
			FirstName = $"Name{index}",
			LastName = $"Surname{index}",
			Email = $"seed-{group}-{index}@example.test"
		};
	}

	protected override MongoBenchPerson CreateSingleEntity() {
		return new MongoBenchPerson {
			Id = ObjectId.GenerateNewId(),
			FirstName = "Insert",
			LastName = "One",
			Email = "insert.one@example.test"
		};
	}

	protected override MongoBenchPerson CloneForInsert(MongoBenchPerson entity) {
		return new MongoBenchPerson {
			Id = entity.Id,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Email = entity.Email
		};
	}

	protected override MongoBenchPerson CreateUpdatedEntity(MongoBenchPerson entity) {
		return new MongoBenchPerson {
			Id = entity.Id,
			FirstName = entity.FirstName,
			LastName = entity.LastName,
			Email = "updated@example.test"
		};
	}
}

