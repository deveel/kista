using Kista.SampleApp.SoftDelete.Data;
using Kista.SampleApp.SoftDelete.Models;

namespace Kista.SampleApp.SoftDelete.Repositories;

public class TaskRepository : EntityRepository<TaskItem, Guid>
{
    public TaskRepository(SampleDbContext context, IServiceProvider? services = null)
        : base(context, services)
    {
    }
}