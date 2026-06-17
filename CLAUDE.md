# EventosVivos

Sistema de reservas para eventos culturales, conferencias y talleres. Prueba técnica
fullstack (.NET + Angular) que resuelve control de aforo en tiempo real, conflictos de
horario de venues y validación de reservas/pagos.

Repositorio: https://github.com/miguelo0406/EventosVivos (monorepo, dos carpetas
independientes en la raíz: `EventosVivos.back` y `EventosVivos.front`).

## Stack tecnológico

- **Backend**: .NET 10 / C# 14, ASP.NET Core Web API.
- **Frontend**: Angular (última versión estable).
- **Base de datos**: PostgreSQL (imagen oficial `postgres:16-alpine` por Docker).
  Justificación: elegible en la capa gratuita de AWS RDS (db.t3.micro / db.t4g.micro,
  750 h/mes), sin costo de licenciamiento, excelente soporte EF Core vía Npgsql, e
  imagen Docker liviana. La arquitectura hexagonal hace que el motor de base de datos
  sea un detalle de infraestructura intercambiable (puerto `IXxxRepository` +
  adaptador EF Core), por lo que este es un detalle, no una decisión estructural.
- **Contenedores**: Docker / docker-compose para desarrollo local; despliegue en AWS
  vía contenedores (IaC, detalle a definir en la fase de despliegue).
- **Testing**: xUnit (mínimo pruebas unitarias de dominio y casos de uso).

## Arquitectura

**Hexagonal (Ports & Adapters)** en el backend, buscando una aplicación desacoplada
donde el dominio no depende de frameworks ni de infraestructura.

```
EventosVivos.back/EventosVivos/
├── src/
│   ├── EventosVivos.Domain/          Núcleo: entidades, value objects, enums,
│   │                                  excepciones de dominio, puertos de salida
│   │                                  (interfaces de repositorio), servicios de dominio.
│   │                                  No depende de ninguna otra capa.
│   ├── EventosVivos.Application/     Casos de uso (orquestación). Define puertos de
│   │                                  entrada (interfaces de caso de uso) y los
│   │                                  implementa. Depende solo de Domain.
│   ├── EventosVivos.Infrastructure/  Adaptadores de salida: EF Core + PostgreSQL,
│   │                                  implementaciones de los repositorios definidos
│   │                                  en Domain. Depende de Domain y Application.
│   └── EventosVivos.Api/             Adaptador de entrada: controllers REST,
│                                      middleware, composición/DI. Depende de
│                                      Application e Infrastructure (solo para wiring
│                                      en Program.cs).
└── tests/
    ├── EventosVivos.Domain.Tests/
    └── EventosVivos.Application.Tests/
```

Regla de dependencia: las flechas siempre apuntan hacia adentro (Domain no conoce a
nadie; Application conoce a Domain; Infrastructure y Api conocen a Application y
Domain, nunca al revés). Esto es Dependency Inversion (principio D de SOLID) aplicado
a nivel arquitectónico.

## Convenciones de código (obligatorias)

- **Idioma**: nombres de variables, clases, métodos y propiedades en **inglés**.
  Comentarios explicativos en **español**.
