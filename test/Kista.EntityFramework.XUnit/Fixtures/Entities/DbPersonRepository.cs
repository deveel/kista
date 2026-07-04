using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista.Entities {
	public class DbPersonRepository : EntityRepository<DbPerson, Guid>, ITestRepository<DbPerson, Guid> {
		public DbPersonRepository(PersonDbContext context, IServiceProvider? services = null, ILogger<EntityRepository<DbPerson, Guid>>? logger = null) 
			: base(context, services, logger) {
		}

		protected override IQueryable<DbPerson> Queryable() => base.Entities.Include(x => x.Relationships);

		protected override async ValueTask<DbPerson> OnEntityFoundByKeyAsync(Guid key, DbPerson entity, CancellationToken cancellationToken = default) {
			await Context.Entry(entity).Collection(x => x.Relationships).LoadAsync(cancellationToken);

			return entity;
		}

		public Task SetEmailAsync(DbPerson person, string email, CancellationToken cancellationToken = default) {
			person.Email = email;

			return Task.CompletedTask;
		}

		ValueTask<DbPerson?> ITestRepository<DbPerson, Guid>.FindFirstAsync(IQuery query, CancellationToken cancellationToken)
			=> FindFirstAsync(query, cancellationToken);

		ValueTask<IReadOnlyList<DbPerson>> ITestRepository<DbPerson, Guid>.FindAllAsync(IQuery query, CancellationToken cancellationToken)
			=> FindAllAsync(query, cancellationToken);

		ValueTask<long> ITestRepository<DbPerson, Guid>.CountAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> CountAsync(filter, cancellationToken);

		ValueTask<bool> ITestRepository<DbPerson, Guid>.ExistsAsync(IQueryFilter filter, CancellationToken cancellationToken)
			=> ExistsAsync(filter, cancellationToken);

		IQueryable<DbPerson> ITestRepository<DbPerson, Guid>.Queryable() => Queryable();
	}
}
