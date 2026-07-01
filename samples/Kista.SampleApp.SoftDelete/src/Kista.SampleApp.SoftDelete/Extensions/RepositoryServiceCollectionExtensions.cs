using Kista;
using Kista.SampleApp.SoftDelete.Data;
using Kista.SampleApp.SoftDelete.Models;
using Kista.SampleApp.SoftDelete.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Kista.SampleApp.SoftDelete.Extensions;

public static class RepositoryServiceCollectionExtensions
{
    public static IServiceCollection AddTaskRepository(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRepositoryContext()
            .UseEntityFramework<SampleDbContext>(builder => builder
                .ConfigureDbContext(options =>
                    options.UseSqlite("Data Source=tasks.db"))
                .WithLifecycle()
                .WithSoftDelete())
            .AddRepository<TaskRepository>();

        return services;
    }
}