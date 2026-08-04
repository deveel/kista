using Deveel;
using Kista;
using Kista.SampleApp.DomainEvents;

namespace Kista.SampleApp.DomainEvents.Endpoints;

public static class TaskEndpoints {
	public static void MapTaskEndpoints(this WebApplication app) {
		var group = app.MapGroup("/api/tasks");

		group.MapPost("/", async (TaskItem task, EntityManager<TaskItem, string> manager) => {
			task.Id = Guid.NewGuid().ToString();
			task.CreatedAtUtc = DateTimeOffset.UtcNow;
			var result = await manager.AddAsync(task);
			return result.IsSuccess()
				? Results.Created($"/api/tasks/{task.Id}", task)
				: Results.BadRequest(result.Error?.Message);
		});

		group.MapGet("/{id}", async (string id, EntityManager<TaskItem, string> manager) => {
			var result = await manager.FindAsync(id);
			return result.IsSuccess()
				? Results.Ok(result.Value)
				: Results.NotFound();
		});
	}
}