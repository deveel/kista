using Kista;
using Kista.SampleApp.Lifecycle;
using Kista.SampleApp.Models;
using Kista.SampleApp.Repositories;
using Kista.SampleApp.SeedData;

namespace Kista.SampleApp.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddContactRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRepositoryContext()
            .UseInMemory(builder => builder
                .WithLifecycle()
            )
            .AddRepository<ContactRepository>()
            .WithSeedData<Contact, DefaultContactSeedData>()
            .WithLifecycleHandler<Contact, ContactLifecycleHandler>()
            .WithLifecycleProfile<SampleLifecycleProfile>()
            .ConfigureLifecycle(options =>
            {
                options.DeleteIfExists = true;
                options.SeedStrategy = SeedStrategy.ByEnvironment;
            });

        return services;
    }
}
