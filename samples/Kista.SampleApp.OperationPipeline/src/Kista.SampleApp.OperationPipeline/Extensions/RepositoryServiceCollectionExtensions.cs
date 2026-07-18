using Kista;
using Kista.SampleApp.OperationPipeline.Data;
using Kista.SampleApp.OperationPipeline.Interceptors;
using Kista.SampleApp.OperationPipeline.Models;
using Kista.SampleApp.OperationPipeline.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Kista.SampleApp.OperationPipeline.Extensions;

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
                .WithLifecycle())
            .AddRepository<TaskRepository>(repo => repo
                .WithManagement(mgmt => mgmt
                    .WithInterceptor<BusinessHoursInterceptor<TaskItem, Guid>>()
                    .WithInterceptor<AuditInterceptor<TaskItem, Guid>>()));

        return services;
    }
}