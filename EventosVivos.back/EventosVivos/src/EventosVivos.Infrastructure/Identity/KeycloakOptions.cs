namespace EventosVivos.Infrastructure.Identity;

// Opciones de Keycloak, enlazadas desde la sección "Keycloak" de configuración
// (variables de entorno en Docker, User Secrets en local). El secreto del cliente NO
// se versiona: viene de secrets/variables de entorno.
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    // URL del realm (emisor del token y metadata para validar el JWT).
    public string Authority { get; set; } = string.Empty;

    // URL base de Keycloak para llamar a los endpoints de token y de admin.
    public string BaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; }
}
