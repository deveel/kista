using Kista;
using Kista.SampleApp.Owners.Models;

namespace Kista.SampleApp.Owners.SeedData;

public class DefaultSeedData : IRepositorySeedDataProvider<Note>, IRepositorySeedDataProvider<TaskItem>
{
    IEnumerable<Note> IRepositorySeedDataProvider<Note>.GetSeedData()
    {
        var now = DateTime.UtcNow;

        return new List<Note>
        {
            new()
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Title = "Shopping List",
                Content = "Milk, eggs, bread, butter",
                OwnerId = "alice",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Title = "Project Ideas",
                Content = "Rewrite the auth module, add caching layer",
                OwnerId = "bob",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Title = "Meeting Notes",
                Content = "Q3 planning — goals: reduce latency by 20%",
                OwnerId = "alice",
                CreatedAt = now
            }
        };
    }

    IEnumerable<TaskItem> IRepositorySeedDataProvider<TaskItem>.GetSeedData()
    {
        var now = DateTime.UtcNow;

        return new List<TaskItem>
        {
            new()
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Title = "Review PR #42",
                IsCompleted = false,
                Owner = "bob",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                Title = "Write documentation",
                IsCompleted = true,
                Owner = "alice",
                CreatedAt = now
            },
            new()
            {
                Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                Title = "Deploy to staging",
                IsCompleted = false,
                Owner = "bob",
                CreatedAt = now
            }
        };
    }

    IEnumerable<object> IRepositorySeedDataProvider.GetSeedData() =>
        ((IRepositorySeedDataProvider<Note>)this).GetSeedData().Cast<object>()
            .Concat(((IRepositorySeedDataProvider<TaskItem>)this).GetSeedData().Cast<object>());
}
