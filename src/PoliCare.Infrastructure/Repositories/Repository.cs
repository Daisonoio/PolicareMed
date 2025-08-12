using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PoliCare.Core.Entities;
using PoliCare.Core.Interfaces;
using System.Linq.Expressions;

namespace PoliCare.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;
    private readonly ILogger<Repository<T>> _logger;

    public Repository(DbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    // Query methods
    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        try
        {
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                return await _dbSet.FindAsync(id);
            }

            // For non-BaseEntity types
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity by id: {Id}", id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            return await _dbSet.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities");
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
    {
        try
        {
            return await _dbSet
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged entities");
            throw;
        }
    }

    // AGGIORNATO: Restituisce IQueryable di Entity Framework
    public virtual IQueryable<T> Find(Expression<Func<T, bool>> expression)
    {
        return _dbSet.Where(expression);
    }

    public virtual IQueryable<T> FindWithDeleted(Expression<Func<T, bool>> expression)
    {
        if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
        {
            return _dbSet.IgnoreQueryFilters().Where(expression);
        }
        return _dbSet.Where(expression);
    }

    // AGGIUNTO: Metodo per ottenere IQueryable
    public virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> expression)
    {
        try
        {
            return await _dbSet.AnyAsync(expression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking entity existence");
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? expression = null)
    {
        try
        {
            return expression == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(expression);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities");
            throw;
        }
    }

    // Command methods
    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.CreatedAt = DateTime.UtcNow;
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            await _dbSet.AddAsync(entity);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity");
            throw;
        }
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            var entityList = entities.ToList();
            var now = DateTime.UtcNow;

            foreach (var entity in entityList)
            {
                if (entity is BaseEntity baseEntity)
                {
                    baseEntity.CreatedAt = now;
                    baseEntity.UpdatedAt = now;
                }
            }

            await _dbSet.AddRangeAsync(entityList);
            return entityList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding range of entities");
            throw;
        }
    }

    public virtual void Update(T entity)
    {
        try
        {
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            _dbSet.Update(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity");
            throw;
        }
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var entity in entities)
            {
                if (entity is BaseEntity baseEntity)
                {
                    baseEntity.UpdatedAt = now;
                }
            }

            _dbSet.UpdateRange(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating range of entities");
            throw;
        }
    }

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var entity = await GetByIdAsync(id);
            if (entity == null) return false;

            if (entity is BaseEntity baseEntity)
            {
                // Soft delete
                baseEntity.IsDeleted = true;
                baseEntity.DeletedAt = DateTime.UtcNow;
                // baseEntity.DeletedBy = _currentUserService.GetUserId(); // TODO: Implement when auth is ready

                _dbSet.Update(entity);
            }
            else
            {
                // Hard delete for non-BaseEntity types
                _dbSet.Remove(entity);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity with id: {Id}", id);
            throw;
        }
    }

    public virtual async Task<bool> DeleteRangeAsync(Expression<Func<T, bool>> expression)
    {
        try
        {
            var entities = await Find(expression).ToListAsync();

            if (!entities.Any()) return false;

            var now = DateTime.UtcNow;
            foreach (var entity in entities)
            {
                if (entity is BaseEntity baseEntity)
                {
                    baseEntity.IsDeleted = true;
                    baseEntity.DeletedAt = now;
                }
                else
                {
                    _dbSet.Remove(entity);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting range of entities");
            throw;
        }
    }

    public virtual async Task<bool> RestoreAsync(Guid id)
    {
        try
        {
            if (!typeof(BaseEntity).IsAssignableFrom(typeof(T)))
            {
                _logger.LogWarning("Restore called on non-BaseEntity type");
                return false;
            }

            var entity = await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => (e as BaseEntity)!.Id == id);

            if (entity == null || entity is not BaseEntity baseEntity)
                return false;

            baseEntity.IsDeleted = false;
            baseEntity.DeletedAt = null;
            baseEntity.DeletedBy = null;
            baseEntity.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(entity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring entity with id: {Id}", id);
            throw;
        }
    }

    public virtual async Task<bool> HardDeleteAsync(Guid id)
    {
        try
        {
            var entity = await _dbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => (e as BaseEntity)!.Id == id);

            if (entity == null) return false;

            _dbSet.Remove(entity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting entity with id: {Id}", id);
            throw;
        }
    }
}