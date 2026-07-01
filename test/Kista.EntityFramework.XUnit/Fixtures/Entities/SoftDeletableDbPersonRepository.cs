using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista.Entities {
	public class SoftDeletableDbPersonRepository : EntityRepository<SoftDeletableDbPerson, Guid> {
		public SoftDeletableDbPersonRepository(SoftDeletablePersonDbContext context, IServiceProvider? services = null, ILogger<EntityRepository<SoftDeletableDbPerson, Guid>>? logger = null)
			: base(context, services, logger) {
		}

		public override IQueryable<SoftDeletableDbPerson> Queryable() {
			var queryable = base.Entities.Include(x => x.Relationships);
			return queryable;
		}

		protected override async ValueTask<SoftDeletableDbPerson> OnEntityFoundByKeyAsync(Guid key, SoftDeletableDbPerson entity, CancellationToken cancellationToken = default) {
			await Context.Entry(entity).Collection(x => x.Relationships).LoadAsync(cancellationToken);

			return entity;
		}
	}
}