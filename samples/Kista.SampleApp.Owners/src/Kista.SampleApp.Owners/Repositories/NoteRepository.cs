using Kista;
using Kista.SampleApp.Owners.Models;

namespace Kista.SampleApp.Owners.Repositories;

public class NoteRepository : InMemoryRepository<Note, Guid>
{
    public NoteRepository(IServiceProvider serviceProvider)
        : base(null, null, serviceProvider)
    {
    }
}
