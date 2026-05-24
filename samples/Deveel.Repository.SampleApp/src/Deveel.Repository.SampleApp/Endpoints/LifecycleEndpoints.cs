using Deveel.Data;
using Deveel.Repository.SampleApp.Models;

namespace Deveel.Repository.SampleApp.Endpoints;

public static class LifecycleEndpoints
{
    public static void MapLifecycleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lifecycle")
            .WithTags("Lifecycle")
            .WithOpenApi();

        group.MapPost("/create", async (
            IRepositoryLifecycleService service,
            CancellationToken ct) =>
        {
            await service.CreateRepositoryAsync<Contact, Guid>(ct);
            return Results.Ok(new { Message = "Contact repository created" });
        })
        .WithName("CreateRepository")
        .WithSummary("Create the Contact repository")
        .WithDescription("Creates the Contact repository if it doesn't exist.");

        group.MapPost("/drop", async (
            IRepositoryLifecycleService service,
            CancellationToken ct) =>
        {
            await service.DropRepositoryAsync<Contact, Guid>(ct);
            return Results.Ok(new { Message = "Contact repository dropped" });
        })
        .WithName("DropRepository")
        .WithSummary("Drop the Contact repository")
        .WithDescription("Drops the Contact repository, removing all data.");

        group.MapPost("/seed", async (
            IRepositoryLifecycleService service,
            CancellationToken ct) =>
        {
            await service.SeedRepositoryAsync<Contact, Guid>(null, ct);
            return Results.Ok(new { Message = "Contact repository seeded" });
        })
        .WithName("SeedRepository")
        .WithSummary("Seed the Contact repository")
        .WithDescription("Seeds the Contact repository with default data.");

        group.MapPost("/initialize", async (
            IRepositoryLifecycleService service,
            CancellationToken ct) =>
        {
            await service.DropRepositoryAsync<Contact, Guid>(ct);
            await service.CreateRepositoryAsync<Contact, Guid>(ct);
            await service.SeedRepositoryAsync<Contact, Guid>(null, ct);

            return Results.Ok(new { Message = "Contact repository initialized (dropped, created, and seeded)" });
        })
        .WithName("InitializeRepository")
        .WithSummary("Fully initialize the Contact repository")
        .WithDescription("Drops, creates, and seeds the Contact repository in one operation.");
    }
}
