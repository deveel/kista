using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista.Entities {
	public class SoftDeletableDbPersonRepository : EntityRepository<SoftDeletableDbPerson, Guid>, ITestRepository<SoftDeletableDbPerson, Guid> {
		public SoftDeletableDbPersonRepository(SoftDeletablePersonDbContext context, IServiceProvider? services = null, ILogger<EntityRepository<SoftDeletableDbPerson, Guid>>? logger = null)
			: base(context, services, logger) {
		}

		protected override IQueryable<SoftDeletableDbPerson> Queryable() {
			var queryable = base.Entities.Include(x => x.Relationships);
			return queryable;
		}

		ValueTask<SoftDeletableDbPerson?> ITestRepository<SoftDeletableDbPerson, Guid>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
			=> FindFirstAsync(query, cancellationToken);

		ValueTask<IReadOnlyList<SoftDeletableDbPerson>> ITestRepository<SoftDeletableDbPerson, Guid>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
			=> FindAllAsync(query, cancellationToken);

		ValueTask<long> ITestRepository<SoftDeletableDbPerson, Guid>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> CountAsync(filter, cancellationToken);

		ValueTask<bool> ITestRepository<SoftDeletableDbPerson, Guid>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> ExistsAsync(filter, cancellationToken);

		IQueryable<SoftDeletableDbPerson> ITestRepository<SoftDeletableDbPerson, Guid>.Queryable() => Queryable();

		protected override async ValueTask<SoftDeletableDbPerson> OnEntityFoundByKeyAsync(Guid key, SoftDeletableDbPerson entity, CancellationToken cancellationToken = default) {
			await Context.Entry(entity).Collection(x => x.Relationships).LoadAsync(cancellationToken);

			return entity;
		}
	}
}