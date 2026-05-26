using Kista;
using Kista.SampleApp.Models;

namespace Kista.SampleApp.Repositories;

public class ContactRepository : InMemoryRepository<Contact, Guid>
{
    public ContactRepository(IServiceProvider serviceProvider)
        : base(null, null, serviceProvider)
    {
    }
}
