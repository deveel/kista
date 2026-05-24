using Deveel.Data;
using Deveel.Repository.SampleApp.Models;

namespace Deveel.Repository.SampleApp.SeedData;

public class DefaultContactSeedData : IRepositorySeedDataProvider<Contact>
{
    public IEnumerable<Contact> GetSeedData()
    {
        var now = DateTime.UtcNow;

        return new List<Contact>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Phone = "+1-555-0101",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                Phone = "+1-555-0102",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                Phone = "+1-555-0103",
                CreatedAt = now
            }
        };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData() => GetSeedData().Cast<object>();
}
