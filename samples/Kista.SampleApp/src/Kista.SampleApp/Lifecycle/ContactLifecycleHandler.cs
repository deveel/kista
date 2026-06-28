using Kista;
using Kista.SampleApp.Models;

namespace Kista.SampleApp.Lifecycle;

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
        _logger.LogCheckingRepositoryExists(nameof(Contact));
        return ValueTask.FromResult(true);
    }

    public ValueTask CreateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogCreatingRepository(nameof(Contact));
        return ValueTask.CompletedTask;
    }

    public ValueTask DropAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDroppingRepository(nameof(Contact));
        return ValueTask.CompletedTask;
    }

    public async ValueTask SeedAsync(object? seedData = null, CancellationToken cancellationToken = default)
    {
        _logger.LogSeedingRepository(nameof(Contact));

        var data = seedData as IEnumerable<Contact>
            ?? _seedDataProvider.GetSeedData();

        if (data != null)
        {
            await _repository.AddRangeAsync(data.ToList(), cancellationToken);
            _logger.LogSeededCount(data.Count());
        }
        else
        {
            _logger.LogNoSeedData(nameof(Contact));
        }
    }
}
