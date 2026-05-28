using Kista;
using Kista.SampleApp.Owners.Endpoints;
using Kista.SampleApp.Owners.Extensions;
using Kista.SampleApp.Owners.Middleware;
using Kista.SampleApp.Owners.Models;
using Kista.SampleApp.Owners.SeedData;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddOwnerScopedRepositories(builder.Configuration);

builder.Services.AddHttpUserAccessor<string>();

var app = builder.Build();

// Seed data directly through the repository (bypasses lifecycle handler)
using (var scope = app.Services.CreateScope())
{
    var seedData = new DefaultSeedData();
    var noteRepo = scope.ServiceProvider.GetRequiredService<IRepository<Note, Guid>>();
    var notes = ((IRepositorySeedDataProvider<Note>)seedData).GetSeedData().ToList();
    if (notes.Count > 0) await noteRepo.AddRangeAsync(notes);

    var taskRepo = scope.ServiceProvider.GetRequiredService<IRepository<TaskItem, Guid>>();
    var tasks = ((IRepositorySeedDataProvider<TaskItem>)seedData).GetSeedData().ToList();
    if (tasks.Count > 0) await taskRepo.AddRangeAsync(tasks);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<FakeUserMiddleware>();

app.MapNoteEndpoints();
app.MapTaskEndpoints();

await app.RunAsync();
