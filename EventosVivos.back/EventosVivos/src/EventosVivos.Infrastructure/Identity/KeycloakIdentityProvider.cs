using System.Net;
using System.Text;
using System.Text.Json;
using EventosVivos.Application.Exceptions;
using EventosVivos.Application.Models;
using EventosVivos.Application.Ports;
using Microsoft.Extensions.Options;

namespace EventosVivos.Infrastructure.Identity;

// Adaptador de salida: implementa IIdentityProvider hablando HTTP con Keycloak (direct
// grant para login/refresh y Admin API para el registro). Toda la dependencia de
// Keycloak vive aquí; Application solo conoce la abstracción (Inversión de dependencias).
public sealed class KeycloakIdentityProvider : IIdentityProvider
{
    // Todo usuario nuevo nace como asistente; el rol 'organizer' se concede aparte (consola
    // de Keycloak o promoción explícita), nunca en el auto-registro.
    private const string DefaultRealmRole = "attendee";

    private readonly HttpClient _httpClient;
    private readonly KeycloakOptions _options;

    public KeycloakIdentityProvider(HttpClient httpClient, IOptions<KeycloakOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    private string TokenEndpoint => $"{_options.BaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token";

    private string UsersAdminEndpoint => $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users";

    public async Task<string> RegisterAsync(
        string email,
        string password,
        string? fullName,
        CancellationToken cancellationToken
    )
    {
        var adminToken = await GetServiceAccountTokenAsync(cancellationToken: cancellationToken);

        // Keycloak 24+ trae activa la acción VERIFY_PROFILE, que exige firstName y
        // lastName no vacíos; si faltan, el login posterior falla con "Account is not
        // fully set up". Garantizamos ambos a partir del nombre completo o del email.
        var (firstName, lastName) = SplitName(fullName: fullName, email: email);

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, UsersAdminEndpoint);
        createRequest.Headers.Authorization = new("Bearer", adminToken);

        createRequest.Content = JsonContent(new
        {
            username = email,
            email,
            enabled = true,
            emailVerified = true,
            firstName,
            lastName,
            // Sin acciones requeridas pendientes para no bloquear el primer login.
            requiredActions = Array.Empty<string>(),
        });

        using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);

        if (createResponse.StatusCode == HttpStatusCode.Conflict)
        {
            throw new RegistrationFailedException(message: "Ya existe un usuario con ese email.");
        }

        if (!createResponse.IsSuccessStatusCode)
        {
            var detail = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            
            throw new RegistrationFailedException(
                message: $"No se pudo registrar el usuario: {detail}"
            );
        }

        // Keycloak devuelve la ubicación del nuevo usuario: .../users/{id}. Ese id es el `sub`.
        var location = createResponse.Headers.Location?.ToString();
        var subjectId = location?[(location.LastIndexOf('/') + 1)..];

        if (string.IsNullOrWhiteSpace(subjectId))
        {
            throw new RegistrationFailedException(
                message: "Keycloak no devolvió el identificador del usuario creado."
            );
        }

        // La contraseña se establece con un reset-password explícito (más fiable que
        // incluir 'credentials' en la creación, que en algunas versiones no la aplica).
        await SetPasswordAsync(
            subjectId: subjectId,
            password: password,
            adminToken: adminToken,
            cancellationToken: cancellationToken
        );

        // Rol por defecto del realm: el usuario queda como asistente desde su creación.
        await AssignRealmRoleAsync(
            subjectId: subjectId,
            roleName: DefaultRealmRole,
            adminToken: adminToken,
            cancellationToken: cancellationToken
        );

