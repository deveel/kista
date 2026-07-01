using Kista;
using Kista.SampleApp.SoftDelete.DTOs;
using Kista.SampleApp.SoftDelete.Models;
using Kista.SampleApp.SoftDelete.Repositories;

using Microsoft.EntityFrameworkCore;

namespace Kista.SampleApp.SoftDelete.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .WithOpenApi();

        group.MapGet("/", async (
            TaskRepository repository,
            CancellationToken ct) =>
        {
            var tasks = await repository.Queryable()
                .OrderBy(t => t.Title)
                .ToListAsync(ct);
            return Results.Ok(tasks.Select(ToResponse));
        })
        .WithName("GetActiveTasks")
        .WithSummary("Get active tasks")
        .WithDescription("Returns only tasks that have not been soft-deleted. The EF global query filter (HasSoftDeleteFilter) excludes deleted rows automatically.");

        group.MapGet("/all", async (
            TaskRepository repository,
            CancellationToken ct) =>
        {
            var tasks = await repository.Queryable()
                .IgnoreQueryFilters()
                .OrderBy(t => t.Title)
                .ToListAsync(ct);
            return Results.Ok(tasks.Select(ToResponse));
        })
        .WithName("GetAllTasks")
        .WithSummary("Get all tasks including deleted")
        .WithDescription("Returns every task, including soft-deleted ones, by ignoring the EF global query filter.");

        group.MapGet("/deleted", async (
            TaskRepository repository,
            CancellationToken ct) =>
        {
            var tasks = await repository.Queryable()
                .IgnoreQueryFilters()
                .Where(t => t.IsDeleted)
                .OrderBy(t => t.Title)
                .ToListAsync(ct);
            return Results.Ok(tasks.Select(ToResponse));
        })
        .WithName("GetDeletedTasks")
        .WithSummary("Get only soft-deleted tasks")
        .WithDescription("Returns only tasks flagged as soft-deleted (IsDeleted == true).");

        group.MapGet("/{id:guid}", async (
            Guid id,
            TaskRepository repository,
            CancellationToken ct) =>
        {
            var task = await repository.FindAsync(id, ct);
            return task is not null
                ? Results.Ok(ToResponse(task))
                : Results.NotFound();
        })
        .WithName("GetTaskById")
        .WithSummary("Get a task by ID")
        .WithDescription("Retrieves a single active task by ID. Soft-deleted tasks are not returned (FindAsync returns null for deleted rows).");

        group.MapPost("/", async (
            CreateTaskRequest request,
            IRepository<TaskItem, Guid> repository,
            CancellationToken ct) =>
        {
            var task = new TaskItem
            {
                Title = request.Title,
                IsCompleted = request.IsCompleted
            };

            await repository.AddAsync(task, ct);

            return Results.Created($"/api/tasks/{task.Id}", ToResponse(task));
        })
        .WithName("CreateTask")
        .WithSummary("Create a new task");

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
            task.UpdatedAtUtc = DateTimeOffset.UtcNow;

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
        .WithSummary("Soft-delete a task")
        .WithDescription("Marks the task as deleted (IsDeleted=true, DeletedAtUtc=now) without removing the row. Active queries will no longer return it. This is the default behavior of RemoveAsync for ISoftDeletable entities.");

        group.MapDelete("/{id:guid}/force", async (
            Guid id,
            TaskRepository repository,
            CancellationToken ct) =>
        {
            var task = await repository.Queryable()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id, ct);
            if (task is null)
                return Results.NotFound();

            await repository.HardDeleteAsync(task, ct);

            return Results.NoContent();
        })
        .WithName("HardDeleteTask")
        .WithSummary("Permanently delete a task")
        .WithDescription("Physically removes the row from the database via HardDeleteAsync. Use with care: this cannot be undone.");

        group.MapPost("/{id:guid}/restore", async (
            Guid id,
            TaskRepository repository,
            CancellationToken ct) =>
        {
            var task = await repository.Queryable()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id, ct);
            if (task is null || !task.IsDeleted)
                return Results.NotFound();

            task.IsDeleted = false;
            task.DeletedAtUtc = null;
            task.DeletedBy = null;
            task.UpdatedAtUtc = DateTimeOffset.UtcNow;

            await repository.UpdateAsync(task, ct);

            return Results.Ok(ToResponse(task));
        })
        .WithName("RestoreTask")
        .WithSummary("Restore a soft-deleted task")
        .WithDescription("Clears the soft-delete flags so the task reappears in active queries.");
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.IsCompleted,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.IsDeleted,
            task.DeletedAtUtc,
            task.DeletedBy);
}