using Deveel.Data;
using Deveel.Repository.SampleApp.Lifecycle;
using Deveel.Repository.SampleApp.Models;
using Deveel.Repository.SampleApp.Repositories;
using Deveel.Repository.SampleApp.SeedData;

namespace Deveel.Repository.SampleApp.Extensions;

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
