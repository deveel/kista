using Kista;
using Kista.SampleApp.OperationPipeline.Data;
using Kista.SampleApp.OperationPipeline.Endpoints;
using Kista.SampleApp.OperationPipeline.Extensions;
using Kista.SampleApp.OperationPipeline.Models;
using Kista.SampleApp.OperationPipeline.SeedData;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddTaskRepository(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    await dbContext.Database.EnsureDeletedAsync();
    await dbContext.Database.EnsureCreatedAsync();

    var seedData = new DefaultSeedData();
    var tasks = ((IRepositorySeedDataProvider<TaskItem>)seedData).GetSeedData().ToList();
    if (tasks.Count > 0)
        await dbContext.Tasks.AddRangeAsync(tasks);

    await dbContext.SaveChangesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTaskEndpoints();

await app.RunAsync();