namespace EventosVivos.Api.Common;

// Envelope de respuesta uniforme para toda la API (mismo estilo que el proyecto
// Contabot de referencia): el cliente siempre recibe { ok, data, error, requestId,
// timestamp }, de modo que el frontend tiene un contrato consistente para éxito y error.
public sealed class ApiResponse<TData>
{
    public bool Ok { get; init; }

    public TData? Data { get; init; }

    public ApiError? Error { get; init; }

    public string? RequestId { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<TData> Success(TData data, string? requestId = null)
    {
        return new()
        {
            Ok = true,
            Data = data,
            RequestId = requestId,
        };
    }

    public static ApiResponse<TData> Failure(ApiError error, string? requestId = null)
    {
        return new()
        {
            Ok = false,
            Error = error,
            RequestId = requestId,
        };
    }
}

// Detalle de error legible por máquina (Code) y por humano (Message). Details lleva la
// lista de errores de validación cuando aplica (RF-01).
public sealed class ApiError
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public IReadOnlyCollection<string>? Details { get; init; }
}
