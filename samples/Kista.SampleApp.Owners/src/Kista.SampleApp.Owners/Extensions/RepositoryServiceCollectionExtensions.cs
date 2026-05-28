using Kista;
using Kista.SampleApp.Owners.Models;
using Kista.SampleApp.Owners.Repositories;
using Kista.SampleApp.Owners.SeedData;

namespace Kista.SampleApp.Owners.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddOwnerScopedRepositories(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRepositoryContext()
            .UseInMemory(builder => builder.WithLifecycle())
            .AddRepository<NoteRepository>(repo => repo
                .WithOwnerScoping()
                .WithSeedData<DefaultSeedData>(), ServiceLifetime.Singleton)
            .AddRepository<TaskRepository>(repo => repo
                .WithOwnerScoping()
                .WithSeedData<DefaultSeedData>(), ServiceLifetime.Singleton);

        return services;
    }
}
