using CmsEvents.Domain.Exceptions;

namespace CmsEvents.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<ExceptionHandlingMiddleware> logger)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "A domain exception occurred while processing the request");
            await HandleExceptionAsync(context, logger, StatusCodes.Status400BadRequest, title: "Domain rule violation");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, logger, StatusCodes.Status500InternalServerError, title: "An unexpected error occurred");
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, ILogger logger, int statusCode, string title)
    {
        if (context.Response.HasStarted)
        {
            logger.LogError
            (
                "Exception occurred after the response had already started for {Path}. " +
                "The response could not be converted to a {StatusCode}. Aborting the connection",
                context.Request.Path, statusCode
            );

            context.Abort();
            return;
        }

        context.Response.StatusCode = statusCode;
        await Results.Problem
        (
            statusCode: statusCode,
            title: title,
            detail: "An error occurred while processing the request",
            instance: context.Request.Path
        ).ExecuteAsync(context);
    }
}