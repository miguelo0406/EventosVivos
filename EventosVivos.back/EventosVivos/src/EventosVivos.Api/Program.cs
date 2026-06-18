using System.Security.Claims;
using System.Text.Json;
using EventosVivos.Api.Middleware;
using EventosVivos.Application;
using EventosVivos.Infrastructure;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:4200"];  

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
                        ?? throw new InvalidOperationException(message: "No se configuró 'Keycloak:Authority'.");
var requireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setupAction: swaggerOptions =>
{
    swaggerOptions.SwaggerDoc(name: "v1", info: new() { Title = "EventosVivos API", Version = "v1" });

    // Permite enviar el JWT (Bearer) desde la propia UI de Swagger para probar endpoints protegidos.
    swaggerOptions.AddSecurityDefinition(
        name: JwtBearerDefaults.AuthenticationScheme,
        securityScheme: new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Pega el access token obtenido en /api/auth/login (sin el prefijo 'Bearer ').",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });

    swaggerOptions.AddSecurityRequirement((OpenApiDocument document) => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = [],
    });
});

// Composition root: Api cablea Application e Infrastructure detrás de sus puertos,
// pero no conoce sus implementaciones concretas (EF Core, Npgsql, Keycloak) directamente.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration: builder.Configuration);

// Capa de seguridad: validación del JWT emitido por Keycloak. La API es stateless;
// no guarda sesión: solo valida firma, emisor y vigencia del token en cada request.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(configureOptions: jwtOptions =>
    {
        jwtOptions.Authority = keycloakAuthority;
        jwtOptions.RequireHttpsMetadata = requireHttpsMetadata;
        jwtOptions.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = keycloakAuthority,
            ValidateAudience = false,
            ValidateLifetime = true,
            // Margen mínimo para que la expiración de 5 min se respete con precisión.
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Keycloak emite los roles del realm anidados en el claim 'realm_access'
        // ({"roles":[...]}). .NET no los reconoce como roles por sí solo, así que los
        // aplanamos a claims de tipo Role tras validar el token; recién entonces
        // RequireRole / [Authorize(Roles=...)] funciona. Se hace aquí (borde de entrada)
        // para que el resto de la app trabaje con el modelo de roles estándar de ASP.NET.
        jwtOptions.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is ClaimsIdentity identity
                    && context.Principal.FindFirst("realm_access")?.Value is { } realmAccess)
                {
                    using var document = JsonDocument.Parse(realmAccess);
                    if (document.RootElement.TryGetProperty("roles", out var roles))
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            if (role.GetString() is { Length: > 0 } roleName)
                            {
                                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                            }
                        }
                    }
                }

                return Task.CompletedTask;
            },
        };
    });

// Autorización global: TODOS los endpoints exigen un JWT válido, salvo los marcados con
// [AllowAnonymous] (login/registro/refresh y Swagger).
builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Política para la superficie de organizador (RF-01, RF-06 y gestión de reservas):
    // exige el rol 'organizer' del realm. Los endpoints de asistente solo requieren un
    // usuario autenticado (cubierto por la FallbackPolicy).
    authorizationOptions.AddPolicy(name: "Organizer", configurePolicy: policy => policy.RequireRole("organizer"));
});

builder.Services.AddCors(setupAction: corsOptions =>
{
    corsOptions.AddPolicy(name: "AllowFrontend", configurePolicy: policyBuilder =>
    {
        policyBuilder
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(setupAction: swaggerUiOptions =>
{
    swaggerUiOptions.SwaggerEndpoint(url: "/swagger/v1/swagger.json", name: "EventosVivos API v1");
    // Swagger como página de inicio: evita el 404 al entrar a la raíz sin conocer la ruta /swagger.
    swaggerUiOptions.RoutePrefix = string.Empty;
});

app.UseDomainExceptionHandling();
app.UseCors(policyName: "AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Aplica migraciones pendientes al iniciar: simplifica el despliegue en contenedores
// (no requiere un paso manual de "dotnet ef database update" en cada entorno).
using (var startupScope = app.Services.CreateScope())
{
    var dbContext = startupScope.ServiceProvider.GetRequiredService<EventosVivosDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Run();

// Necesario para que WebApplicationFactory<Program> (pruebas de integración) pueda
// referenciar este punto de entrada de top-level statements.
public partial class Program
{
}
