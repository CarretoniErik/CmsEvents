using CmsEvents.Api;
using CmsEvents.Api.Endpoints;
using CmsEvents.Api.Middlewares;
using CmsEvents.Application;
using CmsEvents.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApi(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)    
    .AddHealthChecks();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>()
   .UseAuthentication()
   .UseAuthorization();

app.MapHealthChecks("/health");
app.MapCmsEndpoints()
   .MapConsumersEndpoints();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
if (!app.Environment.IsEnvironment("Testing")) 
{
    app.UseHttpsRedirection();
    await app.Services.EnsureDatabaseCreatedAsync();
}

app.Run();