using Kista.SampleApp.DomainEvents.Endpoints;
using Kista.SampleApp.DomainEvents.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddTaskRepository(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
	app.MapOpenApi();
}

app.MapTaskEndpoints();

app.Run();