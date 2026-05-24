using Deveel.Data;
using Deveel.Repository.SampleApp.Models;

namespace Deveel.Repository.SampleApp.Repositories;

public class ContactRepository : InMemoryRepository<Contact, Guid>
{
    public ContactRepository(IServiceProvider serviceProvider)
        : base(null, null, serviceProvider)
    {
    }
}
