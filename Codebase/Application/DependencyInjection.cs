using CmsEvents.Application.UseCases.DisableCmsEvent;
using CmsEvents.Application.UseCases.ListCmsEvents;
using CmsEvents.Application.UseCases.ProcessCmsEvents;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CmsEvents.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProcessCmsEventsUseCase, ProcessCmsEventsUseCase>();
        services.AddScoped<IListCmsEventsUseCase, ListCmsEventsUseCase>();
        services.AddScoped<IDisableCmsEventUseCase, DisableCmsEventUseCase>();
        services.AddScoped<IValidator<ProcessCmsEventsInput>, ProcessCmsEventInputValidator>();
        services.AddScoped<ICmsEventSanitizer, CmsEventSanitizer>();
        return services;
    }
}