        return subjectId;
    }

    // Asigna un rol del realm al usuario. El service account tiene 'manage-users' pero NO
    // 'view-realm', así que no puede leer /roles/{name} (Keycloak responde 403). En su lugar
    // consultamos los roles que SÍ puede asignar a este usuario (role-mappings/realm/available,
    // permiso cubierto por manage-users) y de ahí tomamos la representación id+name que exige
    // el POST de mapeo. Así evitamos ampliar permisos del service account en el realm.
    private async Task AssignRealmRoleAsync(
        string subjectId,
        string roleName,
        string adminToken,
        CancellationToken cancellationToken
    )
    {
        using var availableRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{UsersAdminEndpoint}/{subjectId}/role-mappings/realm/available"
        );

        availableRequest.Headers.Authorization = new("Bearer", adminToken);

        using var availableResponse = await _httpClient.SendAsync(
            availableRequest,
            cancellationToken
        );

        if (!availableResponse.IsSuccessStatusCode)
        {
            var detail = await availableResponse.Content.ReadAsStringAsync(cancellationToken);
            
            throw new RegistrationFailedException(
                message: $"No se pudieron leer los roles asignables: {detail}"
            );
        }

        await using var stream = await availableResponse.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            utf8Json: stream,
            cancellationToken: cancellationToken
        );

        string? roleId = null;
        foreach (var role in document.RootElement.EnumerateArray())
        {
            if (role.TryGetProperty("name", out var name) && name.GetString() == roleName)
            {
                roleId = role.GetProperty("id").GetString();

                break;
            }
        }

        // Si el rol no aparece como disponible (p. ej. ya estaba asignado), no hay nada que hacer.
        if (roleId is null)
        {
            return;
        }

        using var assignRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{UsersAdminEndpoint}/{subjectId}/role-mappings/realm"
        );

        assignRequest.Headers.Authorization = new("Bearer", adminToken);

        assignRequest.Content = JsonContent(
            payload: new[]
            {
                new
                {
                    id = roleId, name = roleName
                }
            }
        );

        using var assignResponse = await _httpClient.SendAsync(
            assignRequest,
            cancellationToken
        );

        if (!assignResponse.IsSuccessStatusCode)
        {
            var detail = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
            
            throw new RegistrationFailedException(
                message: $"No se pudo asignar el rol '{roleName}': {detail}"
            );
        }
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken
    )
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["username"] = email,
            ["password"] = password,
            ["scope"] = "openid email",
        };

        return await RequestTokenAsync(form: form, cancellationToken: cancellationToken);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken
    )
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["refresh_token"] = refreshToken,
        };

        return await RequestTokenAsync(form: form, cancellationToken: cancellationToken);
    }

    private async Task<AuthenticationResult> RequestTokenAsync(
        Dictionary<string, string> form,
        CancellationToken cancellationToken
    )
    {
        using var response = await _httpClient.PostAsync(
            requestUri: TokenEndpoint,
            content: new FormUrlEncodedContent(form),
            cancellationToken: cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            throw new AuthenticationFailedException();
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(
            utf8Json: stream,
            cancellationToken: cancellationToken
        );

        var root = document.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()!;
        var (subject, email) = DecodeSubjectAndEmail(jwt: accessToken);

        return new()
        {
            AccessToken = accessToken,
            RefreshToken = root.GetProperty("refresh_token").GetString()!,
            ExpiresIn = root.GetProperty("expires_in").GetInt32(),
            RefreshExpiresIn = root.TryGetProperty("refresh_expires_in", out var refreshExpires)
                ? refreshExpires.GetInt32()
                : 0,
            TokenType = root.TryGetProperty("token_type", out var tokenType) ? tokenType.GetString()! : "Bearer",
            Subject = subject,
            Email = email,
        };
    }

    // Deriva nombre y apellido (ambos no vacíos) del nombre completo; si no viene, usa
    // la parte local del email como respaldo.
    private static (string FirstName, string LastName) SplitName(
        string? fullName,
        string email
    )
    {
        var fallback = email.Split('@')[0];

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (fallback, fallback);
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var first = parts[0];
        var last = parts.Length > 1 ? string.Join(' ', parts[1..]) : first;

        return (first, last);
    }

    private async Task SetPasswordAsync(
        string subjectId,
        string password,
        string adminToken,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{UsersAdminEndpoint}/{subjectId}/reset-password");
        request.Headers.Authorization = new("Bearer", adminToken);
        request.Content = JsonContent(new { type = "password", value = password, temporary = false });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new RegistrationFailedException(
                message: $"No se pudo establecer la contraseña: {detail}"
            );
        }
    }

    private async Task<string> GetServiceAccountTokenAsync(CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
        };

        HttpResponseMessage response = await _httpClient.PostAsync(
            requestUri: TokenEndpoint,
            content: new FormUrlEncodedContent(form),
            cancellationToken: cancellationToken
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement.GetProperty("access_token").GetString()!;
    }

    // Decodifica el payload del JWT (sin validar firma; eso lo hace el middleware de
    // autenticación) solo para extraer `sub` y `email` y poder crear el espejo local.
    private static (string Subject, string Email) DecodeSubjectAndEmail(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return (string.Empty, string.Empty);
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;

        var subject = root.TryGetProperty("sub", out var sub) ? sub.GetString() ?? string.Empty : string.Empty;
        var email = root.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString() ?? string.Empty
            : root.TryGetProperty("preferred_username", out var username)
                ? username.GetString() ?? string.Empty
                : string.Empty;

        return (subject, email);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2:
                padded += "==";
                break;
            case 3:
                padded += "=";
                break;
        }

        return Convert.FromBase64String(padded);
    }

    private static StringContent JsonContent(object payload)
    {
        var json = JsonSerializer.Serialize(payload);
        return new(json, Encoding.UTF8, "application/json");
    }
}