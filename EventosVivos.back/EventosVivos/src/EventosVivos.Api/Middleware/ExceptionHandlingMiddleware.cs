using EventosVivos.Api.Common;
using EventosVivos.Application.Exceptions;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Api.Middleware;

// Adaptador transversal: traduce excepciones de dominio/aplicación a respuestas HTTP
// consistentes, envueltas en el mismo ApiResponse que usan los controllers.
// Responsabilidad única (S de SOLID): este es el único lugar de toda la Api que decide
// el mapeo entre un tipo de excepción y un código HTTP; los controllers quedan libres
// de try/catch repetitivo.
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException domainException)
        {
            await WriteDomainExceptionResponseAsync(context: context, domainException: domainException);
        }
        catch (Exception unexpectedException)
        {
            _logger.LogError(unexpectedException, "Error no controlado procesando {Path}", context.Request.Path);
            await WriteUnexpectedExceptionResponseAsync(context: context);
        }
    }

    private static Task WriteDomainExceptionResponseAsync(HttpContext context, DomainException domainException)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = MapToStatusCode(domainException: domainException);

        var error = new ApiError
        {
            Code = domainException.ErrorCode,
            Message = domainException.Message,
            Details = domainException is DomainValidationException validationException
                ? validationException.Errors
                : null,
        };

        var body = ApiResponse<object>.Failure(error: error, requestId: context.TraceIdentifier);

        return context.Response.WriteAsJsonAsync(body);
    }

    private static Task WriteUnexpectedExceptionResponseAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var error = new ApiError
        {
            Code = "INTERNAL_SERVER_ERROR",
            Message = "Ocurrió un error inesperado. Intenta nuevamente más tarde.",
        };

        var body = ApiResponse<object>.Failure(error: error, requestId: context.TraceIdentifier);

        return context.Response.WriteAsJsonAsync(body);
    }

    private static int MapToStatusCode(DomainException domainException) => domainException switch
    {
        DomainValidationException => StatusCodes.Status400BadRequest,
        AuthenticationFailedException => StatusCodes.Status401Unauthorized,
        EventNotFoundException or VenueNotFoundException or ReservationNotFoundException => StatusCodes.Status404NotFound,
        VenueScheduleConflictException or InvalidReservationStateTransitionException or InvalidEventStateException
            or RegistrationFailedException or VenueInUseException
            or VenueCapacityBelowScheduledEventException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity,
    };
}
