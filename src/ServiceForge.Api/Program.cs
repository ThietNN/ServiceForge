using ServiceForge.Application;
using ServiceForge.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceForgeApplication();
builder.Services.AddServiceForgeInfrastructure();

var app = builder.Build();

app.Run();
