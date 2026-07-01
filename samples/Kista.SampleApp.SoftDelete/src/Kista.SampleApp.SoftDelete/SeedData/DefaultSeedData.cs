using Kista;
using Kista.SampleApp.SoftDelete.Models;

namespace Kista.SampleApp.SoftDelete.SeedData;

public class DefaultSeedData : IRepositorySeedDataProvider<TaskItem>
{
    IEnumerable<TaskItem> IRepositorySeedDataProvider<TaskItem>.GetSeedData()
    {
        var now = DateTimeOffset.UtcNow;

        return new List<TaskItem>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Title = "Review PR #42",
                IsCompleted = false,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Write documentation",
                IsCompleted = true,
                CreatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Title = "Deploy to staging",
                IsCompleted = false,
                CreatedAtUtc = now,
                IsDeleted = true,
                DeletedAtUtc = now,
                DeletedBy = "seed"
            }
        };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData() =>
        ((IRepositorySeedDataProvider<TaskItem>)this).GetSeedData().Cast<object>();
}