using Deveel.Data;
using Deveel.Repository.SampleApp.Models;

namespace Deveel.Repository.SampleApp.Lifecycle;

public class ContactLifecycleHandler : IRepositoryLifecycleHandler<Contact>
{
    private readonly ILogger<ContactLifecycleHandler> _logger;
    private readonly IRepository<Contact, Guid> _repository;
    private readonly IRepositorySeedDataProvider<Contact> _seedDataProvider;

    public ContactLifecycleHandler(
        ILogger<ContactLifecycleHandler> logger,
        IRepository<Contact, Guid> repository,
        IRepositorySeedDataProvider<Contact> seedDataProvider)
    {
        _logger = logger;
        _repository = repository;
        _seedDataProvider = seedDataProvider;
    }

    public ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking if Contact repository exists");
        return ValueTask.FromResult(true);
    }

    public ValueTask CreateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating Contact repository");
        return ValueTask.CompletedTask;
    }

    public ValueTask DropAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dropping Contact repository");
        return ValueTask.CompletedTask;
    }

    public async ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding Contact repository");

        var data = seedData as IEnumerable<Contact>
            ?? _seedDataProvider.GetSeedData();

        if (data != null)
        {
            await _repository.AddRangeAsync(data.ToList(), cancellationToken);
            _logger.LogInformation("Seeded {Count} contacts", data.Count());
        }
        else
        {
            _logger.LogInformation("No seed data available for Contact repository");
        }
    }
}