- **Casing**: variables locales y parámetros en `camelCase`; métodos y propiedades en
  `PascalCase`. Campos privados con prefijo `_camelCase` (convención estándar de C#).
- **Indentación**: 4 espacios (sin tabs).
- **Final de archivo**: exactamente un renglón vacío al final de cada archivo.
- **Parámetros nombrados**: SIEMPRE usar named parameters al invocar constructores y
  métodos con más de un argumento (`CreateEvent(title: x, venueId: y)`, no
  `CreateEvent(x, y)`).
- **Construcción de objetos**: preferir siempre `new()` con target-typed inference en
  vez de repetir el nombre de la clase:
  ```csharp
  // Preferido
  return new() { Title = title, Capacity = capacity };

  // Evitar
  return new EventDto() { Title = title, Capacity = capacity };
  ```
- **SOLID**: cada vez que se aplique un principio SOLID de forma deliberada, se deja
  un comentario breve en español junto a la clase/método indicando qué principio es y
  cómo se aplica (ver ejemplos ya presentes en `Domain` y `Application`).
- **Principios de diseño**: arquitectura hexagonal, inyección de dependencias vía
  constructor, value objects inmutables para invariantes (Email, ConfirmationCode),
  excepciones de dominio tipadas (no excepciones genéricas) para reglas de negocio.

## Datos de referencia (seed)

| ID | Nombre            | Capacidad | Ciudad    |
|----|--------------------|-----------|-----------|
| 1  | Auditorio Central  | 200       | Bogotá    |
| 2  | Sala Norte         | 50        | Bogotá    |
| 3  | Arena Sur          | 500       | Medellín  |

Tipos de evento válidos: `conferencia`, `taller`, `concierto`.

## Reglas de negocio (resumen — ver enunciado completo de la prueba para detalle)

| ID | Regla |
|----|-------|
| RN-01 | El evento no puede exceder la capacidad del venue asignado. |
| RN-02 | Dos eventos activos no pueden compartir venue con horarios superpuestos. |
| RN-03 | Eventos en fin de semana (sáb/dom) no pueden iniciar después de las 22:00. |
| RN-04 | No se permiten reservas para eventos que inicien en menos de 1 hora. |
| RN-05 | Eventos con precio > $100 limitan a máximo 10 entradas por transacción. |
| RN-06 | Un evento se marca `completado` automáticamente cuando ahora > hora de fin (estado derivado, no se persiste un job). |
| RN-07 | Cancelar una reserva `confirmada` con menos de 48 h del evento la marca como `perdida`: no libera entradas, solo queda registrada para reporte. |

Regla adicional de RF-03: si el evento inicia en menos de 24 h, una reserva nueva
admite máximo 5 entradas por transacción (independiente del límite de RN-05 por
precio; aplica el más restrictivo).

### Decisiones de interpretación (spec ambigua, documentadas para el evaluador)

- **RF-05 vs RN-07**: el enunciado de RF-05 es internamente inconsistente ("cambiar de
  confirmada a cancelada" pero luego "si ya está pagada/confirmada, rechazar"). Se
  interpreta que una reserva se puede cancelar desde `pendiente_pago` o `confirmada`;
  solo se rechaza si ya está `cancelada` o `perdida` (terminal). Esto es necesario para
  que RN-07 (penalización al cancelar una reserva confirmada) sea alcanzable.
  Se modela un cuarto estado de reserva, `perdida`, distinto de `cancelada`, para
  diferenciar "cancelada con liberación de cupo" de "cancelada sin liberación,
  solo a efectos de reporte".
- **Disponibilidad de entradas**: cuentan contra el cupo las reservas en estado
  `pendiente_pago`, `confirmada` y `perdida` (esta última a propósito no libera cupo).
  Solo `cancelada` libera entradas.
- **RN-06 (estado completado)**: se calcula en el momento de la consulta
  (`ahora > fechaFin && estado == activo` ⇒ `completado`) en vez de un job en segundo
  plano, para mantener el dominio simple y testeable sin infraestructura de scheduling.

## Despliegue (AWS)

Pendiente de definir en la fase de IaC. Se documentará paso a paso en
`EventosVivos.back/DEPLOYMENT.md` (o equivalente) una vez el backend y frontend estén
containerizados. Prioridad: maximizar uso de la capa gratuita de AWS (12 meses) —
EC2 t2/t3.micro en vez de Fargate, RDS PostgreSQL db.t3.micro, ECR para imágenes.

## Estado del repositorio / pendientes conocidos

- La clave SSH local (`~/.ssh/id_ed25519`) todavía no está autorizada en la cuenta de
  GitHub del usuario (autenticación `git@github.com` falla con
  "Permission denied (publickey)"). El remote ya está configurado en formato SSH;
  falta agregar la llave pública en GitHub → Settings → SSH and GPG keys.
