using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PoliCare.Core.Interfaces;
using PoliCare.Infrastructure.Data;

namespace PoliCare.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly PoliCareDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(PoliCareDbContext context, ILogger<UnitOfWork> logger, ILoggerFactory loggerFactory)
    {
        _context = context;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _repositories = new Dictionary<Type, object>();
    }

    public bool HasActiveTransaction => _currentTransaction != null;

    public IRepository<T> Repository<T>() where T : class
    {
        if (_repositories.ContainsKey(typeof(T)))
        {
            return (IRepository<T>)_repositories[typeof(T)];
        }

        var repositoryLogger = _loggerFactory.CreateLogger<Repository<T>>();
        var repository = new Repository<T>(_context, repositoryLogger);
        _repositories[typeof(T)] = repository;
        return repository;
    }

    public async Task<int> CompleteAsync()
    {
        return await CompleteAsync(CancellationToken.None);
    }

    public async Task<int> CompleteAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency exception occurred");
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update exception occurred");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing unit of work");
            throw;
        }
    }

    public async Task BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            _logger.LogWarning("Transaction already in progress");
            return;
        }

        _currentTransaction = await _context.Database.BeginTransactionAsync();
        _logger.LogInformation("Database transaction started");
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();

            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync();
                _logger.LogInformation("Database transaction committed");
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync();
                _logger.LogInformation("Database transaction rolled back");
            }
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}