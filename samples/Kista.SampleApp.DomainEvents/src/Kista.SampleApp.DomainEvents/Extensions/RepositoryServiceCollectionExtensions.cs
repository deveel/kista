using Deveel;
using Hermodr;
using Kista;
using Kista.Events;
using Kista.SampleApp.DomainEvents.Data;

using Microsoft.EntityFrameworkCore;

namespace Kista.SampleApp.DomainEvents.Extensions;

public static class RepositoryServiceCollectionExtensions {
	public static IServiceCollection AddTaskRepository(this IServiceCollection services, IConfiguration configuration) {
		services.AddDbContext<SampleDbContext>(opt =>
			opt.UseInMemoryDatabase("kista-domain-events-sample"));

		services.AddRepositoryContext()
			.AddRepository<TaskRepository>(repo => repo
				.WithManagement(mgmt => mgmt
					.WithHermodrEvents(options => {
						options.SourceUriScheme = "sampleapp";
					})));

		// Register an in-process subscriber that reacts to task-created events
		var builder = services.AddEventPublisher();
		builder.AddSubscriptions(subs => subs
			.Subscribe("kista.entity.created", async (cloudEvent, ct) => {
				Console.WriteLine($"[Subscriber] Task created: {cloudEvent.Subject} (event {cloudEvent.Type} from {cloudEvent.Source})");
				await Task.CompletedTask;
			}));

		return services;
	}
}