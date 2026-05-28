using Kista;
using Kista.SampleApp.Owners.Models;

namespace Kista.SampleApp.Owners.Repositories;

public class TaskRepository : InMemoryRepository<TaskItem, Guid>
{
    public TaskRepository(IServiceProvider serviceProvider)
        : base(null, null, serviceProvider)
    {
    }
}
