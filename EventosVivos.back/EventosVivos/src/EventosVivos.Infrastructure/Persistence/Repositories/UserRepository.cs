using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Ports;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly EventosVivosDbContext _dbContext;

    public UserRepository(EventosVivosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByKeycloakSubjectIdAsync(
        string keycloakSubjectId,
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(user => user.KeycloakSubjectId == keycloakSubjectId,
                cancellationToken: cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken
    )
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}