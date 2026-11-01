using ServiceForge.Application;
using ServiceForge.Infrastructure;
using ServiceForge.Api.Endpoints;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceForgeApplication();
var connectionString = builder.Configuration.GetConnectionString("ServiceForge")
    ?? Environment.GetEnvironmentVariable("SERVICEFORGE_CONNECTION_STRING");
builder.Services.AddServiceForgeInfrastructure(connectionString);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapServiceForgeEndpoints();
app.Run();

public partial class Program;
