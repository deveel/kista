using Deveel;
using Kista;
using Kista.SampleApp.OperationPipeline.Data;
using Kista.SampleApp.OperationPipeline.DTOs;
using Kista.SampleApp.OperationPipeline.Models;

using Microsoft.EntityFrameworkCore;

namespace Kista.SampleApp.OperationPipeline.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks")
            .WithTags("Tasks")
            .WithOpenApi();

        group.MapGet("/", async (
            SampleDbContext dbContext,
            CancellationToken ct) =>
        {
            var tasks = await dbContext.Tasks
                .OrderBy(t => t.Title)
                .ToListAsync(ct);
            return Results.Ok(tasks.Select(ToResponse));
        })
        .WithName("GetAllTasks")
        .WithSummary("Get all tasks");

        group.MapGet("/{id:guid}", async (
            Guid id,
            EntityManager<TaskItem, Guid> manager,
            CancellationToken ct) =>
        {
            var result = await manager.FindAsync(id, ct);

            if (result.IsError())
                return Results.NotFound();

            return Results.Ok(ToResponse(result.Value!));
        })
        .WithName("GetTaskById")
        .WithSummary("Get a task by ID");

        group.MapPost("/", async (
            CreateTaskRequest request,
            EntityManager<TaskItem, Guid> manager,
            CancellationToken ct) =>
        {
            var task = new TaskItem
            {
                Title = request.Title,
                IsCompleted = request.IsCompleted
            };

            var result = await manager.AddAsync(task, ct);

            if (result.IsError())
                return Results.Json(new
                {
                    success = false,
                    error = result.Error?.Message ?? "Operation failed"
                }, statusCode: StatusCodes.Status400BadRequest);

            return Results.Created($"/api/tasks/{task.Id}", ToResponse(task));
        })
        .WithName("CreateTask")
        .WithSummary("Create a new task")
        .WithDescription("Creates a task through the EntityManager pipeline. The AuditInterceptor logs the write in PostWriteAsync, and the BusinessHoursInterceptor may short-circuit the operation in PreWriteAsync if attempted outside 09:00-18:00 UTC.");

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTaskRequest request,
            EntityManager<TaskItem, Guid> manager,
            CancellationToken ct) =>
        {
            var findResult = await manager.FindAsync(id, ct);
            if (findResult.IsError())
                return Results.NotFound();

            var task = findResult.Value!;
            task.Title = request.Title;
            task.IsCompleted = request.IsCompleted;

            var result = await manager.UpdateAsync(task, ct);

            if (result.IsError())
                return Results.Json(new
                {
                    success = false,
                    error = result.Error?.Message ?? "Operation failed"
                }, statusCode: StatusCodes.Status400BadRequest);

            return Results.Ok(ToResponse(task));
        })
        .WithName("UpdateTask")
        .WithSummary("Update an existing task")
        .WithDescription("Updates a task through the EntityManager pipeline, triggering the interceptors.");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            EntityManager<TaskItem, Guid> manager,
            CancellationToken ct) =>
        {
            var findResult = await manager.FindAsync(id, ct);
            if (findResult.IsError())
                return Results.NotFound();

            var result = await manager.RemoveAsync(findResult.Value!, ct);

            if (result.IsError())
                return Results.Json(new
                {
                    success = false,
                    error = result.Error?.Message ?? "Operation failed"
                }, statusCode: StatusCodes.Status400BadRequest);

            return Results.NoContent();
        })
        .WithName("DeleteTask")
        .WithSummary("Delete a task")
        .WithDescription("Removes a task through the EntityManager pipeline. The AuditInterceptor logs the deletion in PostWriteAsync.");
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            task.IsCompleted,
            task.CreatedAtUtc,
            task.UpdatedAtUtc);
}