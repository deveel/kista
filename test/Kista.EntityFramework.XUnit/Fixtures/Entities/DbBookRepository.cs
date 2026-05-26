using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kista.Entities
{
	public class DbBookRepository : EntityUserRepository<DbBookWithOwner, Guid, string>
	{
		public DbBookRepository(DbContext context, IUserAccessor<string> userAccessor, IServiceProvider? services = null, ILogger<DbBookRepository>? logger = null)
			: base(context, userAccessor, services, logger)
		{
		}
	}
}
