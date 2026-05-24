using Deveel.Repository.SampleApp.Endpoints;
using Deveel.Repository.SampleApp.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddContactRepository(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapLifecycleEndpoints();
app.MapContactEndpoints();

app.Run();
