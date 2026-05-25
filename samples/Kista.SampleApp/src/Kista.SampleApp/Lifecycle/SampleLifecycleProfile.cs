using Kista;
using Kista.SampleApp.Models;

namespace Kista.SampleApp.Lifecycle;

public class SampleLifecycleProfile : IRepositoryLifecycleProfile
{
    private readonly IRepositorySeedDataProvider<Contact> _seedDataProvider;

    public SampleLifecycleProfile(IRepositorySeedDataProvider<Contact> seedDataProvider)
    {
        _seedDataProvider = seedDataProvider;
    }

    public SeedStrategy GetSeedStrategy(string? environmentName = null)
    {
        return environmentName?.ToLowerInvariant() switch
        {
            "development" or "dev" => SeedStrategy.Always,
            "production" or "prod" => SeedStrategy.Never,
            _ => SeedStrategy.IfMissing
        };
    }

    public object? GetSeedData(Type entityType)
    {
        if (typeof(Contact).IsAssignableFrom(entityType))
            return _seedDataProvider.GetSeedData();

        return null;
    }
}
