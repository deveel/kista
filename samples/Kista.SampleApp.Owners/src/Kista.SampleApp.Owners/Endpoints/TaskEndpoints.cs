using Kista;
using Kista.SampleApp.Owners.DTOs;
using Kista.SampleApp.Owners.Models;

namespace Kista.SampleApp.Owners.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .WithOpenApi();

        group.MapGet("/", async (
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var tasks = await repository.FindAllAsync(ct);
            return Results.Ok((tasks ?? Array.Empty<TaskItem>()).Select(ToResponse));
        })
        .WithName("GetAllTasks")
        .WithSummary("Get all tasks for the current user");

        group.MapGet("/{id:guid}", async (
            Guid id,
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var task = await repository.FindAsync(id, ct);
            return task is not null
                ? Results.Ok(ToResponse(task))
                : Results.NotFound();
        })
        .WithName("GetTaskById")
        .WithSummary("Get a task by ID");

        group.MapPost("/", async (
            CreateTaskRequest request,
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var task = new TaskItem
            {
                Title = request.Title
            };

            await repository.AddAsync(task, ct);

            return Results.Created($"/api/tasks/{task.Id}", ToResponse(task));
        })
        .WithName("CreateTask")
        .WithSummary("Create a new task")
        .WithDescription("Owner is automatically set from the current user context.");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTaskRequest request,
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var task = await repository.FindAsync(id, ct);
            if (task is null)
                return Results.NotFound();

            task.Title = request.Title;
            task.IsCompleted = request.IsCompleted;
            task.UpdatedAt = DateTime.UtcNow;

            await repository.UpdateAsync(task, ct);

            return Results.Ok(ToResponse(task));
        })
        .WithName("UpdateTask")
        .WithSummary("Update an existing task");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var task = await repository.FindAsync(id, ct);
            if (task is null)
                return Results.NotFound();

            await repository.RemoveAsync(task, ct);

            return Results.NoContent();
        })
        .WithName("DeleteTask")
        .WithSummary("Delete a task");

        group.MapDelete("/", async (
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var tasks = await repository.FindAllAsync(ct);
            if (tasks != null)
                await repository.RemoveRangeAsync(tasks, ct);

            return Results.NoContent();
        })
        .WithName("DeleteAllTasks")
        .WithSummary("Delete all tasks for the current user");
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.IsCompleted,
            task.Owner,
            task.CreatedAt,
            task.UpdatedAt
        );
}
