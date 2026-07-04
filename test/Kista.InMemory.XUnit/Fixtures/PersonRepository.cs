namespace Kista {
	public class PersonRepository : InMemoryRepository<Person>, ITestRepository<Person, string> {
		public PersonRepository()
			: this(Array.Empty<Person>()) {
		}

		internal PersonRepository(IList<Person>? entities)
			: base(entities) {
		}

		ValueTask<Person?> ITestRepository<Person, string>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
			=> FindFirstAsync(query, cancellationToken);

		ValueTask<IReadOnlyList<Person>> ITestRepository<Person, string>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
			=> FindAllAsync(query, cancellationToken);

		ValueTask<long> ITestRepository<Person, string>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> CountAsync(filter, cancellationToken);

		ValueTask<bool> ITestRepository<Person, string>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> ExistsAsync(filter, cancellationToken);

		IQueryable<Person> ITestRepository<Person, string>.Queryable() => Queryable();
	}
}
