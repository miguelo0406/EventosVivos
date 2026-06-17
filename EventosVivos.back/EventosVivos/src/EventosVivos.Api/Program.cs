using EventosVivos.Api.Middleware;
using EventosVivos.Application;
using EventosVivos.Infrastructure;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString(name: "EventosVivosDatabase")
    ?? throw new InvalidOperationException(message: "No se configuró la cadena de conexión 'EventosVivosDatabase'.");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(setupAction: swaggerOptions =>
{
    swaggerOptions.SwaggerDoc(
        name: "v1",
        info: new() { Title = "EventosVivos API", Version = "v1" });
});

// Composition root: Api cablea Application e Infrastructure detrás de sus puertos,
// pero no conoce sus implementaciones concretas (EF Core, Npgsql) directamente.
builder.Services.AddApplicationUseCases();
builder.Services.AddInfrastructure(connectionString: connectionString);

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
    swaggerUiOptions.SwaggerEndpoint(url: "/swagger/v1/swagger.json", name: "EventosVivos API v1"));

app.UseDomainExceptionHandling();
app.UseCors(policyName: "AllowFrontend");
app.UseHttpsRedirection();
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
