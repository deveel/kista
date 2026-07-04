namespace Kista {
	public class SoftDeletablePersonRepository : InMemoryRepository<SoftDeletablePerson, string>, ITestRepository<SoftDeletablePerson, string> {
		public SoftDeletablePersonRepository()
			: this(Array.Empty<SoftDeletablePerson>()) {
		}

		internal SoftDeletablePersonRepository(IList<SoftDeletablePerson>? entities)
			: base(entities) {
		}

		ValueTask<SoftDeletablePerson?> ITestRepository<SoftDeletablePerson, string>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
			=> FindFirstAsync(query, cancellationToken);

		ValueTask<IReadOnlyList<SoftDeletablePerson>> ITestRepository<SoftDeletablePerson, string>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
			=> FindAllAsync(query, cancellationToken);

		ValueTask<long> ITestRepository<SoftDeletablePerson, string>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> CountAsync(filter, cancellationToken);

		ValueTask<bool> ITestRepository<SoftDeletablePerson, string>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> ExistsAsync(filter, cancellationToken);

		IQueryable<SoftDeletablePerson> ITestRepository<SoftDeletablePerson, string>.Queryable() => Queryable();
	}
}