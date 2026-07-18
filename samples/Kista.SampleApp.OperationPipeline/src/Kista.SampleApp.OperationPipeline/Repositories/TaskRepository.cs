using Kista.SampleApp.OperationPipeline.Data;
using Kista.SampleApp.OperationPipeline.Models;

namespace Kista.SampleApp.OperationPipeline.Repositories;

public class TaskRepository : EntityRepository<TaskItem, Guid>
{
    public TaskRepository(SampleDbContext context, IServiceProvider? services = null)
        : base(context, services)
    {
    }
}