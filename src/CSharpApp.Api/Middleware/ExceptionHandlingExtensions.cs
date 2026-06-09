namespace CSharpApp.Api.Middleware;

public static class ExceptionHandlingExtensions
{
    public static WebApplication UseGlobalExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionHandlerApp =>
        {
            exceptionHandlerApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var (statusCode, title, detail) = GetProblemDetails(exception);

                context.Response.StatusCode = statusCode;

                await Results.Problem(
                    statusCode: statusCode,
                    title: title,
                    detail: detail,
                    instance: context.Request.Path)
                    .ExecuteAsync(context);
            });
        });

        return app;
    }

    private static (int StatusCode, string Title, string Detail) GetProblemDetails(Exception? exception)
    {
        return exception switch
        {
            HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                "Third-party service unavailable",
                "The upstream service could not complete the request."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "The server encountered an unexpected error while processing the request.")
        };
    }
}
