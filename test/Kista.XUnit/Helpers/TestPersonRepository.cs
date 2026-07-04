namespace Kista;

internal sealed class TestPersonRepository : Repository<Person, string>, ITestRepository<Person, string> {
	private readonly List<Person> _people;

	public TestPersonRepository(int seedCount = 20) {
		var faker = new PersonFaker();
		_people = faker.Generate(Math.Max(1, seedCount));
		_people[0].FirstName = "Alice";
	}

	public IReadOnlyList<Person> Universe => _people;

	protected override IServiceProvider? Services => null;
	protected override string? GetEntityKey(Person entity) => entity.Id;
	protected override IQueryable<Person> Queryable() => _people.AsQueryable();
	protected override bool IsQueryable => true;

	public QueryBuilder<Person> PublicQuery() => CreateQuery();

	ValueTask<Person?> ITestRepository<Person, string>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
		=> FindFirstAsync(query, cancellationToken);

	ValueTask<IReadOnlyList<Person>> ITestRepository<Person, string>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
		=> FindAllAsync(query, cancellationToken);

	ValueTask<long> ITestRepository<Person, string>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
		=> CountAsync(filter, cancellationToken);

	ValueTask<bool> ITestRepository<Person, string>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
		=> ExistsAsync(filter, cancellationToken);

	IQueryable<Person> ITestRepository<Person, string>.Queryable() => Queryable();

	public override ValueTask AddAsync(Person entity, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entity);
		_people.Add(entity);
		return ValueTask.CompletedTask;
	}

	public override ValueTask AddRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entities);
		_people.AddRange(entities);
		return ValueTask.CompletedTask;
	}

	public override ValueTask<bool> UpdateAsync(Person entity, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entity);
		var idx = _people.FindIndex(p => p.Id == entity.Id);
		if (idx < 0) return ValueTask.FromResult(false);
		_people[idx] = entity;
		return ValueTask.FromResult(true);
	}

	public override ValueTask<bool> RemoveAsync(Person entity, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entity);
		return ValueTask.FromResult(_people.Remove(entity));
	}

	public override ValueTask RemoveRangeAsync(IEnumerable<Person> entities, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(entities);
		foreach (var e in entities.ToList())
			_people.Remove(e);
		return ValueTask.CompletedTask;
	}

	public override ValueTask<Person?> FindAsync(string key, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(key);
		return ValueTask.FromResult(_people.FirstOrDefault(p => p.Id == key));
	}
}